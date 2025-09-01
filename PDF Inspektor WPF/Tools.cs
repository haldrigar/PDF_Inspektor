// ====================================================================================================
// <copyright file="Tools.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

/// <summary>
/// Klasa narzędziowa zawierająca metody pomocnicze.
/// </summary>
internal static class Tools
{
    /// <summary>
    /// Sprawdza, czy okno jest widoczne na którymkolwiek z ekranów i w razie potrzeby przesuwa je na środek ekranu głównego.
    /// Używa natywnego Win32 API zamiast Windows Forms.
    /// </summary>
    /// <param name="window">Okno do sprawdzenia.</param>
    public static void EnsureWindowIsOnScreen(Window window)
    {
        // Pobierz obszar roboczy ekranu, na którym znajduje się okno.
        Rect currentScreenArea = ScreenInterop.GetScreenWorkArea(window);

        // Sprawdź, czy okno jest w pełni widoczne w obszarze roboczym.
        // Proste sprawdzenie: czy lewy górny róg okna jest w granicach ekranu.
        bool isVisible = currentScreenArea.Contains(new Point(window.Left, window.Top));

        // Jeśli okno jest poza ekranem, przenieś je na środek ekranu głównego.
        if (!isVisible)
        {
            Rect primaryScreenArea = ScreenInterop.GetPrimaryScreenWorkArea();

            window.Left = primaryScreenArea.Left + ((primaryScreenArea.Width - window.Width) / 2);
            window.Top = primaryScreenArea.Top + ((primaryScreenArea.Height - window.Height) / 2);
        }
    }

    /// <summary>
    /// Sprawdza istnienie narzędzia i rozpakowuje je z archiwum ZIP, jeśli jest to konieczne.
    /// </summary>
    /// <param name="tool">Obiekt konfiguracyjny narzędzia zewnętrznego.</param>
    public static void EnsureAndUnpackTool(ExternalTool tool)
    {
        string baseDirectory = AppContext.BaseDirectory; // Katalog bazowy aplikacji
        string executablePath = Path.Combine(baseDirectory, tool.ExecutablePath); // Pełna ścieżka do pliku wykonywalnego
        string zipPath = Path.Combine(baseDirectory, "Tools", tool.ZipFileName); // Pełna ścieżka do archiwum ZIP

        // Sprawdź, czy narzędzie już istnieje
        if (File.Exists(executablePath))
        {
            return;
        }

        // Sprawdź, czy archiwum ZIP istnieje, jeśli trzeba je rozpakować
        if (!File.Exists(zipPath))
        {
            MessageBox.Show($"Nie znaleziono ani aplikacji {tool.Name}, ani archiwum {tool.ZipFileName}.", $"Brak {tool.Name}", MessageBoxButton.OK, MessageBoxImage.Error);

            return;
        }

        try
        {
            MessageBox.Show($"Brak aplikacji {tool.Name}. Nastąpi rozpakowanie archiwum. Poczekaj na komunikat o zakończeniu.", $"Instalacja {tool.Name}", MessageBoxButton.OK, MessageBoxImage.Information);

            // Rozpakuj do katalogu 'Tools', który jest katalogiem nadrzędnym dla archiwum
            string extractPath = Path.GetDirectoryName(zipPath) ?? baseDirectory;

            // Rozpakuj archiwum ZIP, nadpisując istniejące pliki
            ZipFile.ExtractToDirectory(zipPath, extractPath, true);

            MessageBox.Show($"Rozpakowywanie {tool.Name} zakończone.", tool.Name, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Wystąpił błąd podczas rozpakowywania {tool.Name}.\n{ex.Message}", "Błąd instalacji", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Uruchamia zewnętrzny proces dla podanego pliku i wykonuje akcję po jego zakończeniu.
    /// </summary>
    /// <param name="executablePath">Pełna ścieżka do pliku wykonywalnego narzędzia.</param>
    /// <param name="fileToOpen">Pełna ścieżka do pliku, który ma zostać otwarty w narzędziu.</param>
    /// <param name="onProcessExited">Akcja do wykonania po zamknięciu procesu. Może być null.</param>
    public static void StartExternalProcess(string executablePath, string fileToOpen, Action? onProcessExited = null)
    {
        if (!File.Exists(executablePath))
        {
            MessageBox.Show($"Nie znaleziono aplikacji: {executablePath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!File.Exists(fileToOpen))
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

            process.Exited += (_, _) =>
            {
                onProcessExited?.Invoke();
                process.Dispose();
            };

            process.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie udało się uruchomić procesu dla pliku: {fileToOpen}.\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}