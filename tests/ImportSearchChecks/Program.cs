using DrawingSearchTools.Services;
using OfficeOpenXml;

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

foreach (var value in new[] { "p142159", "P283917", " p910495 ", "p7130056", "4-104 064 010", "1-204.642.697", "123456789", " 001234567 ", "082 978 200" })
    Check(BomReader.IsDrawingNumber(value), "Rejected " + value);
foreach (var value in new[] { "60X85X17", "GB/T898-1988", "SB1101-00", "p", "p142159x", "12345678", "1234567890", "" })
    Check(!BomReader.IsDrawingNumber(value), "Accepted " + value);
foreach (var name in new[] { "p142159", "PREFIX-P-142159-revA", "P drawing 142159", "p1421590" })
    Check(DrawingFileSearcher.MatchesSearchKey(name, "*p*142159*"), "Missed " + name);
foreach (var name in new[] { "142159", "142159p", "p142158", "p142-159" })
    Check(!DrawingFileSearcher.MatchesSearchKey(name, "*p*142159*"), "False match " + name);
Check(DrawingFileSearcher.MatchesSearchKey("drawing-064010-A", "064010"), "Legacy search failed");
Check(!DrawingFileSearcher.MatchesSearchKey("drawing-064011-A", "064010"), "Legacy false match");
Check(DrawingFileSearcher.MatchesSearchKey("drawing-001234567-revA", "001234567"), "Nine-digit search failed");
Check(DrawingFileSearcher.MatchesSearchKey("1-204.642.697", "642697"), "Dotted filename failed");
Check(DrawingFileSearcher.MatchesSearchKey("082 978 200", "082978200"), "Spaced filename failed");

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
var folder = Path.Combine(Path.GetTempPath(), "DrawingSearchChecks-" + Guid.NewGuid());
Directory.CreateDirectory(folder);
try
{
    var path = Path.Combine(folder, "fixture.xlsx");
    using (var package = new ExcelPackage())
    {
        var sheet = package.Workbook.Worksheets.Add("BOM");
        sheet.Cells[1, 5].Value = "p999999"; // E1 must be ignored.
        sheet.Cells[4, 5].Value = "123456789"; // E4 must be ignored.
        sheet.Cells[5, 5].Value = "p142159";
        sheet.Cells[6, 5].Value = " P142159 ";
        sheet.Cells[7, 5].Value = "4-104 064 010";
        sheet.Cells[8, 5].Value = "001234567";
        sheet.Cells[9, 5].Value = "1-204.642.697";
        sheet.Cells[10, 5].Value = "082 978 200";
        sheet.Cells[11, 5].Value = "082978200"; // Same drawing, different format.
        package.SaveAs(new FileInfo(path));
    }
    var items = new BomReader().Read(path);
    Check(items.Count == 5, "Import/deduplication or E5 boundary failed");
    Check(items[0].RowNumber == 5, "Rows before E5 were imported");
    Check(items[0].SearchKey == "*p*142159*", "Wrong P key");
    Check(items[1].SearchKey == "064010", "Wrong legacy key");
    Check(items[2].SearchKey == "001234567", "Wrong nine-digit key");
    Check(items[3].SearchKey == "642697", "Wrong dotted drawing key");
    Check(items[4].SearchKey == "082978200", "Wrong grouped nine-digit key");
    foreach (var file in new[] { "P-142159.pdf", "prefix-p-extra-142159.dwg", "142159.tif", "142159p.tiff", "p142159.txt", "drawing064010.tif", "part-001234567-revA.pdf", "1-204.642.697.pdf", "082 978 200.dwg" })
        File.WriteAllText(Path.Combine(folder, file), "");
    await new DrawingFileSearcher().SearchAsync(new[] { folder }, items, null, CancellationToken.None);
    Check(items[0].MatchedFiles.Count == 2, "P search integration failed");
    Check(items[1].MatchedFiles.Count == 1, "Legacy search integration failed");
    Check(items[2].MatchedFiles.Count == 1, "Nine-digit search integration failed");
    Check(items[3].MatchedFiles.Count == 1, "Dotted drawing search integration failed");
    Check(items[4].MatchedFiles.Count == 1, "Grouped nine-digit search integration failed");
    if (args.Length > 0)
    {
        var actual = new BomReader().Read(args[0]);
        Check(actual.Count == 48, "Expected 48 drawings from Book5, got " + actual.Count);
        Check(actual.Any(i => i.SearchKey == "*p*7130056*"), "Seven-digit drawing lost");
        Console.WriteLine("Book5.xlsx: 48 drawings loaded successfully.");
    }
    Console.WriteLine("All import and search checks passed.");
}
finally
{
    Directory.Delete(folder, true);
}

