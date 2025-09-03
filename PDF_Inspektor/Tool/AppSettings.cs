// ====================================================================================================
// <copyright file="AppSettings.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor.Tool;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Definiuje pojedyncze narzędzie zewnętrzne używane przez aplikację.
/// </summary>
public class ExternalTool
{
    /// <summary>
    /// Pobiera nazwę wyświetlaną narzędzia.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Pobiera ścieżkę do pliku wykonywalnego.
    /// </summary>
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>
    /// Pobiera nazwę archiwum ZIP do rozpakowania.
    /// </summary>
    public string ZipFileName { get; init; } = string.Empty;
}

/// <summary>
/// Zawiera informacje o ustawieniach aplikacji oraz zarządza ich zapisem i odczytem.
/// </summary>
internal class AppSettings
{
    // Ścieżka do pliku konfiguracyjnego w katalogu bazowym aplikacji.
    [JsonIgnore]
    private static readonly string ConfigFilePath = Path.Combine(AppContext.BaseDirectory, "PDF_Inspektor.appsettings.json");

    // Opcje serializacji JSON ze wcięciami dla lepszej czytelności.
    [JsonIgnore]
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Pobiera lub ustawia ścieżkę sieciową do aktualizacji aplikacji.
    /// </summary>
    public string UpdatePath { get; set; } = @"\\192.168.0.40\Aplikacje\PDF_Inspektor\";

    /// <summary>
    /// Pobiera lub ustawia ostatnio używany katalog do otwierania plików PDF.
    /// </summary>
    public string LastUsedDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Pobiera lub ustawia ostatnio używany plik PDF.
    /// </summary>
    public string LastUsedFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Pobiera klucz licencyjny Syncfusion.
    /// </summary>
    public string SyncfusionLicenseKey { get; init; } = "SyncfusionLicenseKey";

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
    /// Pobiera listę skonfigurowanych narzędzi zewnętrznych.
    /// </summary>
    public List<ExternalTool> Tools { get; init; } =
    [
        new() { Name = "IrfanView", ExecutablePath = @"Tools\IrfanView\IrfanViewPortable.exe", ZipFileName = "irfanview.zip" },
        new() { Name = "GIMP", ExecutablePath = @"Tools\GIMP\GIMPPortable.exe", ZipFileName = "gimp.zip" },
    ];

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