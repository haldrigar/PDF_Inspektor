// ====================================================================================================
// <copyright file="MainWindow.xaml.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
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

using Syncfusion.Pdf;
using Syncfusion.Windows.PdfViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow
{
    // Przechowuje ustawienia aplikacji
    private readonly AppSettings _appSettings;

    // Dodaj pole do porównywania nazw plików, aby nie tworzyć go za każdym razem
    private readonly NaturalStringComparer _naturalComparer = new();

    // Przechowuje strumień aktualnie załadowanego pliku PDF
    private MemoryStream _selectedPdfStream = new();

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
    public ObservableCollection<PdfFile> PdfFiles { get; } = [];

    /// <summary>
    /// Pobiera lub ustawia aktualnie zaznaczony plik PDF w interfejsie użytkownika.
    /// </summary>
    public PdfFile? SelectedPdfFile { get; set; }

    // Funkcje obsługująca uruchomienie okna
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait; // Ustaw kursor na "czekanie"

        this.Top = this._appSettings.WindowTop;
        this.Left = this._appSettings.WindowLeft;
        this.Width = this._appSettings.WindowWidth;
        this.Height = this._appSettings.WindowHeight;

        // Upewnij się, że okno jest widoczne na ekranie
        Tools.EnsureWindowIsOnScreen(this);

        // Sprawdź i rozpakuj narzędzia zdefiniowane w konfiguracji
        foreach (ExternalTool tool in this._appSettings.Tools)
        {
            // Rozpakuj narzędzie, jeśli to konieczne
            Tools.EnsureAndUnpackTool(tool);
        }

        // Załadowanie plików z ostatnio używanego katalogu, jeśli istnieje.
        if (!string.IsNullOrEmpty(this._appSettings.LastUsedDirectory) && Directory.Exists(this._appSettings.LastUsedDirectory))
        {
            // Załaduj pliki z ostatnio używanego katalogu
            this.LoadFiles([this._appSettings.LastUsedDirectory]);

            // jeśli udało się załadować pliki
            if (this.PdfFiles.Count > 0)
            {
                // Spróbuj przywrócić zaznaczenie
                PdfFile? fileToSelect = this.PdfFiles.FirstOrDefault(f => f.FilePath == this._appSettings.LasUsedFilePath);

                // Ustawienie zaznaczenia na znaleziony plik lub na pierwszy plik na liście
                this.FocusAndScrollToListBoxItem(fileToSelect ?? this.PdfFiles.First());
            }
        }
    }

    // Funkcja obsługująca zamknięcie okna
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

    // Obsługa zmiany rozmiaru okna
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
        // ============================================== USUNIĘCIE Z LISTY SKASOWANYCH PLIKÓW ==================================
        bool filesWereRemoved = false;

        // Używamy pętli 'for' od końca, aby bezpiecznie usuwać elementy z kolekcji.
        for (int i = this.PdfFiles.Count - 1; i >= 0; i--)
        {
            PdfFile fileToCheck = this.PdfFiles[i];

            if (!File.Exists(fileToCheck.FilePath))
            {
                // Plik nie istnieje na dysku, więc usuwamy go z listy.
                this.PdfFiles.RemoveAt(i);

                filesWereRemoved = true;

                Debug.WriteLine($"Plik '{fileToCheck.FileName}' nie istnieje. Usunięto z listy.");
            }
        }

        if (filesWereRemoved)
        {
            this.StatusBarItemInfo.Content = "Odświeżono listę, usunięto z listy skasowane pliki.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            // Jeśli po usunięciu nie ma już żadnych plików, zakończ.
            if (this.PdfFiles.Count == 0)
            {
                this.PdfViewer.Unload(true);
                this._selectedPdfStream.Dispose();

                return;
            }
        }

        // =============================== SPRAWDZENIE CZY ZAZNACZONY PLIK BYŁ MODYFIKOWANY POZA PROGRAMEM ====================
        // Sprawdź, czy jakikolwiek plik jest zaznaczony
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile && this.ListBoxFiles.SelectedItems.Count == 1)
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

            this.FocusAndScrollToListBoxItem(selectedPdfFile);
        }
    }

    // Obsługa przeciągania i upuszczania plików
    private void ListBoxFiles_DragOver(object sender, DragEventArgs e)
    {
        // Sprawdzamy czy przeciągane są pliki/foldery.
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

        e.Handled = true;
    }

    // Obsługa upuszczania plików
    private void ListBoxFiles_Drop(object sender, DragEventArgs e)
    {
        // Sprawdź, czy są upuszczone pliki
        if (e.Data.GetData(DataFormats.FileDrop) is string[] droppedItems)
        {
            // Załaduj pliki PDF z upuszczonych elementów
            this.LoadFiles(droppedItems);
        }
    }

    // Obsługa przycisku "Otwórz katalog"
    private void ButtonOpenDirectory_Click(object sender, RoutedEventArgs e)
    {
        // Otwórz okno dialogowe wyboru folderu
        Microsoft.Win32.OpenFolderDialog dialog = new()
        {
            Title = "Wybierz folder do przetwarzania",
        };

        // Jeśli użytkownik anulował wybór, zakończ działanie funkcji
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        // Załaduj istniejące pliki z wybranego folderu
        this.LoadFiles([dialog.FolderName]);

        // Zapisz wybrany katalog jako ostatnio używany
        this._appSettings.LastUsedDirectory = dialog.FolderName;
    }

    // Funkcja ładująca pliki PDF z podanych ścieżek
    private void LoadFiles(IEnumerable<string> paths)
    {
        Mouse.OverrideCursor = Cursors.Wait; // Ustaw kursor na "czekanie"

        /* ------------------------------------------------------ */
        this.PdfFiles.Clear(); // Wyczyść istniejącą listę plików

        this.PdfViewer.Unload(true); // Wyczyść widok PdfViewer

        this._selectedPdfStream?.Dispose(); // Zwolnij poprzedni strumień, jeśli istnieje
        /* ------------------------------------------------------ */

        List<string> filesToLoad = []; // Lista plików do załadowania

        // Przetwarzanie każdej ścieżki
        foreach (string path in paths)
        {
            // Sprawdzenie, czy ścieżka jest plikiem PDF lub katalogiem
            if (File.Exists(path) && path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) // Pojedynczy plik PDF
            {
                filesToLoad.Add(path);
            }
            else if (Directory.Exists(path)) // Katalog
            {
                filesToLoad.AddRange(Directory.GetFiles(path, "*.pdf", SearchOption.TopDirectoryOnly)); // Rekursywnie dodaj wszystkie pliki PDF z katalogu
            }
        }

        // Jeśli lista plików do załadowania nie jest pusta
        if (filesToLoad.Count > 0)
        {
            // sprawdzenie czy wszystkie pliki pochodzą z jednego katalogu
            int uniqueDirectoryCount = filesToLoad.Select(Path.GetDirectoryName).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            if (uniqueDirectoryCount > 1) // jeśli pliki pochodzą z więcej niż jednego katalogu
            {
                Mouse.OverrideCursor = null;

                MessageBox.Show("Wszystkie pliki muszą pochodzić z jednego folderu!", "Sprawdzenie ilości folderów", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return;
            }

            // Sortowanie plików w kolejności naturalnej
            filesToLoad.Sort(new NaturalStringComparer());

            // Dodanie plików do ObservableCollection
            foreach (string file in filesToLoad)
            {
                this.PdfFiles.Add(new PdfFile(file));
            }

            // Ustaw ostatnio używany katalog na pierwszy katalog z listy. Będzie i tak jeden katalog, bo ładujemy z jednego katalogu lub z jednego upuszczonego katalogu.
            this._appSettings.LastUsedDirectory = this.PdfFiles.First().DirectoryName;

            // Aktualizuj TextBox z katalogiem
            this.TextBoxDirectory.Text = Path.GetFileName(this.PdfFiles.First().DirectoryName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            // Zaznacz pierwszy plik z listy
            this.FocusAndScrollToListBoxItem(this.PdfFiles.First());

            // Przywrócenie kursora jest tu nie potrzebne. Zajmie się tym DocumentLoaded po załadowaniu pierwszego pliku.
        }
        else // Jeśli lista jest pusta, wyczyść widok PdfViewer
        {
            Mouse.OverrideCursor = null;
        }
    }

    // Obsługa zmiany zaznaczenia w ListBox
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

            this.PdfViewer.Unload();

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

        // Spróbuj załadować do PdfViewer zaznczony plik
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

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
            this.PdfViewer.Unload();

            this._selectedPdfStream.Dispose();

            Mouse.OverrideCursor = null;
        }
    }

    // Obsługa zdarzenia DocumentLoaded kontrolki PdfViewer
    // Jest wywoływane po zakończeniu ładowania dokumentu PDF.
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
            // Zapisz ścieżkę ostatnio używanego pliku
            this._appSettings.LasUsedFilePath = selectedPdfFile.FilePath;

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

    // Obsługa przycisków obrotu
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

    // Obsługa przycisku "Usuń"
    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        this.DeleteSelectedFiles(); // Funkcja usuwająca zaznaczone pliki
    }

    // Obsługa klawiszy na liście plików
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

    // Funkcja obracająca stronę i zapisująca zmiany
    private void RotateAndSave(bool rotateRight)
    {
        // Sprawdź, czy zaznaczony element jest typu PdfFile
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            Mouse.OverrideCursor = Cursors.Wait;

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

                Mouse.OverrideCursor = null; // Przywróć kursor
            }
        }
    }

    private void ButtonEditIrfanView_Click(object sender, RoutedEventArgs e)
    {
        this.LaunchConfiguredTool("IrfanView");
    }

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

            try
            {
                Process process = new()
                {
                    StartInfo = new ProcessStartInfo(executablePath, $"\"{fileToOpen}\"") { UseShellExecute = false },
                    EnableRaisingEvents = true,
                };

                process.Exited += (_, _) => { process.Dispose(); };

                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nie udało się uruchomić procesu dla pliku: {fileToOpen}.\n{ex.Message}",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            System.Windows.Threading.DispatcherPriority.Input);
    }

    // Obsługa zmiany nazwy pliku
    private void RenameSelectedFile()
    {
        if (this.ListBoxFiles.SelectedItems.Count != 1)
        {
            this.StatusBarItemInfo.Content = "Zmiana nazwy wymaga zaznaczenia jednego pliku.";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Orange);

            return;
        }

        if (this.ListBoxFiles.SelectedItem is PdfFile pdfToRename)
        {
            string originalFileName = pdfToRename.FileName;

            // Utwórz i pokaż okno dialogowe
            RenameWindow dialog = new(originalFileName)
            {
                Owner = this, // Ustaw właściciela, aby okno pojawiło się na środku aplikacji
            };

            // Jeśli użytkownik kliknął "OK"
            if (dialog.ShowDialog() == true)
            {
                string newFileName = dialog.NewFileName.Trim();

                // Walidacja nowej nazwy
                if (string.IsNullOrWhiteSpace(newFileName) || newFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newFileName == originalFileName)
                {
                    return; // Nazwa nieprawidłowa lub się nie zmieniła
                }

                if (Path.GetExtension(newFileName) != ".pdf")
                {
                    newFileName += ".pdf";
                }

                string newFilePath = Path.Combine(pdfToRename.DirectoryName, newFileName);

                try
                {
                    // Zmień nazwę pliku na dysku.
                    File.Move(pdfToRename.FilePath, newFilePath);

                    // Usuń stary obiekt z listy.
                    this.PdfFiles.Remove(pdfToRename);

                    pdfToRename.FileName = newFileName; // Zaktualizuj nazwę w obiekcie PdfFile
                    pdfToRename.FilePath = newFilePath; // Zaktualizuj ścieżkę w obiekcie PdfFile

                    // Dodaj zaktualizowany obiekt z powrotem do listy, używając metody sortującej.
                    this.AddAndSortFile(pdfToRename);

                    // Ustaw fokus na nowo posortowanym elemencie.
                    this.FocusAndScrollToListBoxItem(pdfToRename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nie udało się zmienić nazwy pliku.\nBłąd: {ex.Message}", "Błąd zmiany nazwy", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

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

    // Obsługa kliknięcia "Zmień nazwę..." w menu kontekstowym
    private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // zmiana nazwy zaznaczonego pliku
        this.RenameSelectedFile();
    }

    // Obsługa kliknięcia "Usuń" w menu kontekstowym
    private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // usunięcie zaznaczonych plików
        this.ButtonDelete_Click(sender, e);
    }

    // Obsługa kliknięcia "Obróć w prawo" w menu kontekstowym
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

    // Obsługa kliknięcia "Obróć w lewo" w menu kontekstowym
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

    // Obsługa kliknięcia "Wytnij" w menu kontekstowym
    private void CutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Wywołaj istniejącą, centralną metodę do wycinania plików.
        this.CutSelectedFiles();
    }

    // Obsługa kliknięcia "Kopiuj" w menu kontekstowym
    private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Wywołaj istniejącą, centralną metodę do kopiowania plików.
        this.CopySelectedFiles();
    }

    // Centralna metoda do kopiowania zaznaczonych plików do schowka
    private void CopySelectedFiles()
    {
        if (this.ListBoxFiles.SelectedItems.Count == 0) // Nic nie jest zaznaczone
        {
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

    // Centralna metoda do wycinania zaznaczonych plików do schowka
    private void CutSelectedFiles()
    {
        if (this.ListBoxFiles.SelectedItems.Count == 0) // Nic nie jest zaznaczone
        {
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
        if (!string.IsNullOrEmpty(this._appSettings.LastUsedDirectory) && Directory.Exists(this._appSettings.LastUsedDirectory))
        {
            // Przechowaj ID aktualnie zaznaczonego pliku
            string? selectedFilePath = this.SelectedPdfFile?.FilePath;

            // Załaduj ponownie pliki z ostatnio używanego katalogu
            this.LoadFiles([this._appSettings.LastUsedDirectory]);

            // Spróbuj przywrócić zaznaczenie
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
        List<PdfFile> itemsToDelete = [.. this.ListBoxFiles.SelectedItems.Cast<PdfFile>()];

        if (itemsToDelete.Count == 0)
        {
            return;
        }

        if (MessageBox.Show($"Czy na pewno chcesz usunąć {itemsToDelete.Count} {(itemsToDelete.Count == 1 ? "plik" : "plików")}?", "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (PdfFile item in itemsToDelete)
        {
            try
            {
                // Najpierw usuń plik z dysku.
                File.Delete(item.FilePath);

                // Jeśli powyższa operacja się udała, usuń plik z listy.
                this.PdfFiles.Remove(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania pliku: {item.FileName}\n\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
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
}