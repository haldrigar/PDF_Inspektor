// ====================================================================================================
// <copyright file="SafeNativeMethods.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.IO;
using System.Runtime.InteropServices;
using System.Security;

/// <summary>
/// Klasa zawierająca bezpieczne metody natywne.
/// </summary>
[SuppressUnmanagedCodeSecurity]
internal static class SafeNativeMethods
{
    /// <summary>
    /// Funkcja porównująca dwa ciągi znaków za pomocą metody logicznej.
    /// </summary>
    /// <param name="psz1">Pierwszy ciąg znaków.</param>
    /// <param name="psz2">Drugi ciąg znaków.</param>
    /// <returns>WYnik prówniania.</returns>
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    public static extern int StrCmpLogicalW(string psz1, string psz2);
}

/// <summary>
/// Klasa porównująca dwa ciągi znaków za pomocą metody logicznej.
/// </summary>
public sealed class NaturalStringComparer : IComparer<string>
{
    /// <summary>
    /// Funkcja porównująca dwa ciągi znaków za pomocą metody logicznej.
    /// </summary>
    /// <param name="a">Pierwszy ciąg znaków.</param>
    /// <param name="b">Drugi ciąg znaków.</param>
    /// <returns>Wynik porównania.</returns>
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public int Compare(string a, string b)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        return SafeNativeMethods.StrCmpLogicalW(a, b);
    }
}

/// <summary>
/// Klasa porównująca dwa pliki za pomocą metody logicznej na podstawie ich nazw.
/// </summary>
public sealed class NaturalFileInfoNameComparer : IComparer<FileInfo>
{
    /// <summary>
    /// Funkcja porównująca dwa pliki za pomocą metody logicznej na podstawie ich nazw.
    /// </summary>
    /// <param name="a">Pierwsza nazwa pliku.</param>
    /// <param name="b">Druga nazwa pliku.</param>
    /// <returns>Wynik porównania.</returns>
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public int Compare(FileInfo a, FileInfo b)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        return SafeNativeMethods.StrCmpLogicalW(a.Name, b.Name);
    }
}