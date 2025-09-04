// ====================================================================================================
// <copyright file="Program.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// 
// Ostatni zapis pliku: 2025-09-04 16:46:58
// ====================================================================================================

namespace PDF_Inspektor_Updater;

using System.Diagnostics;

/// <summary>
/// Program aktualizujący aplikację PDF Inspektor.
/// </summary>
public static class Program
{
    private static int Main(string[] args)
    {
        //if (!Debugger.IsAttached)
        //{
        //    Debugger.Launch();
        //}

        Console.WriteLine("PDF Inspektor Updater");
        Console.WriteLine("=====================\n");

        if (args.Length != 3)
        {
            Console.WriteLine("Program można wywołać tylko z aplikacji głównej!");

            return 1; // Zwróć kod błędu
        }

        string updatePath = args[0];
        string localPath = args[1];
        string mainExeFile = args[2];

        Console.WriteLine($"Ścieżka źródłowa aktualizacji: {updatePath}\n");
        Console.WriteLine($"Lokalna ścieżka aplikacji: {localPath}\n");
        Console.WriteLine("Rozpoczynanie aktualizacji...\n");

        try
        {
            string mainExeFileName = Path.GetFileNameWithoutExtension(mainExeFile); // Nazwa pliku głównej aplikacji bez rozszerzenia

            // Czekaj i zamknij wszystkie istniejące procesy głównej aplikacji
            foreach (Process proc in Process.GetProcessesByName(mainExeFileName))
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit();
                }
                catch
                {
                    // Ignoruj błędy, jeśli proces już się zakończył
                }
            }

            // Daj systemowi chwilę na zwolnienie plików
            Thread.Sleep(1000);

            // Pobierz wszystkie pliki ze źródła aktualizacji, włączając podfoldery
            foreach (string sourceFilePath in Directory.GetFiles(updatePath, "*.*", SearchOption.AllDirectories))
            {
                // Utwórz ścieżkę względną, aby zachować strukturę folderów
                string relativePath = Path.GetRelativePath(updatePath, sourceFilePath);

                // Zbuduj pełną ścieżkę docelową w folderze lokalnym
                string destinationFilePath = Path.Combine(localPath, relativePath);

                // Sprawdź, czy plik docelowy nie istnieje lub plik źródłowy jest nowszy
                if (!File.Exists(destinationFilePath) || File.GetLastWriteTime(sourceFilePath) > File.GetLastWriteTime(destinationFilePath))
                {
                    // Upewnij się, że folder docelowy istnieje
                    string? destinationDirectory = Path.GetDirectoryName(destinationFilePath);

                    if (destinationDirectory != null && !Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    // Skopiuj plik, nadpisując istniejący
                    File.Copy(sourceFilePath, destinationFilePath, true);

                    Console.WriteLine($"Zaktualizowano: {relativePath}");
                }
            }

            Console.WriteLine("\nAktualizacja zakończona pomyślnie.\n");

            // Uruchom ponownie główną aplikację
            Process.Start(mainExeFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Błąd podczas aktualizacji: " + ex.Message);
            Console.WriteLine("Naciśnij dowolny klawisz, aby zakończyć...");

            Console.ReadKey(false);

            return 2; // Zwróć kod błędu
        }

        return 0; // Zwróć kod sukcesu
    }
}