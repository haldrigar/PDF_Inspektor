// ====================================================================================================
// <copyright file="PDFTools.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using System.IO;

using Syncfusion.Pdf;
using Syncfusion.Pdf.Exporting;
using Syncfusion.Pdf.Parsing;

/// <summary>
/// Klasa narzędziowa do obsługi plików PDF.
/// </summary>
internal static class PDFTools
{
    /// <summary>
    /// Obraca pierwszą stronę dokumentu PDF o 90 stopni i zapisuje zmiany.
    /// </summary>
    /// <param name="filePath">Ścieżka do pliku PDF.</param>
    /// <param name="rotateRight">True, aby obrócić w prawo; false, aby obrócić w lewo.</param>
    /// <returns>True, jeśli operacja się powiodła; w przeciwnym razie false.</returns>
    public static bool RotateAndSave(string filePath, bool rotateRight)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            // Załaduj dokument bezpośrednio z pliku. Syncfusion poradzi sobie z tymczasowym dostępem.
            var loadedDocument = new PdfLoadedDocument(filePath);

            // Sprawdzenie, czy dokument ma co najmniej jedną stronę
            if (loadedDocument.Pages.Count == 0)
            {
                loadedDocument.Close(true);
                return false;
            }

            if (loadedDocument.Pages[0] is not PdfLoadedPage loadedPage)
            {
                loadedDocument.Close(true);
                return false;
            }

            int rotationAngle = (int)PdfPageRotateAngle.RotateAngle90;
            int currentRotation = (int)loadedPage.Rotation;
            int newRotation = rotateRight
                ? (currentRotation + rotationAngle) % 360
                : (currentRotation - rotationAngle + 360) % 360;

            loadedPage.Rotation = (PdfPageRotateAngle)newRotation;

            // Zapisz i zamknij dokument, zwalniając zasoby.
            loadedDocument.Save();
            loadedDocument.Close(true);

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
    /// <returns>Wartość DPI.</returns>
    public static (int dpiX, int dpiY) GetDPI(PdfLoadedDocument pdfLoadedDocument)
    {
        // Sprawdź, czy dokument zawiera strony
        if (pdfLoadedDocument.Pages.Count <= 0)
        {
            return (0, 0);
        }

        PdfLoadedPage page = (PdfLoadedPage)pdfLoadedDocument.Pages[0]; // Pobierz pierwszą stronę dokumentu

        // Sprawdź, czy strona zawiera obrazy
        if (page.ImagesInfo.Length <= 0)
        {
            return (0, 0);
        }

        PdfImageInfo imgInfo = page.ImagesInfo[0]; // Pobierz informacje o pierwszym obrazie na pierwszej stronie

        // Rozdzielczość w pikselach obrazu (oryginalny rozmiar obrazu) bez względu na skalowanie w PDF
        int widthPx = imgInfo.Image.Width;
        int heightPx = imgInfo.Image.Height;

        // Rozmiar ramki w punktach PDF (1 punkt = 1/72 cala) - rozmiar obrazu w dokumencie PDF zależy od kąta obrotu strony. Obraz może być obrócony, ale jego rozmiar w pikselach pozostaje taki sam.
        float widthPt = imgInfo.Bounds.Width;
        float heightPt = imgInfo.Bounds.Height;

        // Uwzględnij obrót strony:
        int rotation = (int)page.Rotation;

        // rotation: 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270° (Syncfusion.Pdf.PdfPageRotateAngle)
        // Jeśli strona jest obrócona o 90° lub 270°, zamień szerokość z wysokością
        if (rotation is 1 or 3)
        {
            (widthPt, heightPt) = (heightPt, widthPt);
        }

        // Przeliczenie DPI
        double dpiX = widthPx / (widthPt / 72.0);
        double dpiY = heightPx / (heightPt / 72.0);

        return (Convert.ToInt32(dpiX), Convert.ToInt32(dpiY));
    }
}