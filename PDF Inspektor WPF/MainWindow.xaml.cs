// ====================================================================================================
// <copyright file="MainWindow.xaml.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Windows.PdfViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true }; // Opcje serializacji JSON

    private readonly string jsonPath = Path.Combine(AppContext.BaseDirectory, "PDF_Inspektor.appsettings.json"); // Ścieżka do pliku konfiguracyjnego

    private readonly AppSettings appSettings; // Obiekt przechowujący ustawienia aplikacji

    private readonly List<PdfFile> pdfFiles = []; // Lista przechowująca informacje o plikach PDF

    /// <summary>
    /// Inicjalizuje nową instancję klasy <see cref="MainWindow"/>.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();

        this.PdfViewer.ToolbarSettings.ShowFileTools = false; // Ukrywa narzędzia plikowe
        this.PdfViewer.ToolbarSettings.ShowAnnotationTools = false; // Ukrywa narzędzia adnotacji
        this.PdfViewer.ToolbarSettings.ShowPageNavigationTools = false; // Ukrywa narzędzia nawigacji stron
        this.PdfViewer.IsTextSearchEnabled = false; // Wyłącza wyszukiwanie tekstu

        this.PdfViewer.ThumbnailSettings.IsVisible = false; // Ukrywa miniatury
        this.PdfViewer.IsBookmarkEnabled = false; // Ukrywa zakładki
        this.PdfViewer.EnableLayers = false; // Ukrywa warstwy
        this.PdfViewer.PageOrganizerSettings.IsIconVisible = false; // Ukrywa ikonę organizatora stron
        this.PdfViewer.EnableRedactionTool = false; // Wyłącza narzędzie do redagowania
        this.PdfViewer.FormSettings.IsIconVisible = false; // Ukrywa ikonę formularzy

        this.PdfViewer.ShowScrollbar = false; // Ukrywa paski przewijania

        /* ---------------------- KONFIGURACJA ------------------------------------ */

        // Jeśli plik konfiguracyjny nie istnieje, utwórz go z domyślnymi ustawieniami
        if (!File.Exists(this.jsonPath))
        {
            this.SaveConfig();
        }

        string json = File.ReadAllText(this.jsonPath); // Wczytaj zawartość pliku konfiguracyjnego

        this.appSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings(); // Deserializuj ustawienia aplikacji

        /* ------------------------------------------------------------------------ */
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private void PdfViewer_DocumentLoaded(object sender, EventArgs args)
    {
        this.PdfViewer.CursorMode = PdfViewerCursorMode.HandTool; // Ustawia kursor na narzędzie ręki

        this.PdfViewer.ZoomMode = ZoomMode.FitPage; // Ustaw tryb powiększenia na dopasowanie do strony

        int selectedIndex = this.ListBoxFiles.SelectedIndex; // Indeks zaznaczonego elementu w ListBox

        PdfFile selectedPdfFile = this.pdfFiles[selectedIndex]; // Pobierz obiekt PdfFile na podstawie indeksu

        string dpi = PDFTools.GetDPI(this.PdfViewer.LoadedDocument); // Pobierz rozdzielczość DPI pierwszego obrazu w dokumencie PDF

        // Jeśli DPI jest różne od 300x300, ustaw kolor tła etykiety na pomarańczowo-czerwony
        if (dpi != "300 x 300")
        {
            this.StatusBarItemMain.Background = new SolidColorBrush(Colors.PaleVioletRed);
        }
        else // Jeśli DPI jest równe 300x300, ustaw kolor tła etykiety na domyślny kolor kontrolki
        {
            this.StatusBarItemMain.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
        }

        // Zaktualizuj tekst etykiety w pasku stanu z informacjami o pliku PDF
        this.StatusBarItemMain.Content = $"Plik: {selectedPdfFile.FileName} | Rozmiar: {selectedPdfFile.FileSize:F0} KB | DPI: {dpi} | Obrót: {this.PdfViewer.LoadedDocument.Pages[0].Rotation}";

        // Ustaw fokus na zaznaczony element w ListBoxFiles
        ListBoxItem? item = this.ListBoxFiles.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as ListBoxItem;
        item?.Focus();
    }

    private void SaveConfig()
    {
        string json = JsonSerializer.Serialize(this.appSettings, JsonOptions);

        File.WriteAllText(this.jsonPath, json);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        this.SaveConfig();
    }

    // Obsługa przeciągania i upuszczania plików do ListBoxFiles
    private void ListBoxFiles_DragOver(object sender, DragEventArgs e)
    {
        // Sprawdzenie, czy przesyłane są pliki lub foldery
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

        e.Handled = true; // Zatrzymuje dalszą propagację zdarzenia i obsługa nie idzie w górę do elementu nadrzędnego
    }

    // Obsługa upuszczania plików do ListBoxFiles
    private void ListBoxFiles_Drop(object sender, DragEventArgs e)
    {
        // Pobierz listę upuszczonych plików lub folderów
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] droppedItems)
        {
            return;
        }

        this.ListBoxFiles.Items.Clear(); // Wyczyść istniejące elementy w ListBox
        this.pdfFiles.Clear(); // Wyczyść istniejące elementy w liście pdfFiles

        List<string> items = [];

        // Przetwórz każdy upuszczony element
        foreach (string item in droppedItems)
        {
            // Dodaj plik PDF, jeśli to plik
            if (File.Exists(item) && string.Equals(Path.GetExtension(item), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                items.Add(item);
            }
            else if (Directory.Exists(item)) // Dodaj wszystkie pliki PDF z folderu i podfolderów
            {
                items.AddRange(Directory.GetFiles(item, "*.pdf", SearchOption.AllDirectories));
            }
        }

        string[] itemsToSort = [.. items]; // Skopiuj do tablicy do sortowania

        Array.Sort(itemsToSort, new NaturalStringComparer()); // Sortowanie naturalne

        foreach (string item in itemsToSort)
        {
            this.ListBoxFiles.Items.Add(item);

            this.pdfFiles.Add(new PdfFile(this.ListBoxFiles.Items.Count - 1, item));
        }

        this.ListBoxFiles.SelectedIndex = 0; // Ustaw pierwszy dodany plik jako zaznaczony
    }

    // Obsługa zmiany zaznaczenia w ListBoxFiles
    private void ListBoxFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int selectedIndex = this.ListBoxFiles.SelectedIndex; // Indeks zaznaczonego elementu w ListBox

        // Sprawdzenie, czy indeks jest prawidłowy, bo po dodaniu nowych plików do listy wywoływane jest jej czyszczenie a to wywołuje to zdarzenie
        if (selectedIndex >= 0)
        {
            PdfFile selectedPdfFile = this.pdfFiles[selectedIndex]; // Pobierz obiekt PdfFile na podstawie indeksu

            this.PdfViewer.Load(selectedPdfFile.FilePath); // Załaduj wybrany plik PDF do kontrolki PdfViewer
        }
    }

    private void ButtonRotate_OnClick(object sender, RoutedEventArgs e)
    {
        int selectedIndex = this.ListBoxFiles.SelectedIndex; // Indeks zaznaczonego elementu w ListBox

        string fileName = this.pdfFiles[selectedIndex].FilePath; // Nazwa zaznaczonego pliku PDF

        PdfLoadedDocument loadedDocument = this.PdfViewer.LoadedDocument; // Pobierz załadowany dokument PDF z kontrolki PdfViewer

        // Sprawdź, czy dokument ma co najmniej jedną stronę i czy pierwsza strona jest typu PdfLoadedPage
        if (loadedDocument.Pages[0] is PdfLoadedPage loadedPage)
        {
            // Obsługa zdarzenia kliknięcia lewym lub prawym przyciskiem myszy
            switch (e.RoutedEvent.Name)
            {
                case "Click": // Lewy przycisk myszy
                {
                    int newRotation = ((int)loadedPage.Rotation + (int)PdfPageRotateAngle.RotateAngle90) % 360; // Oblicz nowy kąt obrotu

                    loadedPage.Rotation = (PdfPageRotateAngle)newRotation; // Ustaw nowy kąt obrotu strony

                    break;
                }

                case "MouseRightButtonDown": // Prawy przycisk myszy
                {
                    int newRotation = ((int)loadedPage.Rotation - (int)PdfPageRotateAngle.RotateAngle90 + 360) % 360; // Oblicz nowy kąt obrotu

                    loadedPage.Rotation = (PdfPageRotateAngle)newRotation; // Ustaw nowy kąt obrotu strony

                    break;
                }
            }

            // Zapisz zmodyfikowany dokument PDF do pliku, nadpisując oryginalny plik
            loadedDocument.Save(fileName);

            // Ponownie załaduj plik PDF do kontrolki PdfViewer
            this.PdfViewer.Load(fileName);
        }
    }
}