// ====================================================================================================
// <copyright file="MainWindow.xaml.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// 
// Ostatni zapis pliku: 2025-09-04 16:53:36
// ====================================================================================================

namespace PDF_Inspektor;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using PDF;

using Syncfusion.Pdf;
using Syncfusion.Windows.PdfViewer;

using Tool;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
internal partial class MainWindow
{
    /// <summary>
    /// Przechowuje ustawienia aplikacji.
    /// </summary>
    private readonly AppSettings _appSettings;

    /// <summary>
    /// Porządny komparator do sortowania nazw plików w sposób naturalny (np. "file2.pdf" przed "file10.pdf").
    /// </summary>
    private readonly NaturalStringComparer _naturalComparer = new();

    /// <summary>
    /// Strumień w pamięci dla aktualnie zaznaczonego pliku PDF.
    /// </summary>
    private MemoryStream? _selectedPdfStream;

    /// <summary>
    /// Aktywny katalog, z którego są ładowane pliki PDF.
    /// </summary>
    private string _activeDirectory = string.Empty;

    /// <summary>
    /// Inicjalizuje nową instancję klasy <see cref="MainWindow"/>.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();

        this.PdfViewer.ThumbnailSettings.IsVisible = false; // Hides the thumbnail icon.
        this.PdfViewer.IsBookmarkEnabled = false; // Hides the bookmark icon.
        this.PdfViewer.EnableLayers = false; // Hides the layer icon.
        this.PdfViewer.PageOrganizerSettings.IsIconVisible = false; // Hides the organize page icon.
        this.PdfViewer.EnableRedactionTool = false; // Hides the redaction icon.
        this.PdfViewer.FormSettings.IsIconVisible = false; // Hides the form icon.

        this.DataContext = this; // Ustawienie kontekstu danych dla bindowania

