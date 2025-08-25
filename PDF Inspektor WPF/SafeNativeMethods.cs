using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace PDF_Inspektor;

[SuppressUnmanagedCodeSecurity]
internal static class SafeNativeMethods
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    public static extern int StrCmpLogicalW(string psz1, string psz2);
}

/// <summary>
/// Compares two strings using the logical comparison method.
/// </summary>
public sealed class NaturalStringComparer : IComparer<string>
{
    /// <summary>
    /// Compares two strings using the logical comparison method.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public int Compare(string a, string b)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        return SafeNativeMethods.StrCmpLogicalW(a, b);
    }
}

/// <summary>
/// Compares two file names using the logical comparison method.
/// </summary>
public sealed class NaturalFileInfoNameComparer : IComparer<FileInfo>
{
    /// <summary>
    /// Compares two file names using the logical comparison method.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public int Compare(FileInfo a, FileInfo b)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        return SafeNativeMethods.StrCmpLogicalW(a.Name, b.Name);
    }
}