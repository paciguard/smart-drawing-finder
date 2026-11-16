using System;
using System.Windows.Forms;
using OfficeOpenXml;

namespace DrawingSearchTools;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // EPPlus 5+ requires a license context. NonCommercial is used by
        // default; replace with LicenseContext.Commercial if you have an
        // EPPlus commercial license key configured.
        OfficeOpenXml.ExcelPackage.LicenseContext =
            OfficeOpenXml.LicenseContext.NonCommercial;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

