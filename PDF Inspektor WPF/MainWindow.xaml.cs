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
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Windows.PdfViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _jsonPath = Path.Combine(AppContext.BaseDirectory, "PDF_Inspektor.appsettings.json");
    private readonly AppSettings _appSettings;

    // Dodaj pole do porównywania nazw plików, aby nie tworzyć go za każdym razem
    private readonly NaturalStringComparer _naturalComparer = new();

    private MemoryStream? _pdfStream;

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

        if (!File.Exists(this._jsonPath))
        {
            this.SaveConfig();
        }

        string json = File.ReadAllText(this._jsonPath);
        this._appSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
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
        this.EnsureWindowIsOnScreen();

        // Sprawdź i rozpakuj narzędzia
        Tools.EnsureAndUnpackTool("IrfanView", "irfanview.zip", "IrfanViewPortable.exe");
        Tools.EnsureAndUnpackTool("GIMP", "gimp.zip", "GIMPPortable.exe");
    }

    // Funkcja obsługująca zamknięcie okna
    private void Window_Closed(object sender, EventArgs e)
    {
        this._appSettings.WindowTop = this.Top;
        this._appSettings.WindowLeft = this.Left;
        this._appSettings.WindowWidth = this.Width;
        this._appSettings.WindowHeight = this.Height;

        this.SaveConfig(); // Zapisz ustawienia do pliku JSON

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

    // Zapisz ustawienia do pliku JSON
    private void SaveConfig()
    {
        string json = JsonSerializer.Serialize(this._appSettings, JsonOptions);
        File.WriteAllText(this._jsonPath, json);
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
        OpenFolderDialog dialog = new()
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
            List<string> directoriesList = this.PdfFiles.Select(p => p.DirectoryName).Distinct(StringComparer.OrdinalIgnoreCase).Distinct().ToList();

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

        if (!File.Exists(selectedPdfFile.FilePath))
        {
            // Plik nie istnieje na dysku, usuń go z listy i zakończ.
            this.PdfFiles.Remove(selectedPdfFile);
            return;
        }

        try
        {
            // 1. Zamknij i zwolnij poprzedni strumień w pamięci, jeśli istnieje.
            this._pdfStream?.Dispose();

            // 2. Otwórz plik na dysku tylko na chwilę, aby skopiować jego zawartość.
            byte[] fileBytes = File.ReadAllBytes(selectedPdfFile.FilePath);

            // 3. Utwórz nowy strumień w pamięci na podstawie wczytanych bajtów.
            this._pdfStream = new MemoryStream(fileBytes);

            // 4. Załaduj dokument do PdfViewer ze strumienia w pamięci.
            //    Plik na dysku jest już wolny!
            this.PdfViewer.Load(this._pdfStream);

            // =========================================================================
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas ładowania pliku do pamięci: {ex.Message}");

            MessageBox.Show($"Nie udało się załadować pliku: {selectedPdfFile.FileName}\n\n{ex.Message}", "Błąd ładowania", MessageBoxButton.OK, MessageBoxImage.Warning);

            // W razie błędu wyczyść podgląd
            this._pdfStream?.Dispose();
            this.PdfViewer.Unload();
        }
    }

    // Obsługa zdarzenia DocumentLoaded kontrolki PdfViewer
    private void PdfViewer_DocumentLoaded(object sender, EventArgs args)
    {
        this.PdfViewer.ZoomMode = ZoomMode.FitPage; // Ustawienie trybu powiększenia na "Dopasuj stronę"

        // Ustawienie szerokości i wysokości na Auto (NaN) dla responsywnego rozmiaru, bo
        // po każdym załadowaniu dokumentu PdfViewer ustawia rozmiar na wielkość obrazu
        this.PdfViewer.Width = double.NaN;
        this.PdfViewer.Height = double.NaN;

        // Ustawienie trybu kursora na "Ręka"
        this.PdfViewer.CursorMode = PdfViewerCursorMode.HandTool;

        (int dpiX, int dpiY) dpi = PDFTools.GetDPI(this.PdfViewer.LoadedDocument);

        this.StatusBarItemMain.Background = Math.Abs(dpi.dpiX - 300) <= 1 && Math.Abs(dpi.dpiY - 300) <= 1
            ? new SolidColorBrush(Color.FromArgb(0, 255, 255, 255))
            : new SolidColorBrush(Colors.Red);

        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            if (this.PdfViewer.LoadedDocument.Pages.Count > 0)
            {
                // Pobierz obrót pierwszej strony
                int rotation = this.PdfViewer.LoadedDocument.Pages[0].Rotation switch
                {
                    PdfPageRotateAngle.RotateAngle0 => 0,
                    PdfPageRotateAngle.RotateAngle90 => 90,
                    PdfPageRotateAngle.RotateAngle180 => 180,
                    PdfPageRotateAngle.RotateAngle270 => 270,
                    _ => throw new NotImplementedException()
                };

                this.StatusBarItemMain.Content = $"Plik [{this.ListBoxFiles.SelectedIndex + 1}/{this.PdfFiles.Count}]: {selectedPdfFile.FileName} | Rozmiar: {selectedPdfFile.FileSize / 1024.0:F0} KB | DPI: {dpi.dpiX} x {dpi.dpiY} | Obrót: {rotation}";
            }
            else
            {
                this.StatusBarItemMain.Content = "Błąd ładowania dokumentu!";
            }
        }
        else
        {
            this.StatusBarItemMain.Content = "Błąd ładowania dokumentu!";
        }

        // Zresetuj kursor do domyślnego
        Mouse.OverrideCursor = null;
    }

    // Obsługa przycisków obrotu
    private void ButtonRotate_OnClick(object sender, RoutedEventArgs e)
    {
        bool rotateRight = e.RoutedEvent.Name == "Click"; // Przycisk "Obróć w prawo" ma zdarzenie "Click" a "Obróć w lewo" ma zdarzenie inne

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
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            PdfLoadedDocument loadedDocument = this.PdfViewer.LoadedDocument; // Pobranie załadowanego dokumentu PDF

            // Sprawdzenie, czy dokument ma co najmniej jedną stronę
            if (loadedDocument.Pages[0] is PdfLoadedPage loadedPage)
            {
                int rotationAngle = (int)PdfPageRotateAngle.RotateAngle90; // Kąt obrotu (90 stopni)

                int currentRotation = (int)loadedPage.Rotation; // Aktualny kąt obrotu strony

                int newRotation = rotateRight ? (currentRotation + rotationAngle) % 360 : (currentRotation - rotationAngle + 360) % 360; // Nowy kąt obrotu strony

                loadedPage.Rotation = (PdfPageRotateAngle)newRotation; // Ustawienie nowego kąta obrotu strony

                string filePath = selectedPdfFile.FilePath; // Ścieżka do zaznaczonego pliku PDF

                // Zapisanie zmian w pliku PDF
                loadedDocument.Save(filePath);
            }
        }
    }

    private void ButtonEditIrfanView_Click(object sender, RoutedEventArgs e)
    {
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            string filePath = selectedPdfFile.FilePath;

            // Sprawdzenie, czy plik istnieje
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Plik PDF nie istnieje: {filePath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            string irfanViewPath = Path.Combine(AppContext.BaseDirectory, "Tools", "IrfanView", "IrfanViewPortable.exe"); // Ścieżka do IrfanView

            // Sprawdzenie, czy IrfanView istnieje
            if (!File.Exists(irfanViewPath))
            {
                MessageBox.Show($"Nie znaleziono IrfanView: {irfanViewPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            try
            {
                Process process = new()
                {
                    StartInfo = new ProcessStartInfo(irfanViewPath, $"\"{filePath}\"") { UseShellExecute = false },
                    EnableRaisingEvents = true, // Umożliwia nasłuchiwanie zdarzenia Exited
                };

                process.Exited += (_, _) =>
                {
                    process.Dispose();
                };

                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się uruchomić IrfanView.\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ButtonEditGimp_Click(object sender, RoutedEventArgs e)
    {
        if (this.ListBoxFiles.SelectedItem is PdfFile selectedPdfFile)
        {
            string filePath = selectedPdfFile.FilePath;

            // Sprawdzenie, czy plik istnieje
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Plik PDF nie istnieje: {filePath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            string gimpPath = Path.Combine(AppContext.BaseDirectory, "Tools", "GIMP", "GIMPPortable.exe"); // Ścieżka do GIMP

            // Sprawdzenie, czy GIMP istnieje
            if (!File.Exists(gimpPath))
            {
                MessageBox.Show($"Nie znaleziono GIMP: {gimpPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            try
            {
                Process process = new()
                {
                    StartInfo = new ProcessStartInfo(gimpPath, $"\"{filePath}\"") { UseShellExecute = false },
                    EnableRaisingEvents = true, // Umożliwia nasłuchiwanie zdarzenia Exited
                };

                process.Exited += (_, _) =>
                {
                    process.Dispose();
                };

                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się uruchomić GIMP.\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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

                // Synchronicznie poczekaj, aż plik będzie gotowy.
                if (!Tools.WaitForFile(e.FullPath))
                {
                    this.StatusBarItemInfo.Content = $"Nie można uzyskać dostępu do: {e.Name}";
                    return;
                }

                // Sprawdzamy, czy plik już istnieje na liście (zapobiega duplikatom).
                if (this.PdfFiles.Any(p => p.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                Debug.WriteLine($"FileWatcher_Created => Znaleziono nowy plik: {e.FullPath}!");

                var fileToAdd = new PdfFile(e.FullPath);

                // Znajdź indeks, pod którym należy wstawić nowy plik, aby zachować porządek.
                int insertIndex = 0;

                while (insertIndex < this.PdfFiles.Count && this._naturalComparer.Compare(this.PdfFiles[insertIndex].FilePath, fileToAdd.FilePath) < 0)
                {
                    insertIndex++;
                }

                // Wstaw plik w odpowiednie miejsce.
                this.PdfFiles.Insert(insertIndex, fileToAdd);

                // Ustawienie zaznaczenia na nowo dodany plik i przewinięcie do niego.
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

                int newIndex = 0;
                while (newIndex < this.PdfFiles.Count && this._naturalComparer.Compare(this.PdfFiles[newIndex].FilePath, fileToRename.FilePath) < 0)
                {
                    newIndex++;
                }

                this.PdfFiles.Insert(newIndex, fileToRename);
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
        this.Dispatcher.Invoke(() =>
        {
            try
            {
                // Czekamy chwilę, aby operacja zapisu pliku mogła się w pełni zakończyć.
                Thread.Sleep(200);

                if (!File.Exists(e.FullPath))
                {
                    return;
                }

                FileInfo fileInfo = new(e.FullPath);
                long fileSize = fileInfo.Length;

                DateTime lastWriteTime = fileInfo.LastWriteTime;

                PdfFile? fileToUpdate = this.PdfFiles.FirstOrDefault(p => p.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));
                if (fileToUpdate != null && (fileToUpdate.FileSize != fileSize || fileToUpdate.LastWriteTime != lastWriteTime))
                {
                    Debug.WriteLine($"FileWatcher_Changed => Znaleziono zmodyfikowany plik: {e.FullPath}!");

                    fileToUpdate.FileSize = fileSize;
                    fileToUpdate.LastWriteTime = lastWriteTime;

                    if (this.SelectedPdfFile == fileToUpdate)
                    {
                        this.PdfViewer.Load(fileToUpdate.FilePath);
                    }

                    this.FocusAndScrollToListBoxItem(fileToUpdate);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w FileWatcher_Changed: {ex.Message}");
            }
        });
    }

    // Funkcja zapewniająca, że okno jest widoczne na ekranie
    private void EnsureWindowIsOnScreen()
    {
        // Pobierz szerokość i wysokość okna
        double width = this.Width;
        double height = this.Height;

        // Przekształć współrzędne WPF na współrzędne ekranu
        int x = (int)this.Left;
        int y = (int)this.Top;

        // Sprawdź każdy ekran
        bool isOnScreen = false;

        // Przeszukaj wszystkie ekrany, aby sprawdzić, czy okno jest widoczne na którymkolwiek z nich
        foreach (Screen screen in Screen.AllScreens)
        {
            Rectangle area = screen.WorkingArea;

            if (x + width > area.Left && x < area.Right && y + height > area.Top && y < area.Bottom)
            {
                isOnScreen = true;

                break;
            }
        }

        // Jeśli okno nie jest widoczne na żadnym ekranie – ustaw domyślnie na środek ekranu głównego
        if (!isOnScreen)
        {
            if (Screen.PrimaryScreen != null) // Sprawdź, czy ekran główny istnieje
            {
                Rectangle primary = Screen.PrimaryScreen.WorkingArea;

                // Ustaw okno na środek ekranu głównego
                this.Left = primary.Left + ((primary.Width - width) / 2);
                this.Top = primary.Top + ((primary.Height - height) / 2);
            }
        }
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