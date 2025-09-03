// ====================================================================================================
// <copyright file="PDFTools.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor.PDF;

using Syncfusion.Pdf;
using Syncfusion.Pdf.Exporting;
using Syncfusion.Pdf.Parsing;

/// <summary>
/// Klasa narzędziowa do obsługi plików PDF.
/// </summary>
internal static class PDFTools
{
    /// <summary>
    /// Obraca pierwszą stronę dostarczonego dokumentu PDF i zapisuje zmiany do wskazanej lokalizacji na dysku.
    /// </summary>
    /// <param name="loadedDocument">Wcześniej załadowany dokument PDF, który ma zostać zmodyfikowany.</param>
    /// <param name="savePath">Pełna ścieżka, pod którą zostanie zapisany zmodyfikowany plik.</param>
    /// <param name="rotateRight">True, aby obrócić w prawo; false, aby obrócić w lewo.</param>
    /// <returns>True, jeśli operacja się powiodła; w przeciwnym razie false.</returns>
    public static bool RotateAndSave(PdfLoadedDocument? loadedDocument, string savePath, bool rotateRight)
    {
        // Sprawdź, czy przekazany dokument i ścieżka są prawidłowe
        if (loadedDocument == null || string.IsNullOrWhiteSpace(savePath))
        {
            return false;
        }

        try
        {
            // Sprawdź, czy dokument zawiera strony
            if (loadedDocument.Pages.Count == 0)
            {
                return false;
            }

            // Pobierz pierwszą stronę dokumentu
            if (loadedDocument.Pages[0] is not PdfLoadedPage loadedPage)
            {
                return false;
            }

            int rotationAngle = (int)PdfPageRotateAngle.RotateAngle90; // Kąt obrotu 90 stopni

            int currentRotation = (int)loadedPage.Rotation; // Aktualny kąt obrotu strony (0, 90, 180, 270)

            int newRotation = rotateRight ? (currentRotation + rotationAngle) % 360 : (currentRotation - rotationAngle + 360) % 360; // Nowy kąt obrotu

            loadedPage.Rotation = (PdfPageRotateAngle)newRotation; // Ustaw nowy kąt obrotu strony

            // Zapisz zmiany do pliku na dysku pod wskazaną ścieżką
            loadedDocument.Save(savePath);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd podczas obracania i zapisywania pliku: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    /// Funkcja zwracająca rozdzielczość DPI pierwszego obrazu na pierwszej stronie dokumentu PDF,
    /// niezależnie od rotacji strony.
    /// </summary>
    /// <param name="pdfLoadedDocument">Dokument PDF.</param>
    /// <returns>Krotka (int, int) z wartościami DPI dla osi X i Y.</returns>
    public static (int dpiX, int dpiY) GetDPI(PdfLoadedDocument pdfLoadedDocument)
    {
        if (pdfLoadedDocument.Pages.Count <= 0 || pdfLoadedDocument.Pages[0] is not PdfLoadedPage page || page.ImagesInfo.Length <= 0)
        {
            return (0, 0);
        }

        PdfImageInfo imgInfo = page.ImagesInfo[0];

        // Właściwość `Image` może być `null`. Musimy to sprawdzić, zanim uzyskamy dostęp do jej właściwości.
        if (imgInfo.Image is not { } image)
        {
            // Jeśli nie możemy uzyskać obiektu obrazu, nie możemy obliczyć DPI.
            return (0, 0);
        }

        // Teraz jest już bezpiecznie. Właściwości Width i Height istnieją na obiekcie PdfImage.
        int widthPx = image.Width;
        int heightPx = image.Height;

        // Rozmiar ramki w punktach PDF (1 punkt = 1/72 cala)
        float widthPt = imgInfo.Bounds.Width;
        float heightPt = imgInfo.Bounds.Height;

        // Uwzględnij obrót strony:
        if (page.Rotation is PdfPageRotateAngle.RotateAngle90 or PdfPageRotateAngle.RotateAngle270)
        {
            (widthPt, heightPt) = (heightPt, widthPt);
        }

        // Uniknij dzielenia przez zero
        if (widthPt <= 0 || heightPt <= 0)
        {
            return (0, 0);
        }

        // Przeliczenie DPI
        double dpiX = widthPx / (widthPt / 72.0);
        double dpiY = heightPx / (heightPt / 72.0);

        return (Convert.ToInt32(dpiX), Convert.ToInt32(dpiY));
    }
}