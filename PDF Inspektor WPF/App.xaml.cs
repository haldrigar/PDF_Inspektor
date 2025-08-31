// ====================================================================================================
// <copyright file="App.xaml.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

using System.Windows;

using MessageBox = System.Windows.MessageBox;

namespace PDF_Inspektor;

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
        this.RegisterSyncfusionLicense();

        this.DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show("Nieobsłużony błąd:\n" + e.Exception.Message, "Błąd krytyczny", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;
        };
    }

    private void RegisterSyncfusionLicense()
    {
        var settings = AppSettings.Load();

        if (string.IsNullOrEmpty(settings.SyncfusionLicenseKey) || settings.SyncfusionLicenseKey == "SyncfusionLicenseKey")
        {
            MessageBox.Show("Klucz licencyjny Syncfusion nie został skonfigurowany w pliku appsettings.json.", "Brak klucza licencyjnego", MessageBoxButton.OK, MessageBoxImage.Error);

            return;
        }

        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(settings.SyncfusionLicenseKey);
    }
}