// ====================================================================================================
// <copyright file="NaturalStringComparer.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor.Tool;

using System.Runtime.InteropServices;

/// <summary>
/// Umożliwia naturalne sortowanie ciągów znaków (np. "file10.txt" po "file2.txt").
/// </summary>
public class NaturalStringComparer : IComparer<string>
{
    /// <summary>
    /// Funkcja porównująca dwa ciągi znaków za pomocą metody logicznej.
    /// </summary>
    /// <param name="x">Pierwszy ciąg znaków.</param>
    /// <param name="y">Drugi ciąg znaków.</param>
    /// <returns>Wynik porównania.</returns>
    public int Compare(string? x, string? y)
    {
        return StrCmpLogicalW(x ?? string.Empty, y ?? string.Empty);
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);
}