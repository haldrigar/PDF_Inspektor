namespace PDF_Inspektor;

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
        if (disposing && (components != null))
        {
            components.Dispose();
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
        tableLayoutPanel1 = new TableLayoutPanel();
        groupBoxFileList = new GroupBox();
        groupBoxFileOperations = new GroupBox();
        groupBoxOptions = new GroupBox();
        groupBoxPDFViewer = new GroupBox();
        pdfViewerControl = new Syncfusion.Windows.Forms.PdfViewer.PdfViewerControl();
        listBoxFiles = new ListBox();
        tableLayoutPanel1.SuspendLayout();
        groupBoxFileList.SuspendLayout();
        groupBoxPDFViewer.SuspendLayout();
        SuspendLayout();
        // 
        // tableLayoutPanel1
        // 
        tableLayoutPanel1.ColumnCount = 2;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400F));
        tableLayoutPanel1.Controls.Add(groupBoxFileList, 1, 0);
        tableLayoutPanel1.Controls.Add(groupBoxFileOperations, 0, 1);
        tableLayoutPanel1.Controls.Add(groupBoxOptions, 1, 1);
        tableLayoutPanel1.Controls.Add(groupBoxPDFViewer, 0, 0);
        tableLayoutPanel1.Dock = DockStyle.Fill;
        tableLayoutPanel1.Location = new Point(0, 0);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 2;
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        tableLayoutPanel1.Size = new Size(1184, 821);
        tableLayoutPanel1.TabIndex = 0;
        // 
        // groupBoxFileList
        // 
        groupBoxFileList.Controls.Add(listBoxFiles);
        groupBoxFileList.Dock = DockStyle.Fill;
        groupBoxFileList.Location = new Point(787, 3);
        groupBoxFileList.Name = "groupBoxFileList";
        groupBoxFileList.Size = new Size(394, 715);
        groupBoxFileList.TabIndex = 1;
        groupBoxFileList.TabStop = false;
        groupBoxFileList.Text = "Lista plików";
        // 
        // groupBoxFileOperations
        // 
        groupBoxFileOperations.Dock = DockStyle.Right;
        groupBoxFileOperations.Location = new Point(210, 724);
        groupBoxFileOperations.Name = "groupBoxFileOperations";
        groupBoxFileOperations.Size = new Size(571, 94);
        groupBoxFileOperations.TabIndex = 2;
        groupBoxFileOperations.TabStop = false;
        groupBoxFileOperations.Text = "Operacje na pliku";
        // 
        // groupBoxOptions
        // 
        groupBoxOptions.Dock = DockStyle.Fill;
        groupBoxOptions.Location = new Point(787, 724);
        groupBoxOptions.Name = "groupBoxOptions";
        groupBoxOptions.Size = new Size(394, 94);
        groupBoxOptions.TabIndex = 3;
        groupBoxOptions.TabStop = false;
        groupBoxOptions.Text = "Opcje";
        // 
        // groupBoxPDFViewer
        // 
        groupBoxPDFViewer.Controls.Add(pdfViewerControl);
        groupBoxPDFViewer.Dock = DockStyle.Fill;
        groupBoxPDFViewer.Location = new Point(3, 3);
        groupBoxPDFViewer.Name = "groupBoxPDFViewer";
        groupBoxPDFViewer.Size = new Size(778, 715);
        groupBoxPDFViewer.TabIndex = 0;
        groupBoxPDFViewer.TabStop = false;
        groupBoxPDFViewer.Text = "Podgląd pliku PDF";
        // 
        // pdfViewerControl
        // 
        pdfViewerControl.CursorMode = Syncfusion.Windows.Forms.PdfViewer.PdfViewerCursorMode.HandTool;
        pdfViewerControl.Dock = DockStyle.Fill;
        pdfViewerControl.EnableContextMenu = false;
        pdfViewerControl.EnableNotificationBar = false;
        pdfViewerControl.HorizontalScrollOffset = 0;
        pdfViewerControl.IsBookmarkEnabled = false;
        pdfViewerControl.IsTextSearchEnabled = false;
        pdfViewerControl.IsTextSelectionEnabled = false;
        pdfViewerControl.Location = new Point(3, 19);
        messageBoxSettings1.EnableNotification = true;
        pdfViewerControl.MessageBoxSettings = messageBoxSettings1;
        pdfViewerControl.MinimumZoomPercentage = 50;
        pdfViewerControl.Name = "pdfViewerControl";
        pdfViewerControl.PageBorderThickness = 5;
        pdfViewerPrinterSettings1.Copies = 1;
        pdfViewerPrinterSettings1.PageOrientation = Syncfusion.Windows.PdfViewer.PdfViewerPrintOrientation.Auto;
        pdfViewerPrinterSettings1.PageSize = Syncfusion.Windows.PdfViewer.PdfViewerPrintSize.ActualSize;
        pdfViewerPrinterSettings1.PrintLocation = (PointF)resources.GetObject("pdfViewerPrinterSettings1.PrintLocation");
        pdfViewerPrinterSettings1.ShowPrintStatusDialog = true;
        pdfViewerControl.PrinterSettings = pdfViewerPrinterSettings1;
        pdfViewerControl.ReferencePath = null;
        pdfViewerControl.ScrollDisplacementValue = 0;
        pdfViewerControl.ShowHorizontalScrollBar = true;
        pdfViewerControl.ShowToolBar = true;
        pdfViewerControl.ShowVerticalScrollBar = true;
        pdfViewerControl.Size = new Size(772, 693);
        pdfViewerControl.SpaceBetweenPages = 5;
        pdfViewerControl.TabIndex = 0;
        pdfViewerControl.TabStop = false;
        pdfViewerControl.Text = "pdfViewerControl";
        textSearchSettings1.CurrentInstanceColor = Color.FromArgb(127, 255, 171, 64);
        textSearchSettings1.HighlightAllInstance = true;
        textSearchSettings1.OtherInstanceColor = Color.FromArgb(127, 254, 255, 0);
        pdfViewerControl.TextSearchSettings = textSearchSettings1;
        pdfViewerControl.ThemeName = "Office2016Colorful";
        pdfViewerControl.VerticalScrollOffset = 0;
        pdfViewerControl.VisualStyle = Syncfusion.Windows.Forms.PdfViewer.VisualStyle.Office2016Colorful;
        pdfViewerControl.ZoomMode = Syncfusion.Windows.Forms.PdfViewer.ZoomMode.FitPage;
        // 
        // listBoxFiles
        // 
        listBoxFiles.Dock = DockStyle.Fill;
        listBoxFiles.FormattingEnabled = true;
        listBoxFiles.ItemHeight = 15;
        listBoxFiles.Location = new Point(3, 19);
        listBoxFiles.Name = "listBoxFiles";
        listBoxFiles.Size = new Size(388, 693);
        listBoxFiles.TabIndex = 0;
        // 
        // FormMain
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1184, 821);
        Controls.Add(tableLayoutPanel1);
        MinimumSize = new Size(1000, 500);
        Name = "FormMain";
        Text = "GISNET PDF Inspektor";
        Load += FormMain_Load;
        tableLayoutPanel1.ResumeLayout(false);
        groupBoxFileList.ResumeLayout(false);
        groupBoxPDFViewer.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel tableLayoutPanel1;
    private GroupBox groupBoxFileList;
    private GroupBox groupBoxFileOperations;
    private GroupBox groupBoxOptions;
    private GroupBox groupBoxPDFViewer;
    private Syncfusion.Windows.Forms.PdfViewer.PdfViewerControl pdfViewerControl;
    private ListBox listBoxFiles;
}
