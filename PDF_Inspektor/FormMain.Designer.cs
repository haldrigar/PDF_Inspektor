namespace PDF_Inspektor_OLD;

partial class FormMain
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (this.components != null))
        {
            this.components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Syncfusion.Windows.Forms.PdfViewer.MessageBoxSettings messageBoxSettings1 = new Syncfusion.Windows.Forms.PdfViewer.MessageBoxSettings();
        Syncfusion.Windows.PdfViewer.PdfViewerPrinterSettings pdfViewerPrinterSettings1 = new Syncfusion.Windows.PdfViewer.PdfViewerPrinterSettings();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
        Syncfusion.Windows.Forms.PdfViewer.TextSearchSettings textSearchSettings1 = new Syncfusion.Windows.Forms.PdfViewer.TextSearchSettings();
        this.tableLayoutPanel = new TableLayoutPanel();
        this.groupBoxFileList = new GroupBox();
        this.listBoxFiles = new ListBox();
        this.groupBoxFileOperations = new GroupBox();
        this.groupBoxOptions = new GroupBox();
        this.groupBoxPDFViewer = new GroupBox();
        this.pdfViewerControl = new Syncfusion.Windows.Forms.PdfViewer.PdfViewerControl();
        this.statusStrip = new StatusStrip();
        this.toolStripStatusLabel = new ToolStripStatusLabel();
        this.tableLayoutPanel.SuspendLayout();
        this.groupBoxFileList.SuspendLayout();
        this.groupBoxPDFViewer.SuspendLayout();
        this.statusStrip.SuspendLayout();
        this.SuspendLayout();
        // 
        // tableLayoutPanel
        // 
        this.tableLayoutPanel.ColumnCount = 2;
        this.tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this.tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400F));
        this.tableLayoutPanel.Controls.Add(this.groupBoxFileList, 1, 0);
        this.tableLayoutPanel.Controls.Add(this.groupBoxFileOperations, 0, 1);
        this.tableLayoutPanel.Controls.Add(this.groupBoxOptions, 1, 1);
        this.tableLayoutPanel.Controls.Add(this.groupBoxPDFViewer, 0, 0);
        this.tableLayoutPanel.Controls.Add(this.statusStrip, 0, 2);
        this.tableLayoutPanel.Dock = DockStyle.Fill;
        this.tableLayoutPanel.Location = new Point(0, 0);
        this.tableLayoutPanel.Name = "tableLayoutPanel";
        this.tableLayoutPanel.RowCount = 3;
        this.tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        this.tableLayoutPanel.RowStyles.Add(new RowStyle());
        this.tableLayoutPanel.Size = new Size(1184, 821);
        this.tableLayoutPanel.TabIndex = 0;
        // 
        // groupBoxFileList
        // 
        this.groupBoxFileList.Controls.Add(this.listBoxFiles);
        this.groupBoxFileList.Dock = DockStyle.Fill;
        this.groupBoxFileList.Location = new Point(787, 3);
        this.groupBoxFileList.Name = "groupBoxFileList";
        this.groupBoxFileList.Size = new Size(394, 693);
        this.groupBoxFileList.TabIndex = 1;
        this.groupBoxFileList.TabStop = false;
        this.groupBoxFileList.Text = "Lista plików";
        // 
        // listBoxFiles
        // 
        this.listBoxFiles.AllowDrop = true;
        this.listBoxFiles.Dock = DockStyle.Fill;
        this.listBoxFiles.FormattingEnabled = true;
        this.listBoxFiles.ItemHeight = 15;
        this.listBoxFiles.Location = new Point(3, 19);
        this.listBoxFiles.Name = "listBoxFiles";
        this.listBoxFiles.Size = new Size(388, 671);
        this.listBoxFiles.TabIndex = 0;
        this.listBoxFiles.SelectedIndexChanged += this.ListBoxFiles_SelectedIndexChanged;
        this.listBoxFiles.DragDrop += this.ListBoxFiles_DragDrop;
        this.listBoxFiles.DragEnter += this.ListBoxFiles_DragEnter;
        // 
        // groupBoxFileOperations
        // 
        this.groupBoxFileOperations.Dock = DockStyle.Right;
        this.groupBoxFileOperations.Location = new Point(210, 702);
        this.groupBoxFileOperations.Name = "groupBoxFileOperations";
        this.groupBoxFileOperations.Size = new Size(571, 94);
        this.groupBoxFileOperations.TabIndex = 2;
        this.groupBoxFileOperations.TabStop = false;
        this.groupBoxFileOperations.Text = "Operacje na pliku";
        // 
        // groupBoxOptions
        // 
        this.groupBoxOptions.Dock = DockStyle.Fill;
        this.groupBoxOptions.Location = new Point(787, 702);
        this.groupBoxOptions.Name = "groupBoxOptions";
        this.groupBoxOptions.Size = new Size(394, 94);
        this.groupBoxOptions.TabIndex = 3;
        this.groupBoxOptions.TabStop = false;
        this.groupBoxOptions.Text = "Opcje";
        // 
        // groupBoxPDFViewer
        // 
        this.groupBoxPDFViewer.Controls.Add(this.pdfViewerControl);
        this.groupBoxPDFViewer.Dock = DockStyle.Fill;
        this.groupBoxPDFViewer.Location = new Point(3, 3);
        this.groupBoxPDFViewer.Name = "groupBoxPDFViewer";
        this.groupBoxPDFViewer.Size = new Size(778, 693);
        this.groupBoxPDFViewer.TabIndex = 0;
        this.groupBoxPDFViewer.TabStop = false;
        this.groupBoxPDFViewer.Text = "Podgląd pliku PDF";
        // 
        // pdfViewerControl
        // 
        this.pdfViewerControl.CursorMode = Syncfusion.Windows.Forms.PdfViewer.PdfViewerCursorMode.HandTool;
        this.pdfViewerControl.Dock = DockStyle.Fill;
        this.pdfViewerControl.EnableContextMenu = false;
        this.pdfViewerControl.EnableNotificationBar = false;
        this.pdfViewerControl.HorizontalScrollOffset = 0;
        this.pdfViewerControl.IsBookmarkEnabled = false;
        this.pdfViewerControl.IsTextSearchEnabled = false;
        this.pdfViewerControl.IsTextSelectionEnabled = false;
        this.pdfViewerControl.Location = new Point(3, 19);
        messageBoxSettings1.EnableNotification = true;
        this.pdfViewerControl.MessageBoxSettings = messageBoxSettings1;
        this.pdfViewerControl.MinimumZoomPercentage = 50;
        this.pdfViewerControl.Name = "pdfViewerControl";
        this.pdfViewerControl.PageBorderThickness = 5;
        pdfViewerPrinterSettings1.Copies = 1;
        pdfViewerPrinterSettings1.PageOrientation = Syncfusion.Windows.PdfViewer.PdfViewerPrintOrientation.Auto;
        pdfViewerPrinterSettings1.PageSize = Syncfusion.Windows.PdfViewer.PdfViewerPrintSize.ActualSize;
        pdfViewerPrinterSettings1.PrintLocation = (PointF)resources.GetObject("pdfViewerPrinterSettings1.PrintLocation");
        pdfViewerPrinterSettings1.ShowPrintStatusDialog = true;
        this.pdfViewerControl.PrinterSettings = pdfViewerPrinterSettings1;
        this.pdfViewerControl.ReferencePath = null;
        this.pdfViewerControl.ScrollDisplacementValue = 0;
        this.pdfViewerControl.ShowHorizontalScrollBar = true;
        this.pdfViewerControl.ShowToolBar = true;
        this.pdfViewerControl.ShowVerticalScrollBar = true;
        this.pdfViewerControl.Size = new Size(772, 671);
        this.pdfViewerControl.SpaceBetweenPages = 5;
        this.pdfViewerControl.TabIndex = 0;
        this.pdfViewerControl.TabStop = false;
        this.pdfViewerControl.Text = "pdfViewerControl";
        textSearchSettings1.CurrentInstanceColor = Color.FromArgb(127, 255, 171, 64);
        textSearchSettings1.HighlightAllInstance = true;
        textSearchSettings1.OtherInstanceColor = Color.FromArgb(127, 254, 255, 0);
        this.pdfViewerControl.TextSearchSettings = textSearchSettings1;
        this.pdfViewerControl.ThemeName = "Office2016Colorful";
        this.pdfViewerControl.VerticalScrollOffset = 0;
        this.pdfViewerControl.VisualStyle = Syncfusion.Windows.Forms.PdfViewer.VisualStyle.Office2016Colorful;
        this.pdfViewerControl.ZoomMode = Syncfusion.Windows.Forms.PdfViewer.ZoomMode.FitPage;
        this.pdfViewerControl.DocumentLoaded += this.PdfViewerControl_DocumentLoaded;
        // 
        // statusStrip
        // 
        this.tableLayoutPanel.SetColumnSpan(this.statusStrip, 2);
        this.statusStrip.Items.AddRange(new ToolStripItem[] { this.toolStripStatusLabel });
        this.statusStrip.Location = new Point(0, 799);
        this.statusStrip.Name = "statusStrip";
        this.statusStrip.Size = new Size(1184, 22);
        this.statusStrip.TabIndex = 4;
        this.statusStrip.Text = "statusStrip";
        // 
        // toolStripStatusLabel
        // 
        this.toolStripStatusLabel.Name = "toolStripStatusLabel";
        this.toolStripStatusLabel.Size = new Size(51, 17);
        this.toolStripStatusLabel.Text = "Gotowy!";
        // 
        // FormMain
        // 
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1184, 821);
        this.Controls.Add(this.tableLayoutPanel);
        this.MinimumSize = new Size(1000, 500);
        this.Name = "FormMain";
        this.Text = "GISNET PDF Inspektor";
        this.FormClosed += this.FormMain_FormClosed;
        this.Load += this.FormMain_Load;
        this.tableLayoutPanel.ResumeLayout(false);
        this.tableLayoutPanel.PerformLayout();
        this.groupBoxFileList.ResumeLayout(false);
        this.groupBoxPDFViewer.ResumeLayout(false);
        this.statusStrip.ResumeLayout(false);
        this.statusStrip.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel tableLayoutPanel;
    private GroupBox groupBoxFileList;
    private GroupBox groupBoxFileOperations;
    private GroupBox groupBoxOptions;
    private GroupBox groupBoxPDFViewer;
    private Syncfusion.Windows.Forms.PdfViewer.PdfViewerControl pdfViewerControl;
    private ListBox listBoxFiles;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel toolStripStatusLabel;
}
