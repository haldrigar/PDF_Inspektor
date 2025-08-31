// ====================================================================================================
// <copyright file="AppSettings.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.IO;
using System.Text.Json;

/// <summary>
/// Zawiera informacje o ustawieniach aplikacji oraz zarządza ich zapisem i odczytem.
/// </summary>
internal class AppSettings
{
    // Ścieżka do pliku konfiguracyjnego w katalogu bazowym aplikacji.
    private static readonly string ConfigFilePath = Path.Combine(AppContext.BaseDirectory, "PDF_Inspektor.appsettings.json");

    /// Opcje serializacji JSON ze wcięciami dla lepszej czytelności.
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Pobiera lub ustawia klucz licencyjny Syncfusion.
    /// </summary>
    public string SyncfusionLicenseKey { get; set; } = "SyncfusionLicenseKey";

    /// <summary>
    /// Pobiera lub ustawia Top okna aplikacji.
    /// </summary>
    public double WindowTop { get; set; } = 100;

    /// <summary>
    /// Pobiera lub ustawia Left okna aplikacji.
    /// </summary>
    public double WindowLeft { get; set; } = 100;

    /// <summary>
    /// Pobiera lub ustawia Height okna aplikacji.
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// Pobiera lub ustawia Width okna aplikacji.
    /// </summary>
    public double WindowHeight { get; set; } = 600;

    /// <summary>
    /// Wczytuje ustawienia z pliku JSON. Jeśli plik nie istnieje, tworzy go z domyślnymi wartościami.
    /// </summary>
    /// <returns>Obiekt klasy <see cref="AppSettings"/> z wczytanymi lub domyślnymi ustawieniami.</returns>
    public static AppSettings Load()
    {
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                string json = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();
            }
            catch (Exception ex)
            {
                // W przypadku błędu odczytu lub deserializacji utwórz domyślne ustawienia
                System.Diagnostics.Debug.WriteLine($"Błąd podczas wczytywania ustawień: {ex.Message}");
                return CreateDefault();
            }
        }
        else
        {
            return CreateDefault();
        }
    }

    /// <summary>
    /// Zapisuje bieżące ustawienia do pliku JSON.
    /// </summary>
    public void Save()
    {
        string json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(ConfigFilePath, json);
    }

    private static AppSettings CreateDefault()
    {
        // Tworzy domyślną instancję ustawień i zapisuje ją do pliku
        AppSettings settings = new();

        // Zapisz domyślne ustawienia do pliku
        settings.Save();

        return settings;
    }
}