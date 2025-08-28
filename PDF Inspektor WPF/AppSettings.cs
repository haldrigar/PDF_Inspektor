// ====================================================================================================
// <copyright file="AppSettings.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

/// <summary>
/// Zawiera informacje o ustawieniach aplikacji.
/// </summary>
internal class AppSettings
{
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
}