// ====================================================================================================
// <copyright file="PdfFile.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor_OLD;

/// <summary>
/// KLasa reprezentująca plik PDF.
/// </summary>
/// <param name="fileIndex">Indeks pliku PDF w liście.</param>
/// <param name="filePath">Ścieżka do pliku PDF.</param>
internal class PdfFile(int fileIndex, string filePath)
{
    /// <summary>
    /// Pobiera lub ustawia indeks pliku PDF w liście.
    /// </summary>
    public int FileIndex { get; set; } = fileIndex;

    /// <summary>
    /// Pobiera lub ustawia ścieżkę do pliku PDF.
    /// </summary>
    public string FilePath { get; set; } = filePath;

    /// <summary>
    /// Pobiera lub ustawia nazwę pliku PDF (bez ścieżki).
    /// </summary>
    public string FileName { get; set; } = Path.GetFileName(filePath);

    /// <summary>
    /// Pobiera lub ustawia rozmiar pliku PDF w kilobajtach (KB).
    /// </summary>
    public double FileSize { get; set; } = new FileInfo(filePath).Length / 1024.0; // Rozmiar pliku w KB
}
