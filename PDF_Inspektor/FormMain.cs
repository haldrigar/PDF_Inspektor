// ====================================================================================================
// <copyright file="FormMain.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

using Syncfusion.Windows.Forms.PdfViewer;

namespace PDF_Inspektor;

/// <summary>
/// Klasa g³ówna formularza aplikacji.
/// </summary>
public partial class FormMain : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormMain"/> class.
    /// </summary>
    public FormMain()
    {
        this.InitializeComponent();

        this.Icon = Properties.Resources.mainIcon;
    }

    private void FormMain_Load(object sender, EventArgs e)
    {
        // Ukryj przyciski Zapisz, Drukuj i Otwórz na pasku narzêdzi kontrolki PDF Viewer.
        this.pdfViewerControl.ToolbarSettings.SaveButton.IsVisible = false;
        this.pdfViewerControl.ToolbarSettings.PrintButton.IsVisible = false;
        this.pdfViewerControl.ToolbarSettings.OpenButton.IsVisible = false;

        this.pdfViewerControl.Load("ScanHelper.pdf");
        this.pdfViewerControl.ZoomMode = ZoomMode.FitPage;
    }
}
