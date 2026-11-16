using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using DrawingSearchTools.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DrawingSearchTools.Services;

/// <summary>
/// Exports search results to a new Excel workbook and copies the
/// matched drawing files into a target folder. Uses EPPlus.
/// </summary>
public class ResultExporter
{
    /// <summary>Save search results as a new .xlsx workbook.</summary>
    public void ExportToExcel(IList<DrawingItem> items, string outputXlsxPath)
    {
        var dir = Path.GetDirectoryName(outputXlsxPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // EPPlus will create a new file if it doesn't exist; if it exists
        // we delete first so we always start clean.
        if (File.Exists(outputXlsxPath))
            File.Delete(outputXlsxPath);

        using var package = new ExcelPackage(new FileInfo(outputXlsxPath));
        var ws = package.Workbook.Worksheets.Add("Search Results");

        // Header row
        string[] headers = {
            "Row", "Position", "Drawing Number", "Search Key",
            "Description", "Status", "Match Count", "Matched File Path(s)"
        };
        for (int c = 0; c < headers.Length; c++)
            ws.Cells[1, c + 1].Value = headers[c];

        using (var header = ws.Cells[1, 1, 1, headers.Length])
        {
            header.Style.Font.Bold = true;
            header.Style.Fill.PatternType = ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            header.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            header.Style.HorizontalAlignment =
                ExcelHorizontalAlignment.Left;
        }

        int row = 2;
        foreach (var item in items)
        {
            ws.Cells[row, 1].Value = item.RowNumber;
            ws.Cells[row, 2].Value = item.PositionNumber ?? string.Empty;
            ws.Cells[row, 3].Value = item.DrawingNumber;
            ws.Cells[row, 4].Value = item.SearchKey;
            ws.Cells[row, 5].Value = item.Description ?? string.Empty;
            ws.Cells[row, 6].Value = item.Status;
            ws.Cells[row, 7].Value = item.MatchedFiles.Count;
            ws.Cells[row, 8].Value = string.Join(" | ", item.MatchedFiles);

            // Colour-code the status cell
            var statusCell = ws.Cells[row, 6];
            if (item.MatchedFiles.Count == 0)
                statusCell.Style.Font.Color.SetColor(Color.Red);
            else if (item.MatchedFiles.Count > 1)
                statusCell.Style.Font.Color.SetColor(Color.DarkOrange);
            else
                statusCell.Style.Font.Color.SetColor(Color.DarkGreen);

            row++;
        }

        // Freeze header and add an autofilter
        ws.View.FreezePanes(2, 1);
        if (items.Count > 0)
            ws.Cells[1, 1, items.Count + 1, headers.Length].AutoFilter = true;

        // Auto-size, but cap the path column so it doesn't get absurd
        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        if (ws.Column(8).Width > 80) ws.Column(8).Width = 80;

        package.Save();
    }

    /// <summary>
    /// Copy every matched drawing file into <paramref name="outputFolder"/>.
    /// Filenames are kept; on collision " (2)", " (3)" etc. is appended.
    /// </summary>
    public CopyReport CopyFiles(IList<DrawingItem> items, string outputFolder,
                                bool overwriteExisting = false)
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        var report = new CopyReport();
        var allFiles = items
            .SelectMany(i => i.MatchedFiles)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var src in allFiles)
        {
            try
            {
                if (!File.Exists(src))
                {
                    report.Errors.Add($"Missing: {src}");
                    continue;
                }

                string destName = Path.GetFileName(src);
                string dest = Path.Combine(outputFolder, destName);

                if (File.Exists(dest) && !overwriteExisting)
                    dest = MakeUniquePath(dest);

                File.Copy(src, dest, overwriteExisting);
                report.Copied++;
            }
            catch (Exception ex)
            {
                report.Errors.Add($"{src} -> {ex.Message}");
            }
        }

        return report;
    }

    private static string MakeUniquePath(string path)
    {
        string dir  = Path.GetDirectoryName(path) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(path);
        string ext  = Path.GetExtension(path);
        int i = 2;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            i++;
        }
        while (File.Exists(candidate));
        return candidate;
    }
}

/// <summary>Outcome of a copy operation.</summary>
public class CopyReport
{
    public int Copied { get; set; }
    public List<string> Errors { get; } = new();
}

