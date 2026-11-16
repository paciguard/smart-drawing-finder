using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DrawingSearchTools.Models;
using DrawingSearchTools.Services;

namespace DrawingSearchTools;

public partial class MainForm : Form
{
    private readonly BomReader _bomReader = new();
    private readonly DrawingFileSearcher _searcher = new();
    private readonly ResultExporter _exporter = new();

    private List<DrawingItem> _items = new();
    private CancellationTokenSource? _cts;
    private bool _chinese = false;   // false = English, true = Chinese

    public MainForm()
    {
        InitializeComponent();

        // Load logo
        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
        if (File.Exists(logoPath))
        {
            try { picLogo.Image = System.Drawing.Image.FromFile(logoPath); }
            catch { /* ignore if logo missing */ }
        }

        // Sensible defaults
        string defaultOut = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MatchedDrawings");
        txtOutputFolder.Text = defaultOut;

        ApplyLanguage();
    }

    // ---------- Language toggle ----------

    private void BtnLang_Click(object? sender, EventArgs e)
    {
        _chinese = !_chinese;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        btnLang.Text = _chinese ? "EN" : "中文";

        if (_chinese)
        {
            Text           = "图纸智能查找工具";
            lblTitle.Text  = "图纸智能查找工具";
            lblBom.Text    = "BOM 文件：";
            lblRoot.Text   = "搜索根目录：";
            lblOutput.Text = "输出目录：";

            btnBrowseBom.Text    = "浏览…";
            btnBrowseRoot.Text   = "浏览…";
            btnBrowseOutput.Text = "浏览…";
            btnLoad.Text         = "加载图纸";
            btnStart.Text        = "开始搜索";
            btnStop.Text         = "停  止";
            btnExportExcel.Text  = "导出 Excel 报表";
            btnCopyFiles.Text    = "复制匹配图纸";

            colRow.HeaderText         = "行号";
            colPosition.HeaderText    = "位置号";
            colDrawing.HeaderText     = "图纸编号";
            colKey.HeaderText         = "关键字";
            colDescription.HeaderText = "描述";
            colStatus.HeaderText      = "状态";
            colMatches.HeaderText     = "#";
            colFirstPath.HeaderText   = "匹配路径（右键查看更多）";

            miOpenFile.Text   = "打开文件";
            miOpenFolder.Text = "打开所在目录";
            miCopyPath.Text   = "复制路径";

            if (lblStatus.Text.StartsWith("Ready"))
                lblStatus.Text = "就绪。";
        }
        else
        {
            Text           = "Smart Drawing Finder  |  图纸智能查找工具";
            lblTitle.Text  = "Smart Drawing Finder  |  图纸智能查找工具";
            lblBom.Text    = "BOM File / BOM文件:";
            lblRoot.Text   = "Search Root / 搜索根目录:";
            lblOutput.Text = "Output Folder / 输出目录:";

            btnBrowseBom.Text    = "Browse… / 浏览";
            btnBrowseRoot.Text   = "Browse… / 浏览";
            btnBrowseOutput.Text = "Browse… / 浏览";
            btnLoad.Text         = "Load / 加载";
            btnStart.Text        = "Start / 开始搜索";
            btnStop.Text         = "Stop / 停止";
            btnExportExcel.Text  = "Export Excel / 导出报表";
            btnCopyFiles.Text    = "Copy Files / 复制图纸";

            colRow.HeaderText         = "Row / 行号";
            colPosition.HeaderText    = "Pos. / 位置号";
            colDrawing.HeaderText     = "Drawing No. / 图纸编号";
            colKey.HeaderText         = "Key / 关键字";
            colDescription.HeaderText = "Description / 描述";
            colStatus.HeaderText      = "Status / 状态";
            colMatches.HeaderText     = "#";
            colFirstPath.HeaderText   = "Matched Path / 匹配路径（右键查看更多）";

            miOpenFile.Text   = "Open File / 打开文件";
            miOpenFolder.Text = "Open Folder / 打开目录";
            miCopyPath.Text   = "Copy Path / 复制路径";

            if (lblStatus.Text == "就绪。")
                lblStatus.Text = "Ready. / 就绪。";
        }
    }

    private string T(string en, string zh) => _chinese ? zh : en;

    // ---------- Browse handlers ----------

