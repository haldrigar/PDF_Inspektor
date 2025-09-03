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
    /// Funkcja rejestrująca klucz licencyjny Syncfusion.
    /// </summary>
    public static void RegisterSyncfusionLicense()
    {
        AppSettings settings = AppSettings.Load();

        if (string.IsNullOrEmpty(settings.SyncfusionLicenseKey) || settings.SyncfusionLicenseKey == "SyncfusionLicenseKey")
        {
            MessageBox.Show("Klucz licencyjny Syncfusion nie został skonfigurowany. Aplikacja zostanie zamknięta.", "Brak klucza licencyjnego", MessageBoxButton.OK, MessageBoxImage.Error);

            // Zakończ aplikację, jeśli klucz jest nieprawidłowy
            Application.Current.Shutdown();

            return;
        }

        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(settings.SyncfusionLicenseKey);
    }

    /// <summary>
    /// Funkcja sprawdzająca, czy dostępna jest nowsza wersja aplikacji w określonej lokalizacji sieciowej.
    /// </summary>
    /// <returns>Zwraca true jeśli aktualizacja jest dostepna.</returns>
    public static bool IsUpdateAvailable()
    {
        try
        {
            AppSettings settings = AppSettings.Load(); // Wczytanie ustawień aplikacji

            string networkPath = settings.UpdateNetworkPath; // Ścieżka sieciowa do sprawdzenia aktualizacji

            string localPath = AppDomain.CurrentDomain.BaseDirectory; // Lokalny katalog aplikacji

            foreach (string netFile in Directory.GetFiles(networkPath))
            {
                string fileName = Path.GetFileName(netFile);

                string localFile = Path.Combine(localPath, fileName);

                if (!File.Exists(localFile) || File.GetLastWriteTimeUtc(netFile) > File.GetLastWriteTimeUtc(localFile))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Błąd sprawdzania aktualizacji: " + ex.Message);
        }

        return false;
    }

    /// <summary>
    /// Funkcja uruchamiająca zewnętrzny program aktualizujący i zamykająca bieżącą aplikację.
    /// </summary>
    public static void RunUpdaterAndExit()
    {
        AppSettings settings = AppSettings.Load();

        string networkPath = settings.UpdateNetworkPath; // Ścieżka sieciowa do sprawdzenia aktualizacji

        string localPath = AppDomain.CurrentDomain.BaseDirectory; // Lokalny katalog aplikacji

        string updaterExePath = Path.Combine(localPath, "PDF_Inspektor_Updater.exe"); // Ścieżka do programu aktualizującego

        try // Kopiowanie pliku aktualizatora na wypadek, gdyby był stary lub go nie było
        {
            File.Copy(Path.Combine(networkPath, "PDF_Inspektor_Updater.exe"), updaterExePath, true);
        }
        catch (Exception e)
        {
            MessageBox.Show(
                $"Nie można skopiować aktualizatora programu! Aktualizacja niemożliwa.\n{e.Message}",
                "Aktualziacja programu",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        ProcessModule? processModule = Process.GetCurrentProcess().MainModule; // Pobranie modułu głównego bieżącego procesu

        if (processModule != null)
        {
            string exePath = processModule.FileName; // Pełna ścieżka do pliku wykonywalnego bieżącej aplikacji, aby przekazać ją do aktualizatora i ponownie ją uruchomić po aktualizacji

            ProcessStartInfo startInfo = new(updaterExePath) { ArgumentList = { networkPath, localPath, exePath } };

            Process.Start(startInfo);
        }

        Application.Current.Shutdown(); // Zamknięcie bieżącej aplikacji
    }
}