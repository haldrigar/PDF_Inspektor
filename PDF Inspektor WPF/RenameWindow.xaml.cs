// ====================================================================================================
// <copyright file="RenameWindow.xaml.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.Windows;
using System.Windows.Input;

/// <summary>
/// Klasa okna do zmiany nazwy pliku PDF.
/// </summary>
public partial class RenameWindow
{
    /// <summary>
    /// Inicjalizuje nowe okno do zmiany nazwy pliku PDF.
    /// </summary>
    /// <param name="currentFileName">Zaznaczony plik.</param>
    public RenameWindow(string currentFileName)
    {
        this.InitializeComponent();

        // Ustaw początkową nazwę w polu tekstowym i zaznacz ją
        this.FileNameTextBox.Text = currentFileName;
        this.FileNameTextBox.Focus();
        this.FileNameTextBox.SelectAll();
    }

    /// <summary>
    /// Pobiera nową nazwę pliku wprowadzaną przez użytkownika.
    /// </summary>
    public string NewFileName { get; private set; } = string.Empty;

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        this.NewFileName = this.FileNameTextBox.Text;
        this.DialogResult = true; // Zamknij okno z wynikiem "true"
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false; // Zamknij okno z wynikiem "false"
    }

    // Obsługa klawisza Enter w polu tekstowym
    private void FileNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            this.OkButton_Click(sender, e);
        }
    }
}