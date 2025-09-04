// ====================================================================================================
// <copyright file="ScreenInterop.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// 
// Ostatni zapis pliku: 2025-09-04 13:43:23
// ====================================================================================================

namespace PDF_Inspektor.Tool;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

/// <summary>
/// Klasa pomocnicza do uzyskiwania informacji o ekranie za pomocą Win32 API,
/// bez potrzeby dołączania referencji do System.Windows.Forms.
/// </summary>
internal static class ScreenInterop
{
    // --- Definicje struktur i stałych Win32 ---
    /// <summary>
    /// Stała określająca, że monitor ma być wybrany na podstawie najbliższego do okna.
    /// </summary>
    private const int MonitorDefaultToNearest = 0x00000002;

    /// <summary>
    /// Stała określająca, że monitor ma być domyślnym monitorem głównym.
    /// </summary>
    private const int MonitorDefaultToPrimary = 0x00000001;

    /// <summary>
    /// Zwraca obszar roboczy ekranu, na którym znajduje się dane okno.
    /// </summary>
    /// <param name="window">Okno, dla którego ma być znaleziony ekran.</param>
    /// <returns>Obszar roboczy (bez paska zadań) jako obiekt Rect.</returns>
    public static System.Windows.Rect GetScreenWorkArea(Window window)
    {
        IntPtr windowHandle = new WindowInteropHelper(window).Handle;
        IntPtr monitorHandle = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);

        Monitorinfo monitorInfo = default;

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
        IntPtr monitorHandle = MonitorFromWindow(nint.Zero, MonitorDefaultToPrimary);
        Monitorinfo monitorInfo = default;

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
    /// Funkcja Win32 zwracająca uchwyt do monitora, na którym znajduje się dane okno.
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="dwFlags"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    /// <summary>
    /// Funkcja Win32 pobierająca informacje o monitorze.
    /// </summary>
    /// <param name="hMonitor"></param>
    /// <param name="lpmi"></param>
    /// <returns></returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref Monitorinfo lpmi);

    /// <summary>
    /// Struktura reprezentująca prostokąt (używana w Win32 API).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
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
        /// <summary>
        /// Wielkość struktury w bajtach.
        /// </summary>
        public int CbSize;

        /// <summary>
        /// Czy monitor jest monitorem głównym.
        /// </summary>
        public Rect RcMonitor;

        /// <summary>
        /// Współrzędne obszaru roboczego (bez paska zadań).
        /// </summary>
        public Rect RcWork;

        /// <summary>
        /// Flagi monitora (np. czy jest główny).
        /// </summary>
        public uint DwFlags;
    }
}