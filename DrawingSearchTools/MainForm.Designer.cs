using System.Windows.Forms;

namespace DrawingSearchTools;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    // Header
    private Panel      pnlHeader   = null!;
    private PictureBox picLogo     = null!;
    private Label      lblTitle    = null!;
    private Button     btnLang     = null!;

    // Top configuration panel
    private Label   lblBom     = null!;
    private TextBox txtBomPath = null!;
    private Button  btnBrowseBom = null!;

    private Label   lblRoot      = null!;
    private TextBox txtSearchRoot = null!;
    private Button  btnBrowseRoot = null!;

    private Label   lblOutput       = null!;
    private TextBox txtOutputFolder = null!;
    private Button  btnBrowseOutput = null!;

    // Action buttons
    private Button btnLoad        = null!;
    private Button btnStart       = null!;
    private Button btnStop        = null!;
    private Button btnExportExcel = null!;
    private Button btnCopyFiles   = null!;

    // Results grid
    private DataGridView              grid           = null!;
    private DataGridViewTextBoxColumn colRow         = null!;
    private DataGridViewTextBoxColumn colPosition    = null!;
    private DataGridViewTextBoxColumn colDrawing     = null!;
    private DataGridViewTextBoxColumn colKey         = null!;
    private DataGridViewTextBoxColumn colDescription = null!;
    private DataGridViewTextBoxColumn colStatus      = null!;
    private DataGridViewTextBoxColumn colMatches     = null!;
    private DataGridViewTextBoxColumn colFirstPath   = null!;

    private ContextMenuStrip  gridMenu     = null!;
    private ToolStripMenuItem miOpenFile   = null!;
    private ToolStripMenuItem miOpenFolder = null!;
    private ToolStripMenuItem miCopyPath   = null!;

    // Status bar
    private StatusStrip          statusStrip = null!;
    private ToolStripStatusLabel lblStatus   = null!;
    private ToolStripProgressBar progressBar = null!;
    private ToolStripStatusLabel lblCounters = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources =
            new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

        // ── Instantiate ──────────────────────────────────────────────────────
        pnlHeader       = new Panel();
        picLogo         = new PictureBox();
        lblTitle        = new Label();
        btnLang         = new Button();
        lblBom          = new Label();
        txtBomPath      = new TextBox();
        btnBrowseBom    = new Button();
        btnLoad         = new Button();
        lblRoot         = new Label();
        txtSearchRoot   = new TextBox();
        btnBrowseRoot   = new Button();
        btnStart        = new Button();
        lblOutput       = new Label();
        txtOutputFolder = new TextBox();
        btnBrowseOutput = new Button();
        btnStop         = new Button();
        btnExportExcel  = new Button();
        btnCopyFiles    = new Button();
        colRow          = new DataGridViewTextBoxColumn();
        colPosition     = new DataGridViewTextBoxColumn();
        colDrawing      = new DataGridViewTextBoxColumn();
        colKey          = new DataGridViewTextBoxColumn();
        colDescription  = new DataGridViewTextBoxColumn();
        colStatus       = new DataGridViewTextBoxColumn();
        colMatches      = new DataGridViewTextBoxColumn();
        colFirstPath    = new DataGridViewTextBoxColumn();
        grid            = new DataGridView();
        gridMenu        = new ContextMenuStrip(components);
        miOpenFile      = new ToolStripMenuItem();
        miOpenFolder    = new ToolStripMenuItem();
        miCopyPath      = new ToolStripMenuItem();
        statusStrip     = new StatusStrip();
        lblStatus       = new ToolStripStatusLabel();
        progressBar     = new ToolStripProgressBar();
        lblCounters     = new ToolStripStatusLabel();

        pnlHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        gridMenu.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        // ── Layout constants ─────────────────────────────────────────────────
        // ClientSize 1180 × 760
        // Header   y=0   h=56
        // Row 1    y=62  (BOM)
        // Row 2    y=98  (Root)
        // Row 3    y=134 (Output)
        // Row 4    y=170 (Export/Copy)
        // Grid     y=206 h≈530
        // Status   y=738

        const int PAD    = 12;   // left/right margin
        const int LW     = 140;  // label width
        const int BH     = 27;   // control height
        const int RH     = 36;   // row pitch
        const int BTN_W  = 96;   // Browse button width
        const int ACT_W  = 110;  // Load/Start/Stop width
        const int WIDE_W = 150;  // Export/Copy width

        const int formW = 1180;
        const int rowY1 = 62;
        const int rowY2 = rowY1 + RH;
        const int rowY3 = rowY2 + RH;
        const int rowY4 = rowY3 + RH;
        const int gridY = rowY4 + RH + 4;
        const int gridH = 736 - gridY;

        // x-positions (right to left)
        const int actX = formW - PAD - ACT_W;           // action btn x
        const int brwX = actX - 4 - BTN_W;             // browse btn x
        const int txtX = PAD + LW + 4;                 // textbox x
        const int txtW = brwX - 4 - txtX;              // textbox width

        // ── pnlHeader ────────────────────────────────────────────────────────
        pnlHeader.BackColor = Color.FromArgb(28, 57, 135);
        pnlHeader.Controls.Add(picLogo);
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(btnLang);
        pnlHeader.Location = new Point(0, 0);
        pnlHeader.Name     = "pnlHeader";
        pnlHeader.Size     = new Size(formW, 56);
        pnlHeader.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pnlHeader.TabIndex = 0;

        // ── picLogo ───────────────────────────────────────────────────────────
        picLogo.Location = new Point(8, 5);
        picLogo.Name     = "picLogo";
        picLogo.Size     = new Size(90, 46);   // compact: 90×46
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.TabIndex = 0;
        picLogo.TabStop  = false;

        // ── lblTitle ──────────────────────────────────────────────────────────
        lblTitle.AutoSize  = false;
        lblTitle.ForeColor = Color.White;
        lblTitle.Font      = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblTitle.Location  = new Point(106, 10);
        lblTitle.Name      = "lblTitle";
        lblTitle.Size      = new Size(950, 36);
        lblTitle.TabIndex  = 1;
        lblTitle.Text      = "Smart Drawing Finder  |  图纸智能查找工具";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;

        // ── btnLang ───────────────────────────────────────────────────────────
        btnLang.FlatStyle                  = FlatStyle.Flat;
        btnLang.FlatAppearance.BorderColor = Color.White;
        btnLang.ForeColor                  = Color.White;
        btnLang.BackColor                  = Color.FromArgb(50, 80, 160);
        btnLang.Font                       = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnLang.Location  = new Point(formW - 94, 14);
        btnLang.Name      = "btnLang";
        btnLang.Size      = new Size(82, 28);
        btnLang.TabIndex  = 2;
        btnLang.Text      = "中文";
        btnLang.Click    += BtnLang_Click;

        // ── Row 1 – BOM ───────────────────────────────────────────────────────
        lblBom.AutoSize  = false;
        lblBom.Location  = new Point(PAD, rowY1 + 3);
        lblBom.Name      = "lblBom";
        lblBom.Size      = new Size(LW, 23);
        lblBom.TabIndex  = 3;
        lblBom.Text      = "BOM File / BOM文件:";
        lblBom.TextAlign = ContentAlignment.MiddleRight;

        txtBomPath.Location = new Point(txtX, rowY1);
        txtBomPath.Name     = "txtBomPath";
        txtBomPath.Size     = new Size(txtW, BH);
        txtBomPath.TabIndex = 4;

        btnBrowseBom.Location = new Point(brwX, rowY1);
        btnBrowseBom.Name     = "btnBrowseBom";
        btnBrowseBom.Size     = new Size(BTN_W, BH);
        btnBrowseBom.TabIndex = 5;
        btnBrowseBom.Text     = "Browse…";
        btnBrowseBom.Click   += BtnBrowseBom_Click;

        btnLoad.Location = new Point(actX, rowY1);
        btnLoad.Name     = "btnLoad";
        btnLoad.Size     = new Size(ACT_W, BH);
        btnLoad.TabIndex = 6;
        btnLoad.Text     = "Load / 加载";
        btnLoad.Click   += BtnLoad_Click;

        // ── Row 2 – Search Root ───────────────────────────────────────────────
        lblRoot.AutoSize  = false;
        lblRoot.Location  = new Point(PAD, rowY2 + 3);
        lblRoot.Name      = "lblRoot";
        lblRoot.Size      = new Size(LW, 23);
        lblRoot.TabIndex  = 7;
        lblRoot.Text      = "Search Root / 搜索根目录:";
        lblRoot.TextAlign = ContentAlignment.MiddleRight;

        txtSearchRoot.Location = new Point(txtX, rowY2);
        txtSearchRoot.Name     = "txtSearchRoot";
        txtSearchRoot.Size     = new Size(txtW, BH);
        txtSearchRoot.TabIndex = 8;

        btnBrowseRoot.Location = new Point(brwX, rowY2);
        btnBrowseRoot.Name     = "btnBrowseRoot";
        btnBrowseRoot.Size     = new Size(BTN_W, BH);
        btnBrowseRoot.TabIndex = 9;
        btnBrowseRoot.Text     = "Browse…";
        btnBrowseRoot.Click   += BtnBrowseRoot_Click;

        btnStart.Location  = new Point(actX, rowY2);
        btnStart.Name      = "btnStart";
        btnStart.Size      = new Size(ACT_W, BH);
        btnStart.TabIndex  = 10;
        btnStart.Text      = "Start / 开始";
        btnStart.BackColor = Color.FromArgb(0, 122, 204);
        btnStart.ForeColor = Color.White;
        btnStart.FlatStyle = FlatStyle.Flat;
        btnStart.Click    += BtnStart_Click;

        // ── Row 3 – Output ────────────────────────────────────────────────────
        lblOutput.AutoSize  = false;
        lblOutput.Location  = new Point(PAD, rowY3 + 3);
        lblOutput.Name      = "lblOutput";
        lblOutput.Size      = new Size(LW, 23);
        lblOutput.TabIndex  = 11;
        lblOutput.Text      = "Output Folder / 输出目录:";
        lblOutput.TextAlign = ContentAlignment.MiddleRight;

        txtOutputFolder.Location = new Point(txtX, rowY3);
        txtOutputFolder.Name     = "txtOutputFolder";
        txtOutputFolder.Size     = new Size(txtW, BH);
        txtOutputFolder.TabIndex = 12;

        btnBrowseOutput.Location = new Point(brwX, rowY3);
        btnBrowseOutput.Name     = "btnBrowseOutput";
        btnBrowseOutput.Size     = new Size(BTN_W, BH);
        btnBrowseOutput.TabIndex = 13;
        btnBrowseOutput.Text     = "Browse…";
        btnBrowseOutput.Click   += BtnBrowseOutput_Click;

        btnStop.Location  = new Point(actX, rowY3);
        btnStop.Name      = "btnStop";
        btnStop.Size      = new Size(ACT_W, BH);
        btnStop.TabIndex  = 14;
        btnStop.Text      = "Stop / 停止";
        btnStop.BackColor = Color.FromArgb(200, 50, 50);
        btnStop.ForeColor = Color.White;
        btnStop.FlatStyle = FlatStyle.Flat;
        btnStop.Enabled   = false;
        btnStop.Click    += BtnStop_Click;

        // ── Row 4 – Copy / Export ─────────────────────────────────────────────
        btnCopyFiles.Location  = new Point(brwX - 4 - WIDE_W, rowY4);
        btnCopyFiles.Name      = "btnCopyFiles";
        btnCopyFiles.Size      = new Size(WIDE_W, BH);
        btnCopyFiles.TabIndex  = 15;
        btnCopyFiles.Text      = "Copy Files / 复制图纸";
        btnCopyFiles.Click    += BtnCopyFiles_Click;

        btnExportExcel.Location  = new Point(brwX, rowY4);
        btnExportExcel.Name      = "btnExportExcel";
        btnExportExcel.Size      = new Size(BTN_W + 4 + ACT_W, BH);
        btnExportExcel.TabIndex  = 16;
        btnExportExcel.Text      = "Export Excel / 导出报表";
        btnExportExcel.BackColor = Color.FromArgb(33, 115, 70);
        btnExportExcel.ForeColor = Color.White;
        btnExportExcel.FlatStyle = FlatStyle.Flat;
        btnExportExcel.Click    += BtnExportExcel_Click;

        // ── Grid columns ──────────────────────────────────────────────────────
        colRow.Name       = "colRow";
        colRow.HeaderText = "Row / 行号";
        colRow.Width      = 46;
        colRow.ReadOnly   = true;

        colPosition.Name       = "colPosition";
        colPosition.HeaderText = "Pos. / 位置号";
        colPosition.Width      = 80;
        colPosition.ReadOnly   = true;

        colDrawing.Name       = "colDrawing";
        colDrawing.HeaderText = "Drawing No. / 图纸编号";
        colDrawing.Width      = 160;
        colDrawing.ReadOnly   = true;

        colKey.Name       = "colKey";
        colKey.HeaderText = "Key / 关键字";
        colKey.Width      = 160;
        colKey.ReadOnly   = true;

        colDescription.Name       = "colDescription";
        colDescription.HeaderText = "Description / 描述";
        colDescription.Width      = 200;
        colDescription.ReadOnly   = true;

        colStatus.Name       = "colStatus";
        colStatus.HeaderText = "Status / 状态";
        colStatus.Width      = 80;
        colStatus.ReadOnly   = true;

        colMatches.Name       = "colMatches";
        colMatches.HeaderText = "#";
        colMatches.Width      = 36;
        colMatches.ReadOnly   = true;

        colFirstPath.Name         = "colFirstPath";
        colFirstPath.HeaderText   = "Matched Path / 匹配路径（右键查看更多）";
        colFirstPath.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colFirstPath.ReadOnly     = true;

        // ── Grid ──────────────────────────────────────────────────────────────
        grid.AllowUserToAddRows    = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly              = true;
        grid.RowHeadersVisible     = false;
        grid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle     = BorderStyle.None;
        grid.Columns.AddRange(new DataGridViewColumn[]
            { colRow, colPosition, colDrawing, colKey, colDescription,
              colStatus, colMatches, colFirstPath });
        grid.ContextMenuStrip = gridMenu;
        grid.Location         = new Point(PAD, gridY);
        grid.Name             = "grid";
        grid.Size             = new Size(formW - PAD * 2, gridH);
        grid.TabIndex         = 17;
        grid.Anchor           = AnchorStyles.Top | AnchorStyles.Bottom
                              | AnchorStyles.Left | AnchorStyles.Right;
        grid.CellDoubleClick += Grid_CellDoubleClick;
        grid.CellFormatting  += Grid_CellFormatting;
        grid.CellMouseDown   += Grid_CellMouseDown;

        // ── Context menu ──────────────────────────────────────────────────────
        miOpenFile.Name   = "miOpenFile";
        miOpenFile.Text   = "Open File / 打开文件";
        miOpenFile.Click += MiOpenFile_Click;

        miOpenFolder.Name   = "miOpenFolder";
        miOpenFolder.Text   = "Open Folder / 打开目录";
        miOpenFolder.Click += MiOpenFolder_Click;

        miCopyPath.Name   = "miCopyPath";
        miCopyPath.Text   = "Copy Path / 复制路径";
        miCopyPath.Click += MiCopyPath_Click;

        gridMenu.Items.AddRange(new ToolStripItem[]
            { miOpenFile, miOpenFolder, miCopyPath });
        gridMenu.Name = "gridMenu";

        // ── StatusStrip ───────────────────────────────────────────────────────
        lblStatus.Name      = "lblStatus";
        lblStatus.Text      = "Ready. / 就绪。";
        lblStatus.Spring    = true;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        progressBar.Name    = "progressBar";
        progressBar.Size    = new Size(180, 16);
        progressBar.Style   = ProgressBarStyle.Marquee;
        progressBar.Visible = false;

        lblCounters.Name = "lblCounters";

        statusStrip.Items.AddRange(new ToolStripItem[]
            { lblStatus, progressBar, lblCounters });
        statusStrip.Location = new Point(0, 738);
        statusStrip.Name     = "statusStrip";
        statusStrip.Size     = new Size(formW, 22);
        statusStrip.TabIndex = 18;

        // ── Form ──────────────────────────────────────────────────────────────
        BackColor     = Color.FromArgb(245, 247, 250);
        ClientSize    = new Size(formW, 760);
        Font          = new Font("Segoe UI", 9F);
        Icon          = (Icon)resources.GetObject("$this.Icon");
        MinimumSize   = new Size(900, 560);
        Name          = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text          = "Smart Drawing Finder  |  图纸智能查找工具";

        Controls.Add(pnlHeader);
        Controls.Add(lblBom);
        Controls.Add(txtBomPath);
        Controls.Add(btnBrowseBom);
        Controls.Add(btnLoad);
        Controls.Add(lblRoot);
        Controls.Add(txtSearchRoot);
        Controls.Add(btnBrowseRoot);
        Controls.Add(btnStart);
        Controls.Add(lblOutput);
        Controls.Add(txtOutputFolder);
        Controls.Add(btnBrowseOutput);
        Controls.Add(btnStop);
        Controls.Add(btnCopyFiles);
        Controls.Add(btnExportExcel);
        Controls.Add(grid);
        Controls.Add(statusStrip);

        pnlHeader.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        gridMenu.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}

