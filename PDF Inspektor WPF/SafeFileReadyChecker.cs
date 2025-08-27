// ====================================================================================================
// <copyright file="SafeFileReadyChecker.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.Diagnostics;
using System.IO;

/// <summary>
/// Klasa pomocnicza do bezpiecznego sprawdzania, czy plik jest gotowy do odczytu,
/// bez zakłócania procesu zapisu.
/// </summary>
public static class SafeFileReadyChecker
{
    /// <summary>
    /// Sprawdza, czy plik jest w pełni skopiowany i gotowy do użycia.
    /// </summary>
    /// <param name="filePath">Ścieżka do pliku.</param>
    /// <param name="timeout">Maksymalny czas oczekiwania na gotowość pliku.</param>
    /// <param name="stabilityCheckTimeMs">Czas (w milisekundach), przez jaki rozmiar pliku musi pozostać niezmienny.</param>
    /// <returns>True, jeśli plik jest gotowy, w przeciwnym razie False.</returns>
    public static bool IsFileReady(string filePath, TimeSpan timeout, int stabilityCheckTimeMs = 1000)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (!File.Exists(filePath))
            {
                Thread.Sleep(100);

                continue;
            }

            try
            {
                // Sprawdzenie stabilności rozmiaru pliku
                long initialSize = new FileInfo(filePath).Length;

                Thread.Sleep(stabilityCheckTimeMs);

                long finalSize = new FileInfo(filePath).Length;

                if (initialSize > 0 && initialSize == finalSize)
                {
                    // Spróbuj otworzyć plik do odczytu (bez blokowania)
                    using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    Debug.WriteLine($"Plik '{filePath}' jest gotowy do użycia.");

                    return true;
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Plik '{filePath}' jest tymczasowo niedostępny lub zablokowany. Czekam... Błąd: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Wystąpił nieoczekiwany błąd podczas sprawdzania pliku '{filePath}': {ex.Message}");

                return false;
            }
        }

        Debug.WriteLine($"Plik '{filePath}' nie stał się gotowy w ciągu {timeout.TotalSeconds} sekund.");

        return false;
    }
}