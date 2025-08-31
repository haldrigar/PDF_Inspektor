// ====================================================================================================
// <copyright file="MainWindow.xaml.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.Collections.ObjectModel;
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

    // Przechowuje strumień PDF w pamięci
    private MemoryStream? _pdfStream;

    // Monitoruje zmiany w katalogu
    private FileSystemWatcher _fileWatcher = new();

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
        this.Top = this._appSettings.WindowTop;
        this.Left = this._appSettings.WindowLeft;
        this.Width = this._appSettings.WindowWidth;
        this.Height = this._appSettings.WindowHeight;

        // Upewnij się, że okno jest widoczne na ekranie
        Tools.EnsureWindowIsOnScreen(this);

        // Sprawdź i rozpakuj narzędzia zdefiniowane w konfiguracji
        foreach (ExternalTool tool in this._appSettings.Tools)
        {
            Tools.EnsureAndUnpackTool(tool);
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

        this._pdfStream?.Dispose(); // Zwolnij zasoby MemoryStream
        this._fileWatcher?.Dispose(); // Zwolnij zasoby FileSystemWatcher
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

    // Obsługa przeciągania i upuszczania plików
    private void ListBoxFiles_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

        e.Handled = true;
    }

    // Obsługa upuszczania plików
    private void ListBoxFiles_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] droppedItems)
        {
            // Załaduj pliki PDF z upuszczonych elementów
            this.LoadFiles(droppedItems);

            // Ustawienie zaznaczenia na pierwszy plik z listy, jeśli lista nie jest pusta
            if (this.PdfFiles.Count > 0)
            {
                // Ustawienie zaznaczenia na pierwszy plik z listy
                this.FocusAndScrollToListBoxItem(this.PdfFiles.First());
            }
        }
    }

    // Obsługa przycisku "Otwórz katalog"
    private void ButtonMonitorDirectory_Click(object sender, RoutedEventArgs e)
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

        // Wyświetl w TextBox nazwę wybranego folderu, ale bez ścieżki (tylko nazwa katalogu)
        this.TextBoxDirectory.Text = Path.GetFileName(dialog.FolderName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        // Załaduj istniejące pliki z wybranego folderu
        this.LoadFiles([dialog.FolderName]);

        // Ustawienie zaznaczenia na ostatni plik z listy, jeśli lista nie jest pusta
        if (this.PdfFiles.Count > 0)
        {
            // Ustawienie zaznaczenia na ostatni plik z listy
            this.FocusAndScrollToListBoxItem(this.PdfFiles.Last());
        }

        // Ustawienie FileSystemWatcher na wybrany folder
        this.SetupFileSystemWatcher(dialog.FolderName);
    }

    // Funkcja ładująca pliki PDF z podanych ścieżek
    private void LoadFiles(IEnumerable<string> paths)
    {
        this.PdfFiles.Clear(); // Wyczyść istniejącą listę plików

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
                filesToLoad.AddRange(Directory.GetFiles(path, "*.pdf", SearchOption.AllDirectories));
            }
        }

        // Sortowanie plików w kolejności naturalnej
        filesToLoad.Sort(new NaturalStringComparer());

        // Dodanie plików do ObservableCollection
        foreach (string file in filesToLoad)
        {
            this.PdfFiles.Add(new PdfFile(file));
        }

        // Ustawienie FileSystemWatcher, jeśli jest dokładnie jeden unikalny katalog
        if (this.PdfFiles.Count > 0)
        {
            // Pobierz unikalne katalogi z listy plików PDF
            List<string> directoriesList = [.. this.PdfFiles.Select(p => p.DirectoryName).Distinct(StringComparer.OrdinalIgnoreCase).Distinct()];

            // Ustaw FileSystemWatcher tylko wtedy, gdy jest dokładnie jeden unikalny katalog
            if (directoriesList.Count == 1)
            {
                this.SetupFileSystemWatcher(directoriesList.First());

                // Wyświetl w TextBox nazwę wybranego folderu, ale bez ścieżki (tylko nazwa katalogu)
                this.TextBoxDirectory.Text = Path.GetFileName(directoriesList.First().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            else
            {
                this.TextBoxDirectory.Text = "Pliki z różnych folderów!";
            }
        }
        else // Jeśli lista jest pusta, wyczyść widok PdfViewer
        {
            this.PdfViewer.Unload(true);
        }
    }

    // Obsługa zmiany zaznaczenia w ListBox
    private void ListBoxFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Sprawdzamy, czy zaznaczony element jest typu PdfFile
        if (this.ListBoxFiles.SelectedItem is not PdfFile selectedPdfFile)
        {
            // Jeśli nic nie jest zaznaczone (np. po usunięciu ostatniego elementu), wyczyść podgląd.
            this._pdfStream?.Dispose();
            this.PdfViewer.Unload();
            return;
        }

        // Aktualizujemy właściwość SelectedPdfFile
        this.SelectedPdfFile = selectedPdfFile;
        this.ReloadPdfView(selectedPdfFile);
    }

    /// <summary>
    /// Funkcja przeładowująca widok PdfViewer z pliku PDF.
    /// </summary>
    /// <param name="pdfFile">Plik PDF.</param>
    private void ReloadPdfView(PdfFile pdfFile)
    {
        if (!File.Exists(pdfFile.FilePath))
        {
            // Plik nie istnieje na dysku, usuń go z listy i zakończ.
            this.PdfFiles.Remove(pdfFile);
            return;
        }

        try
        {
            // 1. Zamknij i zwolnij poprzedni strumień w pamięci, jeśli istnieje.
            this._pdfStream?.Dispose();

            // 2. Otwórz plik na dysku tylko na chwilę, aby skopiować jego zawartość.
            byte[] fileBytes = File.ReadAllBytes(pdfFile.FilePath);

            // 3. Utwórz nowy strumień w pamięci na podstawie wczytanych bajtów.
            this._pdfStream = new MemoryStream(fileBytes);

            // 4. Załaduj dokument do PdfViewer ze strumienia w pamięci.
            //    Plik na dysku jest już wolny!
            this.PdfViewer.Load(this._pdfStream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas ładowania pliku do pamięci: {ex.Message}");
            MessageBox.Show($"Nie udało się załadować pliku: {pdfFile.FileName}\n\n{ex.Message}", "Błąd ładowania", MessageBoxButton.OK, MessageBoxImage.Warning);

            // W razie błędu wyczyść podgląd
            this._pdfStream?.Dispose();
            this.PdfViewer.Unload();
        }
    }

    // Obsługa zdarzenia DocumentLoaded kontrolki PdfViewer
    private void PdfViewer_DocumentLoaded(object sender, EventArgs args)
    {
        this.PdfViewer.ZoomMode = ZoomMode.FitPage;

        this.PdfViewer.Width = double.NaN;
        this.PdfViewer.Height = double.NaN;

        this.PdfViewer.CursorMode = PdfViewerCursorMode.HandTool;

        // =========================================== SPRAWDZENIE BŁĘDÓW ===========================================
        List<string> errors = []; // Lista błędów do wyświetlenia

        (int dpiX, int dpiY) = PDFTools.GetDPI(this.PdfViewer.LoadedDocument);

        if (Math.Abs(dpiX - 300) > 1 || Math.Abs(dpiY - 300) > 1)
        {
            errors.Add("BŁĄD DPI");
        }

        // Sprawdź, czy FileWatcher jest aktywny i ma poprawną ścieżkę
        if (!string.IsNullOrEmpty(this._fileWatcher.Path) && Directory.Exists(this._fileWatcher.Path))
        {
            // sprawdź ilość pliku w monitorowanym katalogu
            int totalFileCount = Directory.GetFiles(this._fileWatcher.Path, "*.pdf").Length;

            if (totalFileCount != this.PdfFiles.Count)
            {
                errors.Add("BŁĄD ILOŚCI PLIKÓW");
            }
        }

        // ==========================================================================================================

        // Jeżeli jest zaznaczony plik
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            if (this.PdfViewer.LoadedDocument.Pages.Count > 0) // Sprawdź, czy dokument ma strony
            {
                int rotation = this.PdfViewer.LoadedDocument.Pages[0].Rotation switch
                {
                    PdfPageRotateAngle.RotateAngle0 => 0,
                    PdfPageRotateAngle.RotateAngle90 => 90,
                    PdfPageRotateAngle.RotateAngle180 => 180,
                    PdfPageRotateAngle.RotateAngle270 => 270,
                    _ => throw new NotImplementedException()
                };
                this.StatusBarItemMain.Content = $"Plik [{this.ListBoxFiles.SelectedIndex + 1}/{this.PdfFiles.Count}]: {selectedPdfFile.FileName} | Rozmiar: {selectedPdfFile.FileSize / 1024.0:F0} KB | DPI: {dpiX} x {dpiY} | Obrót: {rotation}";
            }
            else
            {
                errors.Add("PUSTY DOKUMENT");
            }

            // Po każdym załadowaniu podglądu upewnij się, że fokus jest na właściwym elemencie listy.
            this.FocusAndScrollToListBoxItem(selectedPdfFile);
        }
        else // Jeżeli nie ma zaznaczonego pliku
        {
            errors.Add("BŁĄD ŁADOWANIA PLIKU");
        }

        // ------------------------------------------------------------------------------
        // Jeżeli są błędy
        // ------------------------------------------------------------------------------
        if (errors.Count > 0)
        {
            string errorMessage = string.Join("; ", errors);

            this.StatusBarItemInfo.Content = errorMessage;

            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Red);
        }
        else // Jeżeli nie ma błędów
        {
            this.StatusBarItemInfo.Content = "OK";
            this.StatusBarItemInfo.Background = new SolidColorBrush(Colors.Green);
        }

        Mouse.OverrideCursor = null;
    }

    // Obsługa przycisków obrotu
    private void ButtonRotate_OnClick(object sender, RoutedEventArgs e)
    {
        bool rotateRight = e is not MouseButtonEventArgs { ChangedButton: MouseButton.Right };

        this.RotateAndSave(rotateRight);

        e.Handled = true; // Zaznacz zdarzenie jako obsłużone
    }

    // Obsługa klawiszy strzałek do obrotu
    private void ListBoxFiles_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Right)
        {
            this.RotateAndSave(true);

            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            this.RotateAndSave(false);

            e.Handled = true;
        }
        else if (e.Key == Key.Delete) // Obsługa klawisza Delete do usuwania pliku
        {
            // Sprawdzenie, czy jest zaznaczony plik PDF
            if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
            {
                try
                {
                    if (File.Exists(selectedPdfFile.FilePath))
                    {
                        File.Delete(selectedPdfFile.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas usuwania pliku:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            e.Handled = true;
        }
    }

    // Funkcja obracająca stronę i zapisująca zmiany
    private void RotateAndSave(bool rotateRight)
    {
        if (this.ListBoxFiles.SelectedItem is not PdfFile selectedPdfFile)
        {
            return;
        }

        Mouse.OverrideCursor = Cursors.Wait;

        bool success = PDFTools.RotateAndSave(selectedPdfFile.FilePath, rotateRight);

        if (!success)
        {
            MessageBox.Show("Nie udało się obrócić i zapisać pliku.", "Błąd operacji", MessageBoxButton.OK, MessageBoxImage.Error);

            // Jeśli operacja się nie udała, musimy zresetować kursor ręcznie.
            Mouse.OverrideCursor = null;
        }

        // Jeśli operacja się udała, NIE resetujemy kursora tutaj.
        // Zrobi to za nas metoda PdfViewer_DocumentLoaded po odświeżeniu widoku.
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
        if (this.ListBoxFiles.SelectedItem is not PdfFile selectedPdfFile)
        {
            return;
        }

        ExternalTool? tool = this._appSettings.Tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        if (tool == null)
        {
            MessageBox.Show($"Nie znaleziono konfiguracji dla narzędzia '{toolName}' w pliku ustawień.", "Błąd konfiguracji", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string editedFilePath = selectedPdfFile.FilePath;
        DateTime lastWriteTimeBeforeEdit = selectedPdfFile.LastWriteTime; // Zapamiętaj czas modyfikacji
        string executablePath = Path.Combine(AppContext.BaseDirectory, tool.ExecutablePath);

        Tools.StartExternalProcess(
            executablePath,
            editedFilePath,
            () =>
            {
                this.Dispatcher.InvokeAsync(() =>
                    {
                        var fileInfo = new FileInfo(editedFilePath);
                        fileInfo.Refresh();

                        // Jeśli plik NIE został zmodyfikowany, przywróć fokus ręcznie.
                        // Jeśli został, DocumentLoaded zrobi to za nas.
                        if (fileInfo.Exists && fileInfo.LastWriteTime == lastWriteTimeBeforeEdit)
                        {
                            Debug.WriteLine("Plik niezmodyfikowany. Ręczne przywracanie fokusu.");
                            this.FocusAndScrollToListBoxItem(selectedPdfFile);
                        }
                    });
            });
    }

    // Ustawienie FileSystemWatcher do monitorowania nowo dodanych plików PDF
    private void SetupFileSystemWatcher(string path)
    {
        // Zwolnij zasoby poprzedniego FileSystemWatcher, jeśli istnieje
        this._fileWatcher?.Dispose();

        this._fileWatcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
            Filter = "*.pdf",
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
            InternalBufferSize = 65536,
        };

        this._fileWatcher.Created += this.FileWatcher_Created; // Obsługa zdarzenia utworzenia pliku
        this._fileWatcher.Deleted += this.FileWatcher_Deleted; // Obsługa zdarzenia usunięcia pliku
        this._fileWatcher.Renamed += this.FileWatcher_Renamed; // Obsługa zdarzenia zmiany nazwy pliku
        this._fileWatcher.Changed += this.FileWatcher_Changed; // Obsługa zdarzenia zmiany pliku
    }

    // Obsługa zdarzenia utworzenia pliku
    private void FileWatcher_Created(object sender, FileSystemEventArgs e)
    {
        this.Dispatcher.Invoke(() =>
        {
            try
            {
                this.StatusBarItemInfo.Content = $"Wykryto nowy plik: {e.Name}...";

                if (!Tools.WaitForFile(e.FullPath))
                {
                    this.StatusBarItemInfo.Content = $"Nie można uzyskać dostępu do: {e.Name}";
                    return;
                }

                if (this.PdfFiles.Any(p => p.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                Debug.WriteLine($"FileWatcher_Created => Znaleziono nowy plik: {e.FullPath}!");
                var fileToAdd = new PdfFile(e.FullPath);
                this.AddAndSortFile(fileToAdd); // <-- Użycie nowej metody
                this.FocusAndScrollToListBoxItem(fileToAdd);
                this.StatusBarItemInfo.Content = $"Dodano: {e.Name}";
            }
            catch (Exception ex)
            {
                this.StatusBarItemInfo.Content = $"Błąd podczas dodawania pliku: {e.Name}";
                Debug.WriteLine($"Nieoczekiwany błąd w FileWatcher_Created: {ex.Message}");
            }
        });
    }

    // Obsługa zdarzenia usunięcia pliku
    private void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        this.Dispatcher.Invoke(() =>
        {
            try
            {
                PdfFile? fileToRemove = this.PdfFiles.FirstOrDefault(p => p.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));

                if (fileToRemove == null)
                {
                    return;
                }

                Debug.WriteLine($"FileWatcher_Deleted => Wykryto usunięcie pliku: {e.FullPath}!");

                int removedIndex = this.PdfFiles.IndexOf(fileToRemove);
                bool wasSelected = this.SelectedPdfFile == fileToRemove;

                if (wasSelected)
                {
                    this.PdfViewer.Unload(true);
                }

                this.PdfFiles.Remove(fileToRemove);

                if (this.PdfFiles.Count == 0)
                {
                    this.StatusBarItemMain.Content = "Gotowy!";
                    this.StatusBarItemInfo.Content = "Wszystkie pliki zostały usunięte.";
                    return;
                }

                if (wasSelected)
                {
                    int newIndex = removedIndex >= this.PdfFiles.Count ? this.PdfFiles.Count - 1 : removedIndex;
                    this.ListBoxFiles.SelectedIndex = newIndex;
                    if (this.ListBoxFiles.SelectedItem is PdfFile newSelectedItem)
                    {
                        this.FocusAndScrollToListBoxItem(newSelectedItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Nieoczekiwany błąd w FileWatcher_Deleted: {ex.Message}");
            }
        });
    }

    // Obsługa zdarzenia zmiany nazwy pliku
    private void FileWatcher_Renamed(object sender, RenamedEventArgs e)
    {
        this.Dispatcher.Invoke(() =>
        {
            try
            {
                PdfFile? fileToRename = this.PdfFiles.FirstOrDefault(p => p.FilePath.Equals(e.OldFullPath, StringComparison.OrdinalIgnoreCase));
                if (fileToRename == null)
                {
                    return;
                }

                Debug.WriteLine($"FileWatcher_Renamed => Zmieniono nazwę pliku na: {e.FullPath}!");
                this.PdfFiles.Remove(fileToRename);
                fileToRename.FilePath = e.FullPath;
                fileToRename.FileName = Path.GetFileName(e.FullPath);
                fileToRename.DirectoryName = Path.GetDirectoryName(e.FullPath) ?? string.Empty;
                this.AddAndSortFile(fileToRename); // <-- Użycie nowej metody
                this.FocusAndScrollToListBoxItem(fileToRename);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Nieoczekiwany błąd w FileWatcher_Renamed: {ex.Message}");
            }
        });
    }

    // Obsługa zdarzenia zmiany pliku, jego rozmiaru lub daty modyfikacji
    private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        // Wykonaj aktualizację w wątku UI
        this.Dispatcher.Invoke(() =>
        {
            try
            {
                // Sprawdź, czy plik istnieje na dysku. Jeśli nie, nic nie rób.
                if (!File.Exists(e.FullPath))
                {
                    return;
                }

                // Znajdź plik na liście w aplikacji
                PdfFile? fileToUpdate = this.PdfFiles.FirstOrDefault(p => p.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));

                if (fileToUpdate == null)
                {
                    return; // Pliku nie ma na naszej liście, ignoruj
                }

                // Pobierz aktualne informacje o pliku z dysku
                var fileInfo = new FileInfo(e.FullPath);
                long newFileSize = fileInfo.Length;
                DateTime newLastWriteTime = fileInfo.LastWriteTime;

                // KLUCZOWY WARUNEK: Sprawdź, czy zmiana nie została już przetworzona.
                // Jeśli czas modyfikacji jest taki sam, ignorujemy to zdarzenie.
                if (fileToUpdate.LastWriteTime >= newLastWriteTime)
                {
                    return;
                }

                // Mamy nową, rzeczywistą zmianę. Aktualizujemy dane.
                Debug.WriteLine($"FileWatcher_Changed => Aktualizacja pliku: {e.FullPath}!");
                fileToUpdate.FileSize = newFileSize;
                fileToUpdate.LastWriteTime = newLastWriteTime; // ustawiamy nowy czas

                // Jeśli zmieniony plik jest aktualnie zaznaczony, przeładuj podgląd.
                if (this.SelectedPdfFile == fileToUpdate)
                {
                    this.ReloadPdfView(fileToUpdate);
                }
                else // Jeśli nie jest zaznaczony, ustaw fokus na ten plik, aby użytkownik zobaczył zmianę.
                {
                    this.FocusAndScrollToListBoxItem(fileToUpdate);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Błąd I/O podczas sprawdzania pliku: {e.FullPath}. Błąd: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Nieoczekiwany błąd w FileWatcher_Changed: {ex.Message}");
            }
        });
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

    // Obsługa przycisku "Usuń"
    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        // Sprawdzenie, czy jest zaznaczony plik PDF
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            try
            {
                if (File.Exists(selectedPdfFile.FilePath))
                {
                    File.Delete(selectedPdfFile.FilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania pliku:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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
}