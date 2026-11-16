# Smart Drawing Finder

A small Windows desktop tool (.NET 8 / WinForms) that scans a BOM Excel
file for drawing numbers (column E, format like `4-104 064 010`) and
finds the matching `.pdf` / `.tif` / `.tiff` files anywhere under a chosen
search root (e.g. `E:\`).

## How it works

P-prefixed drawing numbers in column E are also supported, ignoring case and
surrounding spaces. For example, `p142159` uses the filename pattern
`*p*142159*`, matching `P142159.pdf` or `P-142159-revA.dwg`.
All digits are preserved, including seven-digit identifiers such as `p7130056`.
The existing numbered format continues to use its last six digits.
Nine-digit drawing numbers without a prefix, such as `001234567`, search
for that complete sequence of digits in the filename.
The reader starts at cell E5. It also accepts dot-separated drawings such as
`1-204.642.697` (search key `642697`) and grouped nine-digit numbers such as
`082 978 200` (search key `082978200`).
Supported drawing file types are `.dwg`, `.pdf`, `.tif`, and `.tiff`.

1. **Load BOM** – the tool reads every row of column E in the chosen
   `.xlsx`. Only entries matching the strict drawing-number pattern
   (`<digit>-<3 digits> <3 digits> <3 digits>`) are kept. Standard part
   references such as `GB/T898-1988` or `SB1101-00` are skipped
   automatically.
2. **Build search key** – the last six digits are concatenated, e.g.
   `4-104 064 010` → search key `064010`.
3. **Search** – the tool walks the search root recursively. Any file
   whose name *contains* the key (case-insensitive) and whose extension
   is `.pdf`, `.tif` or `.tiff` is recorded as a match. Permission-denied
   folders, junctions and known system folders (`$RECYCLE.BIN`, etc.)
   are skipped safely.
4. **Review** – results appear in a grid; green = exactly one match,
   yellow = multiple matches, red = nothing found. Double-click to open
   a file, right-click for more options.
5. **Export / Copy** – save the result table as a new Excel workbook
   and/or copy every matched drawing into a single output folder.

## Build

Requires the .NET 8 SDK and Windows. To build an installable setup, install
Inno Setup 6 and run this from the project root:

```bat
build_installer.bat
```

This rebuilds the current `DrawingSearchTools\DrawingSearchTools.csproj`,
publishes it to `publish`, then creates
`installer_output\SmartDrawingFinder_Setup_v<version>.exe`, using the version
from the application project. The installer is made from the same application
build. Run `build_install.bat` to
refresh only the published application.

For a normal development build:

```bat
dotnet build DrawingSearchTools\DrawingSearchTools.csproj -c Release
```

## Notes

* Excel I/O uses [EPPlus](https://www.epplussoftware.com/) – no
  Microsoft Office or Excel install required. EPPlus is dual-licensed
  (Polyform Non-Commercial / Commercial). The app sets
  `LicenseContext.NonCommercial` at start-up; if you use this tool in a
  commercial setting, please purchase an EPPlus commercial license and
  change the context in `Program.cs`.
* The first run on a large E:\ drive can take a while because every
  folder must be enumerated. Subsequent runs benefit from the OS file
  cache.

