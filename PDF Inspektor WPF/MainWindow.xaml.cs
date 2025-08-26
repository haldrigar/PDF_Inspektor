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
using System.Threading.Tasks;
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
public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _jsonPath = Path.Combine(AppContext.BaseDirectory, "PDF_Inspektor.appsettings.json");
    private readonly AppSettings _appSettings;
    private FileSystemWatcher _fileWatcher = new();

    // Używamy ObservableCollection do automatycznej aktualizacji UI
    public ObservableCollection<PdfFile> PdfFiles { get; } = new();

    // Właściwość dla bindowania zaznaczonego elementu
    public PdfFile? SelectedPdfFile { get; set; }

    // Kolekcja do śledzenia ostatnio dodanych plików w celu deduplikacji
    private readonly HashSet<string> _recentlyAddedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public MainWindow()
    {
        this.InitializeComponent();
        this.DataContext = this; // Ustawienie kontekstu danych dla bindowania

        if (!File.Exists(this._jsonPath))
        {
            this.SaveConfig();
        }

        string json = File.ReadAllText(this._jsonPath);
        this._appSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    protected override void OnClosed(EventArgs e)
    {
        this._fileWatcher?.Dispose();
        base.OnClosed(e);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        this.Top = this._appSettings.WindowTop;
        this.Left = this._appSettings.WindowLeft;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        this._appSettings.WindowTop = this.Top;
        this._appSettings.WindowLeft = this.Left;
        this.SaveConfig();
    }

    private void SaveConfig()
    {
        string json = JsonSerializer.Serialize(this._appSettings, JsonOptions);
        File.WriteAllText(this._jsonPath, json);
    }

    private void ListBoxFiles_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void ListBoxFiles_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] droppedItems)
        {
            this.LoadFiles(droppedItems);
        }
    }

    private void ButtonMonitorDirectory_Click(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new() { Title = "Wybierz folder do przetwarzania" };

        if (dialog.ShowDialog() == true)
        {
            this.LoadFiles([dialog.FolderName]);
            this.SetupFileSystemWatcher(dialog.FolderName);
        }
    }

    /// <summary>
    /// Centralna metoda do ładowania plików z podanych ścieżek (pliki lub foldery).
    /// </summary>
    private void LoadFiles(IEnumerable<string> paths)
    {
        this.PdfFiles.Clear();

        var filesToLoad = new List<string>();

        foreach (string path in paths)
        {
            if (File.Exists(path) && path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                filesToLoad.Add(path);
            }
            else if (Directory.Exists(path))
            {
                filesToLoad.AddRange(Directory.GetFiles(path, "*.pdf", SearchOption.AllDirectories));
            }
        }

        filesToLoad.Sort(new NaturalStringComparer());

        foreach (string file in filesToLoad)
        {
            this.PdfFiles.Add(new PdfFile(this.PdfFiles.Count, file));
        }

        // Zamiast ustawiać indeks, ustawiamy powiązaną właściwość
        if (this.PdfFiles.Any())
        {
            this.SelectedPdfFile = this.PdfFiles.First();

            // Ręczna aktualizacja, aby SelectionChanged na pewno się wywołało
            this.ListBoxFiles.SelectedItem = this.SelectedPdfFile;
        }
        else
        {
            this.SelectedPdfFile = null;
        }
    }

    private void ListBoxFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Sprawdzamy, czy zaznaczony element jest typu PdfFile
        if (ListBoxFiles.SelectedItem is PdfFile selected)
        {
            // Aktualizujemy właściwość SelectedPdfFile, co jest dobrą praktyką
            this.SelectedPdfFile = selected;
            this.PdfViewer.Load(selected.FilePath);
        }
    }

    private void PdfViewer_DocumentLoaded(object sender, EventArgs args)
    {
        this.PdfViewer.Width = double.NaN;
        this.PdfViewer.Height = double.NaN;
        this.PdfViewer.CursorMode = PdfViewerCursorMode.HandTool;

        if (this.SelectedPdfFile is not PdfFile currentPdfFile) return;

        string dpi = PDFTools.GetDPI(this.PdfViewer.LoadedDocument);
        currentPdfFile.DPI = dpi;

        this.StatusBarItemMain.Background = dpi != "300 x 300" ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));

        this.StatusBarItemMain.Content = $"Plik: {currentPdfFile.FileName} | Rozmiar: {currentPdfFile.FileSize:F0} KB | DPI: {dpi} | Obrót: {this.PdfViewer.LoadedDocument.Pages[0].Rotation}";
    }

    private void ButtonRotate_OnClick(object sender, RoutedEventArgs e)
    {
        bool rotateRight = e.RoutedEvent.Name == "Click";
        this.RotateAndSave(rotateRight);
    }

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
    }

    private void RotateAndSave(bool rotateRight)
    {
        if (this.SelectedPdfFile is not PdfFile currentFile) return;

        string filePath = currentFile.FilePath;
        PdfLoadedDocument loadedDocument = this.PdfViewer.LoadedDocument;

        if (loadedDocument.Pages[0] is not PdfLoadedPage loadedPage) return;

        int rotationAngle = (int)PdfPageRotateAngle.RotateAngle90;
        int currentRotation = (int)loadedPage.Rotation;
        int newRotation = rotateRight ? (currentRotation + rotationAngle) % 360 : (currentRotation - rotationAngle + 360) % 360;

        loadedPage.Rotation = (PdfPageRotateAngle)newRotation;
        loadedDocument.Save(filePath);
        this.PdfViewer.Load(filePath);
    }

    private void ButtonEdit_Click(object sender, RoutedEventArgs e)
    {
        if (this.SelectedPdfFile is not PdfFile currentFile) return;

        string filePath = currentFile.FilePath;
        DateTime lastWriteTime = File.GetLastWriteTime(filePath);
        string irfanViewPath = Path.Combine(AppContext.BaseDirectory, "IrfanView", "IrfanViewPortable.exe");

        Process process = new()
        {
            StartInfo = new ProcessStartInfo(irfanViewPath, $"\"{filePath}\"") { UseShellExecute = false },
            EnableRaisingEvents = true,
        };

        process.Exited += (_, _) =>
        {
            this.Dispatcher.Invoke(() =>
            {
                if (File.GetLastWriteTime(filePath) != lastWriteTime)
                {
                    this.PdfViewer.Load(filePath);
                }
            });
            process.Dispose();
        };

        process.Start();
    }

    private void SetupFileSystemWatcher(string path)
    {
        this._fileWatcher?.Dispose();
        this._fileWatcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName,
            Filter = "*.pdf",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
        };
        this._fileWatcher.Created += this.FileWatcher_Created;
    }

    private async void FileWatcher_Created(object sender, FileSystemEventArgs e)
    {
        // Użyj lock, aby zapewnić bezpieczeństwo wątkowe dla HashSet
        lock (this._recentlyAddedFiles)
        {
            // Jeśli plik jest już przetwarzany, zignoruj to zdarzenie
            if (_recentlyAddedFiles.Contains(e.FullPath))
            {
                Debug.WriteLine($"Zignorowano zduplikowane zdarzenie dla pliku: {e.Name}");
                return;
            }

            // Dodaj plik do zestawu, aby oznaczyć go jako "w trakcie przetwarzania"
            _recentlyAddedFiles.Add(e.FullPath);
        }

        try
        {
            // Oczekiwanie na gotowość pliku
            bool isReady = await Task.Run(() => SafeFileReadyChecker.IsFileReady(e.FullPath, TimeSpan.FromSeconds(30)));

            if (isReady)
            {
                Debug.WriteLine($"Plik {e.Name} jest gotowy. Dodaję do listy.");
                await this.Dispatcher.InvokeAsync(() =>
                {
                    if (!this.PdfFiles.Any(p => p.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        var newPdf = new PdfFile(this.PdfFiles.Count, e.FullPath);
                        this.PdfFiles.Add(newPdf);
                        this.ListBoxFiles.SelectedItem = newPdf;
                        this.ListBoxFiles.ScrollIntoView(newPdf);
                    }
                });
            }
            else
            {
                Debug.WriteLine($"Timeout: Plik {e.Name} nie ustabilizował się na czas.");
            }
        }
        finally
        {
            // Po krótkim opóźnieniu usuń plik z listy, aby mógł być ponownie przetworzony w przyszłości.
            // Opóźnienie daje pewność, że wszystkie zduplikowane zdarzenia zostaną przechwycone.
            await Task.Delay(2000);
            lock (this._recentlyAddedFiles)
            {
                this._recentlyAddedFiles.Remove(e.FullPath);
            }
        }
    }

}