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
    /// </summary>
    /// <param name="window">Okno do sprawdzenia.</param>
    public static void EnsureWindowIsOnScreen(Window window)
    {
        double width = window.Width;
        double height = window.Height;
        double x = window.Left;
        double y = window.Top;

        bool isOnScreen = false;

        // Przeszukaj wszystkie ekrany, aby sprawdzić, czy okno jest widoczne na którymkolwiek z nich
        foreach (Screen screen in Screen.AllScreens)
        {
            Rectangle area = screen.WorkingArea;

            // Sprawdź, czy jakakolwiek część okna jest widoczna na ekranie
            if (x < area.Right && x + width > area.Left && y < area.Bottom && y + height > area.Top)
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
                window.Left = primary.Left + ((primary.Width - width) / 2);
                window.Top = primary.Top + ((primary.Height - height) / 2);
            }
        }
    }

    /// <summary>
    /// Uruchamia zewnętrzny proces dla podanego pliku.
    /// </summary>
    /// <param name="executablePath">Pełna ścieżka do pliku wykonywalnego narzędzia.</param>
    /// <param name="fileToOpen">Pełna ścieżka do pliku, który ma zostać otwarty w narzędziu.</param>
    public static void StartExternalProcess(string executablePath, string fileToOpen)
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
            // Użycie cudzysłowów wokół ścieżki pliku zapewnia obsługę spacji w nazwach.
            Process process = new()
            {
                StartInfo = new ProcessStartInfo(executablePath, $"\"{fileToOpen}\"") { UseShellExecute = false },
                EnableRaisingEvents = true,
            };

            // Po prostu zwalniamy zasoby po zakończeniu procesu.
            process.Exited += (_, _) =>
            {
                process.Dispose();
            };

            process.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie udało się uruchomić procesu dla pliku: {fileToOpen}.\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Sprawdza istnienie narzędzia i rozpakowuje je z archiwum ZIP, jeśli jest to konieczne.
    /// </summary>
    /// <param name="tool">Obiekt konfiguracyjny narzędzia zewnętrznego.</param>
    public static void EnsureAndUnpackTool(ExternalTool tool)
    {
        string baseDirectory = AppContext.BaseDirectory;
        string executablePath = Path.Combine(baseDirectory, tool.ExecutablePath);
        string zipPath = Path.Combine(baseDirectory, "Tools", tool.ZipFileName);

        if (File.Exists(executablePath))
        {
            return;
        }

        if (!File.Exists(zipPath)) // Sprawdź, czy archiwum ZIP istnieje
        {
            MessageBox.Show($"Nie znaleziono ani aplikacji {tool.Name}, ani archiwum {tool.ZipFileName}.", $"Brak {tool.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            MessageBox.Show($"Brak aplikacji {tool.Name}. Nastąpi rozpakowanie archiwum. Poczekaj na komunikat o zakończeniu.", $"Instalacja {tool.Name}", MessageBoxButton.OK, MessageBoxImage.Information);

            // Rozpakuj do katalogu 'Tools', który jest katalogiem nadrzędnym dla archiwum
            string extractPath = Path.GetDirectoryName(zipPath) ?? baseDirectory;
            ZipFile.ExtractToDirectory(zipPath, extractPath, true);

            File.Delete(zipPath); // Opcjonalnie usuń archiwum po rozpakowaniu

            MessageBox.Show($"Rozpakowywanie {tool.Name} zakończone.", tool.Name, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Wystąpił błąd podczas rozpakowywania {tool.Name}.\n{ex.Message}", "Błąd instalacji", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Synchronicznie czeka, aż plik przestanie być blokowany przez inny proces.
    /// </summary>
    /// <param name="fullPath">Pełna ścieżka do pliku.</param>
    /// <param name="timeoutMs">Maksymalny czas oczekiwania w milisekundach.</param>
    /// <returns>True, jeśli plik stał się dostępny; w przeciwnym razie false.</returns>
    public static bool WaitForFile(string fullPath, int timeoutMs = 3000)
    {
        Stopwatch sw = Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                // Spróbuj otworzyć plik z wyłącznym dostępem. Jeśli się uda, oznacza to, że żaden inny proces go nie blokuje.
                using FileStream stream = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.None);

                return true; // Sukces, plik jest dostępny.
            }
            catch (FileNotFoundException)
            {
                // Plik został usunięty w międzyczasie.
                Debug.WriteLine("Plik został usunięty podczas oczekiwania.");

                return false;
            }
            catch (IOException)
            {
                // Plik jest w użyciu, poczekaj i spróbuj ponownie.
                Debug.WriteLine("Plik jest nadal używany, oczekiwanie...");
                Thread.Sleep(100); // Poczekaj 100 ms przed ponowną próbą (blokuje bieżący wątek).
            }
        }

        Debug.WriteLine("Przekroczono limit czasu oczekiwania na plik.");
        return false; // Przekroczono limit czasu.
    }
}