        // Załaduj ustawienia aplikacji
        this._appSettings = AppSettings.Load();
    }

    /// <summary>
    /// Pobiera lista plików PDF do wyświetlenia w interfejsie użytkownika.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public ObservableCollection<PdfFile> PdfFiles { get; } = [];

    /// <summary>
    /// Pobiera lub ustawia aktualnie zaznaczony plik PDF w interfejsie użytkownika.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public PdfFile? SelectedPdfFile { get; set; }

    /// <summary>
    /// Funkcja obsługująca zdarzenie załadowania okna.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        this.Top = this._appSettings.WindowTop;
        this.Left = this._appSettings.WindowLeft;
        this.Width = this._appSettings.WindowWidth;
        this.Height = this._appSettings.WindowHeight;

        // Upewnij się, że okno jest widoczne na ekranie
        Tools.EnsureWindowIsOnScreen(this);

        // Sprawdź, czy dostępna jest aktualizacja
        if (Tools.IsUpdateAvailable())
        {
            MessageBox.Show(
                "Wykryto aktualizację programu!\n\nProgram zostanie zamknięty, a po aktualizacji uruchomiony ponownie.",
                "Aktualizacja programu",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Tools.RunUpdaterAndExit();

            return; // Zakończ działanie bieżącej instancji
        }

        // Sprawdź i rozpakuj narzędzia zdefiniowane w konfiguracji
        foreach (ExternalTool tool in this._appSettings.Tools)
        {
            // Rozpakuj narzędzie, jeśli to konieczne
            Tools.EnsureAndUnpackTool(tool);
        }

        Mouse.OverrideCursor = null;
    }

    /// <summary>
    /// Funkcja obsługująca zdarzenie zamknięcia okna.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Closed(object sender, EventArgs e)
    {
        this._appSettings.WindowTop = this.Top;
        this._appSettings.WindowLeft = this.Left;
        this._appSettings.WindowWidth = this.Width;
        this._appSettings.WindowHeight = this.Height;

        this._appSettings.Save(); // Zapisz ustawienia

        // Zwolnij zasoby strumienia PDF przy zamykaniu aplikacji.
        this._selectedPdfStream?.Dispose();
    }

    /// <summary>
    /// Funkcja obsługująca zdarzenie zmiany rozmiaru okna.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Po zmianie rozmiaru okna, ustaw ponownie tryb powiększenia na "Dopasuj stronę"
        this.PdfViewer.ZoomMode = ZoomMode.FitPage;

        // Ustawienie szerokości i wysokości na Auto (NaN) dla responsywnego rozmiaru
        this.PdfViewer.Width = double.NaN;
        this.PdfViewer.Height = double.NaN;
    }

    /// <summary>
    /// Obsługuje zdarzenie aktywacji okna (gdy odzyskuje fokus).
    /// Sprawdza, czy zaznaczony plik został zmodyfikowany i w razie potrzeby go odświeża.
    /// </summary>
    private void Window_Activated(object sender, EventArgs e)
    {
        // ============================================== SYNCHRONIZACJA LISTY PLIKÓW Z DYSKIEM ==================================

        // Sprawdź, czy aktywny katalog jest ustawiony i istnieje
        if (!string.IsNullOrEmpty(this._activeDirectory) && Directory.Exists(this._activeDirectory))
        {
            // Pobierz aktualną listę plików PDF z dysku
            HashSet<string> pdfOnDiskFilePaths = new(Directory.GetFiles(this._activeDirectory,
                    "*.pdf",
                    SearchOption.TopDirectoryOnly),
                StringComparer.OrdinalIgnoreCase);

            // Pobierz listę plików aktualnie załadowanych w aplikacji
            List<PdfFile> pdfFiles = [.. this.PdfFiles];

            // --- Krok 1: Usuń pliki, które nie istnieją już na dysku ---

            Debug.WriteLine("Usuwanie z listy plików, których nie ma już na dysku...");

            int removedCount = 0;

            foreach (PdfFile pdfFile in pdfFiles)
            {
                if (!pdfOnDiskFilePaths.Contains(pdfFile.FilePath))
                {
                    this.PdfFiles.Remove(pdfFile);

                    Debug.WriteLine($"Plik '{pdfFile.FileName}' usunięto z listy.");

                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                this.StatusBarItemInfo.Content = $"Usunięto {removedCount} plików PDF z listy.";
                this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

                if (this.PdfFiles.Count == 0)
                {
                    this.PdfViewer.Unload(true);
                    this._selectedPdfStream?.Dispose();
                }
            }

            Debug.WriteLine("Zakończono usuwanie z listy plików, których nie ma już na dysku.");

            // --- Krok 2: Dodaj nowe pliki, które pojawiły się na dysku ---

            Debug.WriteLine("Dodawanie nowych plików z dysku na listę...");

            int addedCount = 0;

            HashSet<string> pdfFilesListbox = new(this.PdfFiles.Select(f => f.FilePath), StringComparer.OrdinalIgnoreCase);

            foreach (string pdfOnDisk in pdfOnDiskFilePaths)
            {
                if (!pdfFilesListbox.Contains(pdfOnDisk))
                {
                    try
                    {
                        PdfFile pdfToAdd = new(pdfOnDisk);

                        this.AddAndSortFile(pdfToAdd);

                        Debug.WriteLine($"Plik '{pdfToAdd.FileName}' dodano do listy.");

                        addedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Błąd podczas dodawania nowego pliku PDF: {pdfOnDisk} - {ex.Message}");
                    }
                }
            }

            if (addedCount > 0)
            {
                this.StatusBarItemInfo.Content = $"Dodano {addedCount} nowych plików PDF do listy.";
                this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);
            }

            Debug.WriteLine("Zakończono dodawanie nowych plików z dysku na listę.");
        }

        // =============================== SPRAWDZENIE CZY ZAZNACZONY PLIK BYŁ MODYFIKOWANY POZA PROGRAMEM ====================
        // Sprawdź, czy jakikolwiek plik jest zaznaczony
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile && this.ListBoxFiles.SelectedItems.Count == 1)
        {
            try
            {
                // Pobierz aktualne informacje o pliku z dysku
                FileInfo fileInfo = new(selectedPdfFile.FilePath);

                fileInfo.Refresh(); // Upewnij się, że dane są aktualne

                // Sprawdź, czy plik istnieje i czy czas modyfikacji jest nowszy
                if (fileInfo.Exists && fileInfo.LastWriteTime > selectedPdfFile.LastWriteTime)
                {
                    Debug.WriteLine($"Wykryto zmianę w pliku '{selectedPdfFile.FileName}' po odzyskaniu fokusu. Przeładowuję...");

                    // Plik został zmieniony, więc przeładuj podgląd
                    this.LoadPdfToView(selectedPdfFile);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas sprawdzania pliku w Window_Activated: {ex.Message}");
            }
            finally
            {
                // Zawsze upewnij się, że zaznaczony element ma fokus.
                this.FocusAndScrollToListBoxItem(selectedPdfFile);
            }
        }
    }

    /// <summary>
    /// Funkcja dodająca plik do kolekcji i utrzymująca sortowanie.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ListBoxFiles_DragOver(object sender, DragEventArgs e)
    {
        // Sprawdzamy czy przeciągane są pliki/foldery.
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

        e.Handled = true;
    }

    /// <summary>
    /// Funkcja obsługująca zdarzenie upuszczenia plików na ListBox.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ListBoxFiles_Drop(object sender, DragEventArgs e)
    {
        // Sprawdź, czy są upuszczone pliki
        if (e.Data.GetData(DataFormats.FileDrop) is string[] droppedItems)
        {
            // Załaduj pliki PDF z upuszczonych elementów
            this.LoadFiles(droppedItems);
        }
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie przycisku otwierania katalogu.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonOpenDirectory_Click(object sender, RoutedEventArgs e)
    {
        // Otwórz okno dialogowe wyboru folderu
        Microsoft.Win32.OpenFolderDialog dialog = new()
        {
            Title = "Wybierz folder do przetwarzania",
            InitialDirectory = this._appSettings.LastUsedDirectory,
        };

        // Jeśli użytkownik wybrał katalog
        if (dialog.ShowDialog() == true)
        {
            // Załaduj istniejące pliki z wybranego folderu
            this.LoadFiles([dialog.FolderName]);

            // Ustaw aktywny katalog
            this._activeDirectory = dialog.FolderName;

            // Zapisz wybrany katalog jako ostatnio używany
            this._appSettings.LastUsedDirectory = dialog.FolderName;
        }
    }

    /// <summary>
    /// Funkcja ładująca pliki PDF z podanych ścieżek (plików lub katalogów).
    /// </summary>
    /// <param name="paths"></param>
    private void LoadFiles(IEnumerable<string> paths)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    if (Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        directories.Add(Path.GetDirectoryName(path) ?? string.Empty);
                    }
                }
                else if (Directory.Exists(path))
                {
                    directories.Add(path);
                }
            }

            directories.RemoveWhere(string.IsNullOrEmpty);

            if (directories.Count > 1)
            {
                MessageBox.Show("Proszę przeciągać pliki lub foldery tylko z jednego katalogu naraz.", "Niedozwolona operacja", MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            string? singleDirectory = directories.FirstOrDefault();

            if (string.IsNullOrEmpty(singleDirectory))
            {
                return; // Brak prawidłowych plików lub folderów
            }

            this._activeDirectory = singleDirectory; // Ustaw aktywny katalog

            this._appSettings.LastUsedDirectory = singleDirectory; // Zapisz wybrany katalog jako ostatnio używany

            this.TextBoxDirectory.Text = Path.GetFileName(singleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            /* --------------------------------------- */

            // Czyszczenie
            this.PdfViewer.Unload(true);

            this._selectedPdfStream?.Dispose();

            this.PdfFiles.Clear();

            /* --------------------------------------- */

            List<string> pdfFiles = [.. Directory.GetFiles(singleDirectory, "*.pdf", SearchOption.TopDirectoryOnly)];

            if (pdfFiles.Count > 0)
            {
                pdfFiles.Sort(this._naturalComparer);

                foreach (string file in pdfFiles)
                {
                    this.PdfFiles.Add(new PdfFile(file));
                }

                // Zaznacz pierwszy plik na liście
                this.FocusAndScrollToListBoxItem(this.PdfFiles[0]);
            }
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    /// <summary>
    /// Funkcja obsługująca zdarzenie zmiany zaznaczenia w ListBox.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ListBoxFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Sprawdzamy, czy zaznaczony element jest typu PdfFile
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            // Aktualizuj właściwość SelectedPdfFile
            this.SelectedPdfFile = selectedPdfFile;

            // Po prostu zainicjuj przeładowanie widoku. Resztą zajmie się DocumentLoaded.
            this.LoadPdfToView(selectedPdfFile);
        }
        else // Jeśli nic nie jest zaznaczone (np. po usunięciu ostatniego elementu), wyczyść podgląd i strumień
        {
            this._selectedPdfStream?.Dispose();

            this.PdfViewer.Unload(true);

            Mouse.OverrideCursor = null;
        }
    }

    /// <summary>
    /// Funkcja przeładowująca widok PdfViewer z pliku PDF.
    /// </summary>
    /// <param name="pdfFileToLoadFile">Plik PDF.</param>
    private void LoadPdfToView(PdfFile pdfFileToLoadFile)
    {
        // Sprawdź, czy plik nadal istnieje
        if (!File.Exists(pdfFileToLoadFile.FilePath))
        {
            this.PdfFiles.Remove(pdfFileToLoadFile);

            this.StatusBarItemInfo.Content = "Usunięto z listy nieistniejący plik.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        Mouse.OverrideCursor = Cursors.Wait;

        // Spróbuj załadować do PdfViewer zaznczony plik
        try
        {
            this._selectedPdfStream?.Dispose(); // Zwolnij poprzedni strumień, jeśli istnieje

            // Utwórz strumień w pamięci na podstawie wczytanych bajtów.
            this._selectedPdfStream = new MemoryStream(File.ReadAllBytes(pdfFileToLoadFile.FilePath));

            // Załaduj dokument do PdfViewer ze strumienia w pamięci.
            this.PdfViewer.Load(this._selectedPdfStream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas ładowania pliku do pamięci: {ex.Message}");

            // W razie błędu wyczyść podgląd
            this.PdfViewer.Unload(true);

            this._selectedPdfStream?.Dispose();
        }
        finally
        {
            // Przywróć kursor na końcu, ale tylko jeśli ładowanie się nie powiodło.
            // Jeśli się powiodło, kursor zostanie przywrócony w zdarzeniu DocumentLoaded.
            if (!this.PdfViewer.IsLoaded)
            {
                Mouse.OverrideCursor = null;
            }
        }
    }

    /// <summary>
    /// Funkcja obsługująca zdarzenie załadowania dokumentu w kontrolce PdfViewer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void PdfViewer_DocumentLoaded(object sender, EventArgs args)
    {
        this.PdfViewer.ZoomMode = ZoomMode.FitPage; // Dopasuj stronę

        this.PdfViewer.Width = double.NaN; // Automatyczna szerokość
        this.PdfViewer.Height = double.NaN; // Automatyczna wysokość

        this.PdfViewer.CursorMode = PdfViewerCursorMode.HandTool; // Ustaw kursor na "rękę"

        List<string> errors = []; // Lista błędów do wyświetlenia

        // Najpierw sprawdź, czy w ogóle mamy załadowany i zaznaczony plik
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            // Pobierz aktualne informacje o pliku, ponieważ mamy pewność, że istnieje i jest dostępny.
            FileInfo fileInfo = new(selectedPdfFile.FilePath);

            fileInfo.Refresh(); // Upewnij się, że dane są aktualne

            selectedPdfFile.FileSize = fileInfo.Length;
            selectedPdfFile.LastWriteTime = fileInfo.LastWriteTime;

            int dpiX = 0, dpiY = 0; // Domyślne wartości DPI

            int rotation = 0; // Domyślny obrót

            // Sprawdź, czy dokument PDF ma strony
            if (this.PdfViewer.LoadedDocument.Pages.Count > 0)
            {
                (dpiX, dpiY) = PDFTools.GetDPI(this.PdfViewer.LoadedDocument);

                // Sprawdź, czy DPI jest różne od 300x300 z tolerancją +/-1
                if (Math.Abs(dpiX - 300) > 1 || Math.Abs(dpiY - 300) > 1)
                {
                    errors.Add("BŁĄD DPI");
                }

                rotation = this.PdfViewer.LoadedDocument.Pages[0].Rotation switch
                {
                    PdfPageRotateAngle.RotateAngle0 => 0,
                    PdfPageRotateAngle.RotateAngle90 => 90,
                    PdfPageRotateAngle.RotateAngle180 => 180,
                    PdfPageRotateAngle.RotateAngle270 => 270,
                    _ => 0,
                };
            }
            else
            {
                errors.Add("PUSTY DOKUMENT");
            }

            // Główny status jest ustawiany ZAWSZE, gdy mamy zaznaczony plik
            this.StatusBarItemMain.Content = $"Plik [{this.ListBoxFiles.SelectedIndex + 1}/{this.PdfFiles.Count}]: {selectedPdfFile.FileName} | Rozmiar: {selectedPdfFile.FileSize / 1024.0:F0} KB | DPI: {dpiX}x{dpiY} | Obrót: {rotation}°";
        }
        else
        {
            errors.Add("BŁĄD ZAZNACZONEGO PLIKU");
        }

        if (errors.Count > 0)
        {
            this.StatusBarItemInfo.Content = string.Join("; ", errors);
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Red);
        }
        else
        {
            this.StatusBarItemInfo.Content = "OK";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Green);
        }

        Mouse.OverrideCursor = null; // Przywróć kursor
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie przycisku obracania pliku PDF.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonRotate_OnClick(object sender, RoutedEventArgs e)
    {
        // Sprawdź, czy zaznaczony jest dokładnie jeden element
        if (this.ListBoxFiles.SelectedItems.Count != 1)
        {
            this.StatusBarItemInfo.Content = "Operacja wymaga zaznaczenia jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        bool rotateRight = e is not MouseButtonEventArgs { ChangedButton: MouseButton.Right };

        this.RotateAndSave(rotateRight);

        e.Handled = true; // Zaznacz zdarzenie jako obsłużone
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie przycisku usuwania pliku PDF.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        this.DeleteSelectedFiles(); // Funkcja usuwająca zaznaczone pliki
    }

    /// <summary>
    /// Funkcja obsługująca zdarzenie naciśnięcia klawisza w ListBox.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ListBoxFiles_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Right || e.Key == Key.Left) // Obsługa klawiszy strzałek do obrotu
        {
            if (this.ListBoxFiles.SelectedItems.Count != 1)
            {
                this.StatusBarItemInfo.Content = "Obrót wymaga zaznaczenia jednego pliku.";
                this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

                e.Handled = true;

                return;
            }

            this.RotateAndSave(e.Key == Key.Right);

            e.Handled = true;
        }
        else if (e.Key == Key.Delete) // Obsługa klawisza Delete do usuwania pliku
        {
            this.DeleteSelectedFiles(); // Funkcja usuwająca zaznaczone pliki

            e.Handled = true;
        }
        else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control) // Kopiowanie plików (Ctrl+C)
        {
            this.CopySelectedFiles();

            e.Handled = true;
        }
        else if (e.Key == Key.X && Keyboard.Modifiers == ModifierKeys.Control) // Wycinanie plików (Ctrl+X)
        {
            this.CutSelectedFiles();

            e.Handled = true;
        }
        else if (e.Key == Key.F2) // zmiana nazwy pliku (F2)
        {
            this.RenameSelectedFile();

            e.Handled = true;
        }
    }

    /// <summary>
    /// Funkcja obracająca i zapisująca zmiany w zaznaczonym pliku PDF.
    /// </summary>
    /// <param name="rotateRight"></param>
    private void RotateAndSave(bool rotateRight)
    {
        // Sprawdź, czy zaznaczony element jest typu PdfFile
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                // Obróć i zapisz zmiany w pliku PDF
                bool success = PDFTools.RotateAndSave(this.PdfViewer.LoadedDocument, selectedPdfFile.FilePath, rotateRight);

                if (success)
                {
                    // Po udanej operacji obrotu i zapisu musimy ręcznie przeładować widok.
                    this.LoadPdfToView(selectedPdfFile);

                    // przywrócenie kursora jest w DocumentLoaded, który i tak zostanie wywołany po LoadPdfToView
                }
                else
                {
                    MessageBox.Show("Nie udało się obrócić i zapisać pliku.", "Błąd operacji", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null; // Przywróć kursor na wszelki wypadek
            }
        }
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie przycisku edycji w zewnętrznym narzędziu IrfanView.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonEditIrfanView_Click(object sender, RoutedEventArgs e)
    {
        this.LaunchConfiguredTool("IrfanView");
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie przycisku edycji w zewnętrznym narzędziu GIMP.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonEditGimp_Click(object sender, RoutedEventArgs e)
    {
        this.LaunchConfiguredTool("GIMP");
    }

    /// <summary>
    /// Uruchamia narzędzie zewnętrzne zdefiniowane w konfiguracji.
    /// </summary>
    /// <param name="toolName">Nazwa narzędzia (zgodna z wpisem w appsettings.json).</param>
    private void LaunchConfiguredTool(string toolName)
    {
        // Sprawdź, czy zaznaczony jest dokładnie jeden element
        if (this.ListBoxFiles.SelectedItems.Count != 1)
        {
            this.StatusBarItemInfo.Content = "Operacja wymaga zaznaczenia jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        // Sprawdź, czy zaznaczony element jest typu PdfFile
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            ExternalTool? tool = this._appSettings.Tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

            if (tool == null)
            {
                MessageBox.Show($"Nie znaleziono konfiguracji dla narzędzia '{toolName}'.", "Błąd konfiguracji", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            string fileToOpen = selectedPdfFile.FilePath; // Ścieżka do pliku PDF do otwarcia

            string executablePath = Path.Combine(AppContext.BaseDirectory, tool.ExecutablePath); // Pełna ścieżka do pliku wykonywalnego

            if (!File.Exists(executablePath)) // Sprawdź, czy plik wykonywalny istnieje
            {
                MessageBox.Show($"Nie znaleziono aplikacji: {executablePath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (!File.Exists(fileToOpen)) // Sprawdź, czy plik do otwarcia istnieje
            {
                MessageBox.Show($"Plik nie istnieje: {fileToOpen}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            // Uruchom narzędzie z plikiem jako argumentem
            try
            {
                Process.Start(new ProcessStartInfo(executablePath, $"\"{fileToOpen}\"") { UseShellExecute = true, });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się uruchomić procesu dla pliku: {fileToOpen}.\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Znajdź narzędzie w konfiguracji
    }

    /// <summary>
    /// Ustawia fokus na podanym elemencie w ListBox, przewijając do niego, jeśli jest to konieczne.
    /// </summary>
    /// <param name="itemToFocus">Element, na którym ma zostać ustawiony fokus.</param>
    private void FocusAndScrollToListBoxItem(PdfFile itemToFocus)
    {
        // Aktywuj główne okno aplikacji, aby wysunęło się na pierwszy plan.
        this.Activate();

        // Ustaw właściwość SelectedPdfFile na element, na którym chcemy ustawić fokus.
        this.SelectedPdfFile = itemToFocus;

        // Upewnij się, że element jest zaznaczony.
        this.ListBoxFiles.SelectedItem = itemToFocus;

        // Powiedz ListBox, aby przewinął widok do tego elementu.
        // To spowoduje, że WPF utworzy dla niego kontener (ListBoxItem), jeśli nie był widoczny.
        this.ListBoxFiles.ScrollIntoView(itemToFocus);

        // Użyj Dispatcher InvokeAsync z niskim priorytetem, aby ustawić fokus.
        // Daje to WPF czas na przetworzenie ScrollIntoView i utworzenie kontenera wizualnego.
        this.Dispatcher.InvokeAsync(
            () =>
            {
                if (this.ListBoxFiles.ItemContainerGenerator.ContainerFromItem(itemToFocus) is ListBoxItem lbi)
                {
                    lbi.Focus();
                }
            },
            DispatcherPriority.ContextIdle);
    }

    /// <summary>
    /// Funkcja obsługująca zmianę nazwy zaznaczonego pliku PDF.
    /// </summary>
    private void RenameSelectedFile()
    {
        // Sprawdź, czy zaznaczony jest dokładnie jeden element
        if (this.ListBoxFiles.SelectedItems.Count != 1)
        {
            this.StatusBarItemInfo.Content = "Zmiana nazwy wymaga zaznaczenia jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        // Sprawdź, czy zaznaczony element jest typu PdfFile
        if (this.ListBoxFiles.SelectedItem is not PdfFile pdfToRename)
        {
            return;
        }

        string currentName = pdfToRename.FileName; // Aktualna nazwa pliku

        bool overwrite = false;

        while (true)
        {
            RenameWindow dialog = new(currentName) { Owner = this };

            // Użytkownik anulował okno zmiany nazwy
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string newFileName = dialog.NewFileName.Trim(); // Nowa nazwa pliku bez białych znaków

            // Walidacja: nazwa pusta lub się nie zmieniła
            if (string.IsNullOrWhiteSpace(newFileName) || newFileName.Equals(pdfToRename.FileName, StringComparison.OrdinalIgnoreCase))
            {
                continue; // Pokaż dialog ponownie
            }

            // Walidacja: niedozwolone znaki
            if (newFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("Nowa nazwa pliku zawiera niedozwolone znaki.", "Błąd zmiany nazwy", MessageBoxButton.OK, MessageBoxImage.Warning);

                currentName = newFileName; // Pozwól użytkownikowi edytować nieprawidłową nazwę

                continue; // Pokaż dialog ponownie
            }

            // Dodaj rozszerzenie .pdf, jeśli go brakuje
            if (!Path.HasExtension(newFileName) || !Path.GetExtension(newFileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                newFileName += ".pdf";
            }

            string newFilePath = Path.Combine(pdfToRename.DirectoryName, newFileName); // Nowa pełna ścieżka pliku

            // Sprawdź, czy plik o nowej nazwie już istnieje i czy nie jest to ten sam plik
            if (File.Exists(newFilePath) && !newFilePath.Equals(pdfToRename.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBoxResult result = MessageBox.Show("Plik o takiej nazwie już istnieje! Czy chcesz go nadpisać?", "Plik istnieje", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                    {
                        overwrite = true; // Użytkownik chce nadpisać

                        break;
                    }

                    case MessageBoxResult.No:
                    {
                        currentName = newFileName; // Ustaw nową nazwę do edycji

                        continue; // Wróć do pętli, aby ponownie otworzyć okno zmiany nazwy
                    }

                    case MessageBoxResult.Cancel:
                    {
                        return; // Użytkownik anulował całą operację
                    }
                }
            }

            // Jeśli doszliśmy tutaj, albo plik nie istnieje, albo użytkownik zgodził się na nadpisanie
            try
            {
                // Jeśli nadpisujemy, najpierw usuń stary plik z listy UI
                if (overwrite)
                {
                    // Znajdź plik, który będzie nadpisany
                    PdfFile? fileToOverwrite = this.PdfFiles.FirstOrDefault(f => f.FilePath.Equals(newFilePath, StringComparison.OrdinalIgnoreCase));

                    // Jeśli taki plik istnieje na liście
                    if (fileToOverwrite != null)
                    {
                        this.PdfFiles.Remove(fileToOverwrite);
                    }
                }

                // Zmień nazwę pliku na dysku
                File.Move(pdfToRename.FilePath, newFilePath, overwrite);

                // Aktualizacja listy plików w UI
                this.PdfFiles.Remove(pdfToRename);

                pdfToRename.FileName = newFileName;
                pdfToRename.FilePath = newFilePath;

                this.AddAndSortFile(pdfToRename);

                this.FocusAndScrollToListBoxItem(pdfToRename);

                return; // Sukces, zakończ pętlę i metodę
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się zmienić nazwy pliku.\nBłąd: {ex.Message}", "Błąd zmiany nazwy", MessageBoxButton.OK, MessageBoxImage.Error);

                return; // Zakończ w przypadku błędu
            }
        }
    }

    /// <summary>
    /// Funkcja obsługująca przewijanie kółkiem myszy nad kontrolką PdfViewer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PdfViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Jeśli klawisz Ctrl jest wciśnięty, nie rób nic.
        // Pozwól kontrolce PdfViewer obsłużyć zdarzenie (zoomowanie).
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            return;
        }

        if (this.PdfFiles.Count == 0) // Jeśli lista jest pusta, nie rób nic. nie ma co zmieniać zaznaczenia
        {
            return;
        }

        int currentIndex = this.ListBoxFiles.SelectedIndex; // Pobierz aktualny indeks zaznaczenia

        if (e.Delta < 0) // Kółko w dół
        {
            if (currentIndex < this.PdfFiles.Count - 1)
            {
                this.ListBoxFiles.SelectedIndex = currentIndex + 1;
            }
        }
        else // Kółko w górę
        {
            if (currentIndex > 0)
            {
                this.ListBoxFiles.SelectedIndex = currentIndex - 1;
            }
        }

        // Ustaw fokus i przewiń do nowego elementu
        if (this.ListBoxFiles.SelectedItem is PdfFile selected)
        {
            this.FocusAndScrollToListBoxItem(selected);
        }

        // Oznacz zdarzenie jako obsłużone TYLKO wtedy, gdy nie wciskamy Ctrl
        e.Handled = true;
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie "Zmień nazwę" w menu kontekstowym.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // zmiana nazwy zaznaczonego pliku
        this.RenameSelectedFile();
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie "Usuń" w menu kontekstowym.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // usunięcie zaznaczonych plików
        this.ButtonDelete_Click(sender, e);
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie "Obróć w prawo" w menu kontekstowym.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RotateRightMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Sprawdź, czy zaznaczony jest dokładnie jeden element
        if (this.ListBoxFiles.SelectedItems.Count != 1)
        {
            this.StatusBarItemInfo.Content = "Operacja wymaga zaznaczenia jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        // Wywołaj istniejącą metodę z odpowiednim parametrem
        this.RotateAndSave(true); // true = obrót w prawo
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie "Obróć w lewo" w menu kontekstowym.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RotateLeftMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Sprawdź, czy zaznaczony jest dokładnie jeden element
        if (this.ListBoxFiles.SelectedItems.Count != 1)
        {
            this.StatusBarItemInfo.Content = "Operacja wymaga zaznaczenia jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        // Wywołaj istniejącą metodę z odpowiednim parametrem
        this.RotateAndSave(false); // false = obrót w lewo
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie "Wytnij" w menu kontekstowym.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Wywołaj istniejącą, centralną metodę do wycinania plików.
        this.CutSelectedFiles();
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie "Kopiuj" w menu kontekstowym.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Wywołaj istniejącą, centralną metodę do kopiowania plików.
        this.CopySelectedFiles();
    }

    /// <summary>
    /// Funkcja kopiująca zaznaczone pliki do schowka.
    /// </summary>
    private void CopySelectedFiles()
    {
        if (this.ListBoxFiles.SelectedItems.Count == 0) // Nic nie jest zaznaczone
        {
            this.StatusBarItemInfo.Content = "Operacja wymaga zaznaczenia co najmniej jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        StringCollection filePaths = [];

        foreach (PdfFile selectedFile in this.ListBoxFiles.SelectedItems)
        {
            filePaths.Add(selectedFile.FilePath);
        }

        // Ustaw listę plików w schowku
        Clipboard.SetFileDropList(filePaths);

        this.StatusBarItemInfo.Content = $"Skopiowano {this.ListBoxFiles.SelectedItems.Count} {(this.ListBoxFiles.SelectedItems.Count == 1 ? "plik" : "plików")} do schowka.";

        this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Green);
    }

    /// <summary>
    /// Funkcja wycinająca zaznaczone pliki do schowka.
    /// </summary>
    private void CutSelectedFiles()
    {
        if (this.ListBoxFiles.SelectedItems.Count == 0) // Nic nie jest zaznaczone
        {
            this.StatusBarItemInfo.Content = "Operacja wymaga zaznaczenia co najmniej jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        StringCollection filePaths = [];

        foreach (PdfFile selectedFile in this.ListBoxFiles.SelectedItems)
        {
            filePaths.Add(selectedFile.FilePath);
        }

        DataObject data = new(); // Utwórz nowy obiekt DataObject

        data.SetFileDropList(filePaths); // Ustaw listę plików do przeniesienia

        MemoryStream dropEffect = new([2, 0, 0, 0]); // 4 bajty określające efekt przeniesienia. Wartość 2 oznacza "przenieś" (Move/Cut).

        // Ustaw efekt przeniesienia w danych
        data.SetData("Preferred DropEffect", dropEffect);

        // Ustaw dane w schowku
        Clipboard.SetDataObject(data, true);

        this.StatusBarItemInfo.Content = $"Wycięto {this.ListBoxFiles.SelectedItems.Count} {(this.ListBoxFiles.SelectedItems.Count == 1 ? "plik" : "plików")}.";

        this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);
    }

    /// <summary>
    /// Obsługuje kliknięcie przycisku odświeżania katalogu.
    /// </summary>
    private void ButtonRefreshDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(this._activeDirectory) && Directory.Exists(this._activeDirectory))
        {
            string? selectedFilePath = this.SelectedPdfFile?.FilePath; // Zapamiętaj ścieżkę zaznaczonego pliku, jeśli istnieje

            this.LoadFiles([this._activeDirectory]); // Przeładuj pliki z ostatnio używanego katalogu

            if (selectedFilePath != null)
            {
                PdfFile? fileToSelect = this.PdfFiles.FirstOrDefault(f => f.FilePath == selectedFilePath);

                if (fileToSelect != null)
                {
                    this.FocusAndScrollToListBoxItem(fileToSelect);
                }
            }
        }
        else
        {
            this.StatusBarItemInfo.Content = "Brak katalogu do odświeżenia.";
        }
    }

    /// <summary>
    /// Usuwa zaznaczone pliki z listy i dysku po potwierdzeniu przez użytkownika.
    /// </summary>
    private void DeleteSelectedFiles()
    {
        List<PdfFile> itemsToDelete = [.. this.ListBoxFiles.SelectedItems.Cast<PdfFile>()]; // Skopiuj zaznaczone elementy do listy

        if (itemsToDelete.Count == 0)
        {
            this.StatusBarItemInfo.Content = "Operacja wymaga zaznaczenia co najmniej jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        // Zapamiętaj indeks pierwszego zaznaczonego pliku
        int startIndex = this.ListBoxFiles.SelectedIndex;

        if (MessageBox.Show($"Czy na pewno chcesz usunąć {itemsToDelete.Count} {(itemsToDelete.Count == 1 ? "plik" : "plików")}?", "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (PdfFile item in itemsToDelete)
        {
            try
            {
                File.Delete(item.FilePath);

                this.PdfFiles.Remove(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania pliku: {item.FileName}\n\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Wybierz następny plik lub ostatni na liście, jeśli to był ostatni plik
        if (this.PdfFiles.Count > 0)
        {
            int nextIndex = Math.Min(startIndex, this.PdfFiles.Count - 1);

            this.ListBoxFiles.SelectedIndex = nextIndex;

            // Ustaw fokus i przewiń do nowego elementu
            if (this.ListBoxFiles.SelectedItem is PdfFile selected)
            {
                this.FocusAndScrollToListBoxItem(selected);
            }
        }
    }

    /// <summary>
    /// Dodaje plik do kolekcji PdfFiles, zachowując sortowanie naturalne.
    /// </summary>
    /// <param name="fileToAdd">Plik do dodania.</param>
    private void AddAndSortFile(PdfFile fileToAdd)
    {
        int insertIndex = 0;

        while (insertIndex < this.PdfFiles.Count && this._naturalComparer.Compare(this.PdfFiles[insertIndex].FilePath, fileToAdd.FilePath) < 0)
        {
            insertIndex++;
        }

        this.PdfFiles.Insert(insertIndex, fileToAdd);
    }

    /// <summary>
    /// Funkcja obsługująca podwójne kliknięcie w TextBoxDirectory, otwierająca Eksplorator Windows w aktywnym katalogu.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TextBoxDirectory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        string dir = this._activeDirectory;

        // Brak katalogu do otwarcia
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            return;
        }

        try
        {
            // Otwórz Eksplorator Windows w podanym katalogu
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true, });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie udało się otworzyć Eksploratora Windows.\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        e.Handled = true; // Zaznacz zdarzenie jako obsłużone
    }

    /// <summary>
    /// Funkcja obsługująca kliknięcie przycisku "Przenumeruj pliki".
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonRenumberFiles_Click(object sender, RoutedEventArgs e)
    {
        // Nic nie jest na liście
        if (this.PdfFiles.Count == 0)
        {
            this.StatusBarItemInfo.Content = "Brak plików do przenumerpowania.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        // Sprawdzenie, czy wszystkie pliki z listy fizycznie istnieją na dysku przed przenumerowaniem
        if (this.PdfFiles.Any(f => !File.Exists(f.FilePath)))
        {
            MessageBox.Show("Lista plików na liście i dysku jest różna!\n\nOdśwież katalog przed przenumerowaniem.",
                "Brakujące pliki",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        // Potwierdzenie od użytkownika
        if (MessageBox.Show(
                $"Czy na pewno przenumerować {this.PdfFiles.Count} plików od 1.pdf do {this.PdfFiles.Count}.pdf?\nOperacja jest nieodwracalna.",
                "Potwierdzenie przenumerowania",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        Mouse.OverrideCursor = Cursors.Wait;

        List<PdfFile> filesToRenumber = [.. this.PdfFiles]; // Tworzymy kopię listy, aby operować na stabilnej kolekcji

        try
        {
            // Etap 1: Zmień nazwy plików na tymczasowe, aby uniknąć kolizji nazw
            Dictionary<string, string> tempMapping = new(StringComparer.OrdinalIgnoreCase); // Mapa: stara ścieżka -> tymczasowa ścieżka

            HashSet<string> pdfFileNames = Directory.GetFiles(this._activeDirectory, "*.pdf")
                .Select(Path.GetFileName)
                .OfType<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int counter = 1;

            // musimy sprawdziź czy pliku o tym numerze nie ma juz na dysku, należy mu nadać nazwę tymczasową, a później zmienić na kolejną
            foreach (PdfFile pdfFile in filesToRenumber)
            {
                string newName = $"{counter}.pdf"; // Nowa nazwa pliku
                string newPath = Path.Combine(this._activeDirectory, newName); // Nowa pełna ścieżka pliku

                // jeśli plik o tej nazwie już istnieje na dysku, nadajemy nazwę tymczasową
                if (pdfFileNames.Contains(newName))
                {
                    string tempName = $"__{counter}__{Guid.NewGuid()}.pdf";
                    string tempPath = Path.Combine(this._activeDirectory, tempName);

                    File.Move(pdfFile.FilePath, tempPath); // Zmień nazwę na tymczasową

                    tempMapping[tempPath] = newPath; // Zmapuj tymczasową ścieżkę na docelową
                }
                else
                {
                    tempMapping[pdfFile.FilePath] = newPath; // Zmapuj starą ścieżkę na docelową
                }

                counter++;
            }

            // zmiana nazw plików
            foreach (KeyValuePair<string, string> newMapping in tempMapping)
            {
                File.Move(newMapping.Key, newMapping.Value);
            }

            // Odśwież listę, aby pokazać nowe nazwy i kolejność
            this.LoadFiles([this._activeDirectory]);

            // ustaw fokus na pierwszy plik na liście
            if (this.PdfFiles.Count > 0)
            {
                this.FocusAndScrollToListBoxItem(this.PdfFiles[0]);
            }

            this.StatusBarItemInfo.Content = $"Przenumerowano {counter - 1} plików!";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Green);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Wystąpił błąd podczas przenumerowania plików:\n" + ex.Message, "Błąd krytyczny", MessageBoxButton.OK, MessageBoxImage.Error);

            // W przypadku błędu warto odświeżyć listę, aby odzwierciedlić aktualny stan na dysku
            this.LoadFiles([this._activeDirectory]);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }
}