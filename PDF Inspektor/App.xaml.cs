// ====================================================================================================
// <copyright file="App.xaml.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.Windows;

using MessageBox = System.Windows.MessageBox;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
public partial class App
{
    /// <summary>
    /// Inicjalizuje nową instancję klasy <see cref="App"/> i rejestruje licencję Syncfusion.
    /// </summary>
    public App()
    {
        Tools.RegisterSyncfusionLicense(); // Rejestracja licencji Syncfusion

        this.DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show("Nieobsłużony błąd:\n" + e.Exception.Message, "Błąd krytyczny", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;
        };
    }
}