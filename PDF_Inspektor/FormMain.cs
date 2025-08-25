// ====================================================================================================
// <copyright file="FormMain.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

using System.Text.Json;

using PDF_Inspektor;

using Syncfusion.Windows.Forms.PdfViewer;

namespace PDF_Inspektor_OLD;

/// <summary>
/// Klasa g³ówna formularza aplikacji.
/// </summary>
public partial class FormMain : Form
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true }; // Opcje serializacji JSON

    private readonly string jsonPath = Path.Combine(AppContext.BaseDirectory, "PDF_Inspektor.appsettings.json"); // Œcie¿ka do pliku konfiguracyjnego

    private readonly AppSettings appSettings; // Obiekt przechowuj¹cy ustawienia aplikacji

    private readonly List<PdfFile> pdfFiles = []; // Lista przechowuj¹ca informacje o plikach PDF

    /// <summary>
    /// Inicjalizuje now¹ instancjê klasy <see cref="FormMain"/>.
    /// </summary>
    public FormMain()
    {
        this.InitializeComponent();

        /* ---------------------- KONFIGURACJA ------------------------------------ */

        // Jeœli plik konfiguracyjny nie istnieje, utwórz go z domyœlnymi ustawieniami
        if (!File.Exists(this.jsonPath))
        {
            this.SaveConfig();
        }

        string json = File.ReadAllText(this.jsonPath); // Wczytaj zawartoœæ pliku konfiguracyjnego

        this.appSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings(); // Deserializuj ustawienia aplikacji

        /* ------------------------------------------------------------------------ */

        this.Icon = Properties.Resources.mainIcon; // Ustaw ikonê aplikacji
    }

    private void FormMain_Load(object sender, EventArgs e)
    {
        // Ukryj przyciski Zapisz, Drukuj i Otwórz na pasku narzêdzi kontrolki PDF Viewer.
        this.pdfViewerControl.ToolbarSettings.SaveButton.IsVisible = false;
        this.pdfViewerControl.ToolbarSettings.PrintButton.IsVisible = false;
        this.pdfViewerControl.ToolbarSettings.OpenButton.IsVisible = false;

        // pobierz katalog aplikacji
        string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string startFileName = Path.Combine(appDirectory, "ScanHelper.pdf");

        this.pdfFiles.Add(new PdfFile(0, startFileName));

        this.listBoxFiles.Items.Add("ScanHelper.pdf");
        this.listBoxFiles.SelectedIndex = 0;
    }

    // Obs³uga zdarzenia DragEnter dla listBoxFiles
    // Sprawdza, czy przeci¹gane pliki s¹ plikami PDF
    private void ListBoxFiles_DragEnter(object sender, DragEventArgs e)
    {
        // SprawdŸ, czy dane s¹ w formacie FileDrop i czy wszystkie pliki maj¹ rozszerzenie .pdf
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;

            if (files.All(f => Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase)))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    // Obs³uga zdarzenia DragDrop dla listBoxFiles
    private void ListBoxFiles_DragDrop(object sender, DragEventArgs e)
    {
        // Dodaj pliki PDF do listBoxFiles jeœli s¹ w formacie FileDrop
        if (e.Data != null)
        {
            this.listBoxFiles.Items.Clear(); // Wyczyœæ istniej¹ce elementy w ListBox
            this.pdfFiles.Clear(); // Wyczyœæ istniej¹c¹ listê pdfFiles

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;

            foreach (string file in files)
            {
                if (Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    PdfFile pdfFile = new(this.listBoxFiles.Items.Count, file);

                    this.pdfFiles.Add(pdfFile);

                    this.listBoxFiles.Items.Add(pdfFile.FileName);
                }
            }

            this.listBoxFiles.SelectedIndex = 0; // Ustaw pierwszy dodany plik jako zaznaczony
        }
    }

    private void ListBoxFiles_SelectedIndexChanged(object sender, EventArgs e)
    {
        int selectedIndex = this.listBoxFiles.SelectedIndex; // Indeks zaznaczonego elementu w ListBox

        PdfFile selectedPdfFile = this.pdfFiles[selectedIndex]; // Pobierz obiekt PdfFile na podstawie indeksu

        this.pdfViewerControl.Load(selectedPdfFile.FilePath); // Za³aduj plik PDF do kontrolki PDF Viewer
    }

    private void PdfViewerControl_DocumentLoaded(object sender, EventArgs args)
    {
        this.pdfViewerControl.ZoomMode = ZoomMode.FitPage; // Ustaw tryb powiêkszenia na dopasowanie do strony

        this.ActiveControl = this.listBoxFiles; // Ustaw fokus na listBoxFiles po za³adowaniu dokumentu

        int selectedIndex = this.listBoxFiles.SelectedIndex; // Indeks zaznaczonego elementu w ListBox

        PdfFile selectedPdfFile = this.pdfFiles[selectedIndex]; // Pobierz obiekt PdfFile na podstawie indeksu

        string dpi = PDFTools.GetDPI(this.pdfViewerControl.LoadedDocument); // Pobierz rozdzielczoœæ DPI pierwszego obrazu w dokumencie PDF

        // Jeœli DPI jest ró¿ne od 300x300, ustaw kolor t³a etykiety na pomarañczowo-czerwony
        if (dpi != "300 x 300")
        {
            this.toolStripStatusLabel.BackColor = Color.OrangeRed;
        }
        else // Jeœli DPI jest równe 300x300, ustaw kolor t³a etykiety na domyœlny kolor kontrolki
        {
            this.toolStripStatusLabel.BackColor = Color.FromName("Control");
        }

        // Zaktualizuj tekst etykiety w pasku stanu z informacjami o pliku PDF
        this.toolStripStatusLabel.Text = $"Plik: {selectedPdfFile.FileName} | Rozmiar: {selectedPdfFile.FileSize:F0} KB | DPI: {dpi}";
    }

    // Zapisz ustawienia aplikacji przy zamykaniu formularza
    private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
    {
        this.SaveConfig();
    }

    // Zapisz ustawienia aplikacji do pliku JSON
    private void SaveConfig()
    {
        string json = JsonSerializer.Serialize(this.appSettings, JsonOptions);

        File.WriteAllText(this.jsonPath, json);
    }
}
