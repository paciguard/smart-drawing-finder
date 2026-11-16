using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DrawingSearchTools.Models;
using OfficeOpenXml;

namespace DrawingSearchTools.Services;

/// <summary>
/// Reads drawing numbers from a BOM (Bill Of Material) Excel file.
/// Entries from E5 can use the grouped format (e.g. "4-104 064 010"),
/// a P-prefixed number, or a nine-digit number. Standard parts such as
/// "GB/T898-1988" or "SB1101-00" are filtered out.
/// </summary>
public class BomReader
{
    // The three-digit groups may be separated by spaces or dots.
    private static readonly Regex DrawingNumberRegex =
        new Regex(@"^\s*(\d)\s*-\s*(\d{3})[.\s]+(\d{3})[.\s]+(\d{3})\s*$",
                  RegexOptions.Compiled);

    private static readonly Regex PDrawingNumberRegex =
        new Regex(@"^p([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NineDigitDrawingNumberRegex =
        new Regex(@"^[0-9]{9}$", RegexOptions.Compiled);

    private static readonly Regex GroupedNineDigitDrawingNumberRegex =
        new Regex(@"^([0-9]{3})[.\s]+([0-9]{3})[.\s]+([0-9]{3})$", RegexOptions.Compiled);

    /// <summary>
    /// Parse all rows of the given workbook and return drawing items.
    /// </summary>
    public List<DrawingItem> Read(string xlsxPath)
    {
        if (!File.Exists(xlsxPath))
            throw new FileNotFoundException("BOM file not found.", xlsxPath);

        var result = new List<DrawingItem>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var package = new ExcelPackage(new FileInfo(xlsxPath));

        foreach (var sheet in package.Workbook.Worksheets)
        {
            if (sheet.Dimension == null) continue;
            int lastRow = sheet.Dimension.End.Row;

            for (int r = 5; r <= lastRow; r++)
            {
                // Column E = 5
                string raw = sheet.Cells[r, 5].Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                string value = raw.Trim();
                var match = DrawingNumberRegex.Match(value);
                var pMatch = PDrawingNumberRegex.Match(value);
                bool nineDigitMatch = NineDigitDrawingNumberRegex.IsMatch(value);
                var groupedNineMatch = GroupedNineDigitDrawingNumberRegex.Match(value);
                if (!match.Success && !pMatch.Success && !nineDigitMatch && !groupedNineMatch.Success)
                    continue;

                string nineDigits = groupedNineMatch.Success
                    ? groupedNineMatch.Groups[1].Value + groupedNineMatch.Groups[2].Value + groupedNineMatch.Groups[3].Value
                    : value;
                string normalized = nineDigitMatch || groupedNineMatch.Success ? nineDigits :
                    pMatch.Success ? "P" + pMatch.Groups[1].Value :
                    $"{match.Groups[1].Value}-{match.Groups[2].Value} " +
                    $"{match.Groups[3].Value} {match.Groups[4].Value}";

                string searchKey = nineDigitMatch || groupedNineMatch.Success ? nineDigits :
                    pMatch.Success ? "*p*" + pMatch.Groups[1].Value + "*" :
                    match.Groups[3].Value + match.Groups[4].Value;

                // Deduplicate so we don't search the same file twice
                // (BOMs often repeat the same drawing across rows).
                if (!seenKeys.Add(normalized))
                    continue;

                result.Add(new DrawingItem
                {
                    RowNumber = r,
                    DrawingNumber = normalized,
                    PositionNumber = (sheet.Cells[r, 4].Text ?? string.Empty).Trim(),
                    Description    = (sheet.Cells[r, 6].Text ?? string.Empty).Trim(),
                    SearchKey = searchKey,
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Convenience helper: true if <paramref name="text"/> matches the
    /// drawing-number pattern.
    /// </summary>
    public static bool IsDrawingNumber(string? text)
        => !string.IsNullOrWhiteSpace(text) &&
           (DrawingNumberRegex.IsMatch(text.Trim()) ||
            PDrawingNumberRegex.IsMatch(text.Trim()) ||
            NineDigitDrawingNumberRegex.IsMatch(text.Trim()) ||
            GroupedNineDigitDrawingNumberRegex.IsMatch(text.Trim()));
}

