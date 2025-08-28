// ====================================================================================================
// <copyright file="PdfFile.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// Klasa reprezentująca plik PDF.
/// </summary>
/// <param name="filePath">Ścieżka do pliku PDF.</param>
public sealed class PdfFile(string filePath) : INotifyPropertyChanged
{
    private string _filePath = filePath;
    private string _fileName = Path.GetFileName(filePath);
    private long _fileSize = new FileInfo(filePath).Length;
    private DateTime _lastWriteTime = new FileInfo(filePath).LastWriteTime;
    private string _directoryName = Path.GetDirectoryName(filePath) ?? string.Empty;

    /// <summary>
    /// Zdarzenie wywoływane, gdy właściwość ulegnie zmianie.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Pobiera lub ustawia ścieżkę do pliku PDF.
    /// </summary>
    public string FilePath
    {
        get => this._filePath;
        set => this.SetField(ref this._filePath, value);
    }

    /// <summary>
    /// Pobiera lub ustawia nazwę pliku PDF (bez ścieżki).
    /// </summary>
    public string FileName
    {
        get => this._fileName;
        set => this.SetField(ref this._fileName, value);
    }

    /// <summary>
    /// Pobiera lub ustawia rozmiar pliku PDF w kilobajtach (KB).
    /// </summary>
    public long FileSize
    {
        get => this._fileSize;
        set => this.SetField(ref this._fileSize, value);
    }

    /// <summary>
    /// Pobiera lub ustawia datę i godzinę ostatniej modyfikacji pliku.
    /// </summary>
    public DateTime LastWriteTime
    {
        get => this._lastWriteTime;
        set => this.SetField(ref this._lastWriteTime, value);
    }

    /// <summary>
    /// Pobiera lub ustawia nazwę katalogu, w którym znajduje się plik PDF.
    /// </summary>
    public string DirectoryName
    {
        get => this._directoryName;
        set => this.SetField(ref this._directoryName, value);
    }

    /// <summary>
    /// Metoda wywołująca zdarzenie PropertyChanged.
    /// </summary>
    /// <param name="propertyName">Nazwa zmienionej właściwości.</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Metoda pomocnicza do ustawiania wartości pola i wywoływania zdarzenia PropertyChanged.
    /// </summary>
    /// <typeparam name="T">Typ pola.</typeparam>
    /// <param name="field">Referencja do pola.</param>
    /// <param name="value">Nowa wartość.</param>
    /// <param name="propertyName">Nazwa właściwości.</param>
    /// <returns>True, jeśli wartość została zmieniona.</returns>
    // ReSharper disable once UnusedMethodReturnValue.Local
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;

        this.OnPropertyChanged(propertyName);

        return true;
    }
}