    private void BtnBrowseBom_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select BOM Excel file",
            Filter = "Excel files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*",
        };
        if (!string.IsNullOrWhiteSpace(txtBomPath.Text) &&
            File.Exists(txtBomPath.Text))
        {
            dlg.InitialDirectory = Path.GetDirectoryName(txtBomPath.Text);
            dlg.FileName = Path.GetFileName(txtBomPath.Text);
        }
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            txtBomPath.Text = dlg.FileName;
        }
    }

    private void BtnBrowseRoot_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select the root folder to search",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(txtSearchRoot.Text)
                ? txtSearchRoot.Text
                : @"E:\",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            txtSearchRoot.Text = dlg.SelectedPath;
        }
    }

    private void BtnBrowseOutput_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select an output folder for copied drawings / Excel report",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(txtOutputFolder.Text)
                ? txtOutputFolder.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            txtOutputFolder.Text = dlg.SelectedPath;
        }
    }

    // ---------- Load BOM ----------

    private void BtnLoad_Click(object? sender, EventArgs e)
    {
        var path = txtBomPath.Text.Trim();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            MessageBox.Show(this, "Please choose a valid BOM Excel file first.",
                "Smart Drawing Finder", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            _items = _bomReader.Read(path);
            RefreshGrid();
            lblStatus.Text = T(
            $"Loaded {_items.Count} unique drawing number(s) from BOM.",
            $"已从 BOM 加载 {_items.Count} 个图纸编号。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Failed to read BOM:\r\n" + ex.Message,
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    // ---------- Search ----------

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_items.Count == 0)
        {
            // Try loading first if BOM path is set
            if (!string.IsNullOrWhiteSpace(txtBomPath.Text) &&
                File.Exists(txtBomPath.Text))
            {
                BtnLoad_Click(this, EventArgs.Empty);
            }
            if (_items.Count == 0)
            {
                MessageBox.Show(this,
                    "No drawing numbers loaded. Please load a BOM file first.",
                    "Smart Drawing Finder", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
        }

        string root = txtSearchRoot.Text.Trim();
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            MessageBox.Show(this, "Please choose a valid search root folder.",
                "Smart Drawing Finder", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        _cts = new CancellationTokenSource();

        var progress = new Progress<SearchProgress>(p =>
        {
            lblStatus.Text = p.IsFinal
                ? T("Search complete.", "搜索完成。")
                : T("Scanning: ", "正在扫描：") + Truncate(p.CurrentPath, 90);
            lblCounters.Text = T(
                $"Folders: {p.FoldersScanned:N0}   Files: {p.FilesScanned:N0}",
                $"目录：{p.FoldersScanned:N0}   文件：{p.FilesScanned:N0}");
        });

        try
        {
            await _searcher.SearchAsync(
                new[] { root }, _items, progress, _cts.Token);

            RefreshGrid();

            int found   = _items.Count(i => i.MatchedFiles.Count > 0);
            int multi   = _items.Count(i => i.MatchedFiles.Count > 1);
            int missing = _items.Count - found;

            lblStatus.Text = T(
                $"Done. {found} matched, {missing} not found, {multi} with multiple matches (duplicates removed).",
                $"完成。{found} 个匹配，{missing} 个未找到，{multi} 个多重匹配（已去重）。");
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = T("Search cancelled.", "搜索已取消。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Search failed:\r\n" + ex.Message,
                "Smart Drawing Finder", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        lblStatus.Text = "Cancelling…";
    }

    // ---------- Export & copy ----------

    private void BtnExportExcel_Click(object? sender, EventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show(this, "Nothing to export. Load a BOM and run a search first.",
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "Save search results as Excel",
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            FileName = "DrawingSearchResults_" +
                       DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx",
            InitialDirectory = Directory.Exists(txtOutputFolder.Text)
                ? txtOutputFolder.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Cursor = Cursors.WaitCursor;
            _exporter.ExportToExcel(_items, dlg.FileName);
            lblStatus.Text = T("Results exported to ", "报表已导出至 ") + dlg.FileName;

            if (MessageBox.Show(this,
                    "Excel saved. Open it now?",
                    "Smart Drawing Finder",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                OpenWithDefaultApp(dlg.FileName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Export failed:\r\n" + ex.Message,
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void BtnCopyFiles_Click(object? sender, EventArgs e)
    {
        int total = _items.Sum(i => i.MatchedFiles.Count);
        if (total == 0)
        {
            MessageBox.Show(this, "No matched files to copy. Run a search first.",
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string folder = txtOutputFolder.Text.Trim();
        if (string.IsNullOrEmpty(folder))
        {
            MessageBox.Show(this, "Please choose an output folder first.",
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(this,
                $"Copy {total} file(s) to:\r\n{folder} ?",
                "Smart Drawing Finder",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            var report = _exporter.CopyFiles(_items, folder);
            string msg = $"Copied {report.Copied} file(s) to:\r\n{folder}";
            if (report.Errors.Count > 0)
                msg += $"\r\n\r\n{report.Errors.Count} error(s):\r\n" +
                       string.Join("\r\n", report.Errors.Take(10));

            MessageBox.Show(this, msg, "Smart Drawing Finder",
                MessageBoxButtons.OK,
                report.Errors.Count == 0 ? MessageBoxIcon.Information
                                         : MessageBoxIcon.Warning);
            lblStatus.Text = T($"Copied {report.Copied} file(s) to output folder.",
                              $"已复制 {report.Copied} 个文件到输出目录。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Copy failed:\r\n" + ex.Message,
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    // ---------- Grid binding & visuals ----------

    private void RefreshGrid()
    {
        grid.SuspendLayout();
        grid.Rows.Clear();
        foreach (var item in _items)
        {
            int idx = grid.Rows.Add(
                item.RowNumber,
                item.PositionNumber ?? string.Empty,
                item.DrawingNumber,
                item.SearchKey,
                item.Description ?? string.Empty,
                item.Status,
                item.MatchedFiles.Count,
                item.MatchedFiles.FirstOrDefault() ?? string.Empty);
            grid.Rows[idx].Tag = item;
        }
        grid.ResumeLayout();
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var item = grid.Rows[e.RowIndex].Tag as DrawingItem;
        if (item == null) return;

        var row = grid.Rows[e.RowIndex];
        if (item.MatchedFiles.Count == 0)
        {
            row.DefaultCellStyle.BackColor = System.Drawing.Color.MistyRose;
            row.DefaultCellStyle.ForeColor = System.Drawing.Color.Maroon;
        }
        else if (item.MatchedFiles.Count > 1)
        {
            row.DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow;
            row.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkOrange;
        }
        else
        {
            row.DefaultCellStyle.BackColor = System.Drawing.Color.Honeydew;
            row.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkGreen;
        }
    }

    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var item = grid.Rows[e.RowIndex].Tag as DrawingItem;
        if (item == null) return;

        if (item.MatchedFiles.Count == 0)
        {
            MessageBox.Show(this,
                $"No file matched for drawing {item.DrawingNumber}.",
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // If exactly one, open it. If multiple, open the first; user can use
        // the context menu to pick a different one.
        OpenWithDefaultApp(item.MatchedFiles[0]);
    }

    private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
        {
            grid.ClearSelection();
            grid.Rows[e.RowIndex].Selected = true;
            grid.CurrentCell = grid.Rows[e.RowIndex].Cells[Math.Max(e.ColumnIndex, 0)];

            // Rebuild the menu so we show one entry per matched file.
            var item = grid.Rows[e.RowIndex].Tag as DrawingItem;
            gridMenu.Items.Clear();

            if (item != null && item.MatchedFiles.Count > 0)
            {
                if (item.MatchedFiles.Count == 1)
                {
                    miOpenFile.Tag = item.MatchedFiles[0];
                    miOpenFolder.Tag = item.MatchedFiles[0];
                    miCopyPath.Tag = item.MatchedFiles[0];
                    gridMenu.Items.Add(miOpenFile);
                    gridMenu.Items.Add(miOpenFolder);
                    gridMenu.Items.Add(miCopyPath);
                }
                else
                {
                    foreach (var file in item.MatchedFiles)
                    {
                        var label = Path.GetFileName(file);
                        var open = new ToolStripMenuItem("Open: " + label) { Tag = file };
                        open.Click += MiOpenFile_Click;
                        gridMenu.Items.Add(open);

                        var fold = new ToolStripMenuItem("Folder of: " + label) { Tag = file };
                        fold.Click += MiOpenFolder_Click;
                        gridMenu.Items.Add(fold);

                        gridMenu.Items.Add(new ToolStripSeparator());
                    }
                }
            }
            else
            {
                var none = new ToolStripMenuItem("No matches") { Enabled = false };
                gridMenu.Items.Add(none);
            }
        }
    }

    private void MiOpenFile_Click(object? sender, EventArgs e)
    {
        string? path = (sender as ToolStripMenuItem)?.Tag as string;
        if (!string.IsNullOrEmpty(path)) OpenWithDefaultApp(path);
    }

    private void MiOpenFolder_Click(object? sender, EventArgs e)
    {
        string? path = (sender as ToolStripMenuItem)?.Tag as string;
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open folder:\r\n" + ex.Message,
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MiCopyPath_Click(object? sender, EventArgs e)
    {
        string? path = (sender as ToolStripMenuItem)?.Tag as string;
        if (!string.IsNullOrEmpty(path))
        {
            Clipboard.SetText(path);
            lblStatus.Text = T("Path copied to clipboard.", "路径已复制到剪贴板。");
        }
    }

    // ---------- Helpers ----------

    private void SetBusy(bool busy)
    {
        btnStart.Enabled       = !busy;
        btnLoad.Enabled        = !busy;
        btnBrowseBom.Enabled   = !busy;
        btnBrowseRoot.Enabled  = !busy;
        btnBrowseOutput.Enabled = !busy;
        btnExportExcel.Enabled = !busy;
        btnCopyFiles.Enabled   = !busy;
        btnStop.Enabled        = busy;

        progressBar.Visible = busy;
        progressBar.MarqueeAnimationSpeed = busy ? 30 : 0;
    }

    private static void OpenWithDefaultApp(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not open file:\r\n" + ex.Message,
                "Smart Drawing Finder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty
           : s.Length <= max ? s
           : "…" + s.Substring(s.Length - max + 1);
}

