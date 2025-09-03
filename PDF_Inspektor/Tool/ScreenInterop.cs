// ====================================================================================================
// <copyright file="ScreenInterop.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor.Tool;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

/// <summary>
/// Klasa pomocnicza do uzyskiwania informacji o ekranie za pomocą Win32 API,
/// bez potrzeby dołączania referencji do System.Windows.Forms.
/// </summary>
public static class ScreenInterop
{
    // --- Definicje struktur i stałych Win32 ---
    private const int MonitorDefaultToNearest = 0x00000002;
    private const int MonitorDefaultToPrimary = 0x00000001;

    /// <summary>
    /// Zwraca obszar roboczy ekranu, na którym znajduje się dane okno.
    /// </summary>
    /// <param name="window">Okno, dla którego ma być znaleziony ekran.</param>
    /// <returns>Obszar roboczy (bez paska zadań) jako obiekt Rect.</returns>
    public static System.Windows.Rect GetScreenWorkArea(Window window)
    {
        var windowHandle = new WindowInteropHelper(window).Handle;
        var monitorHandle = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);

        var monitorInfo = default(Monitorinfo);

        monitorInfo.CbSize = Marshal.SizeOf(monitorInfo);
        GetMonitorInfo(monitorHandle, ref monitorInfo);

        // Zwróć obszar roboczy (rcWork)
        return new System.Windows.Rect(
            monitorInfo.RcWork.Left,
            monitorInfo.RcWork.Top,
            monitorInfo.RcWork.Right - monitorInfo.RcWork.Left,
            monitorInfo.RcWork.Bottom - monitorInfo.RcWork.Top);
    }

    /// <summary>
    /// Zwraca obszar roboczy ekranu głównego.
    /// </summary>
    /// <returns>Obszar roboczy (bez paska zadań) jako obiekt Rect.</returns>
    public static System.Windows.Rect GetPrimaryScreenWorkArea()
    {
        var monitorHandle = MonitorFromWindow(nint.Zero, MonitorDefaultToPrimary);
        var monitorInfo = default(Monitorinfo);

        monitorInfo.CbSize = Marshal.SizeOf(monitorInfo);

        GetMonitorInfo(monitorHandle, ref monitorInfo);

        // Zwróć obszar roboczy (rcWork)
        return new System.Windows.Rect(
            monitorInfo.RcWork.Left,
            monitorInfo.RcWork.Top,
            monitorInfo.RcWork.Right - monitorInfo.RcWork.Left,
            monitorInfo.RcWork.Bottom - monitorInfo.RcWork.Top);
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref Monitorinfo lpmi);

    /// <summary>
    /// Struktura reprezentująca prostokąt (używana w Win32 API).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        /// <summary>
        /// współrzędna X lewego górnego rogu.
        /// </summary>
        public int Left;

        /// <summary>
        /// Współrzędna Y lewego górnego rogu.
        /// </summary>
        public int Top;

        /// <summary>
        /// Współrzędna X prawego dolnego rogu.
        /// </summary>
        public int Right;

        /// <summary>
        /// Współrzędna Y prawego dolnego rogu.
        /// </summary>
        public int Bottom;
    }

    /// <summary>
    /// Struktura przechowująca informacje o monitorze (używana w Win32 API).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct Monitorinfo
    {
        public int CbSize;
        public Rect RcMonitor;
        public Rect RcWork;
        public uint DwFlags;
    }
}