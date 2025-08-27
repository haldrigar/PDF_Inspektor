// ====================================================================================================
// <copyright file="PdfFile.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.IO;

/// <summary>
/// Klasa reprezentująca plik PDF.
/// </summary>
/// <param name="filePath">Ścieżka do pliku PDF.</param>
public class PdfFile(string filePath)
{
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
    public long FileSize { get; set; } = new FileInfo(filePath).Length;

    /// <summary>
    /// Pobiera lub ustawia datę i godzinę ostatniej modyfikacji pliku.
    /// </summary>
    public DateTime LastWriteTime { get; set; } = new FileInfo(filePath).LastWriteTime;
}
