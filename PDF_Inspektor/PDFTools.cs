// ====================================================================================================
// <copyright file="PDFTools.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

using Syncfusion.Pdf;
using Syncfusion.Pdf.Exporting;
using Syncfusion.Pdf.Parsing;

internal static class PDFTools
{
    public static string GetDPI(PdfLoadedDocument pdfLoadedDocument)
    {
        PdfLoadedPage page = (PdfLoadedPage)pdfLoadedDocument.Pages[0]; // Pobierz pierwszą stronę dokumentu

        PdfImageInfo imgInfo = page.ImagesInfo[0]; // Pobierz informacje o pierwszym obrazie na stronie

        // Rozdzielczość w pikselach
        int widthPx = imgInfo.Image.Width;
        int heightPx = imgInfo.Image.Height;

        // Rozmiar ramki w punktach PDF (1 punkt = 1/72 cala)
        float widthPt = imgInfo.Bounds.Width;
        float heightPt = imgInfo.Bounds.Height;

        // Przeliczenie DPI
        double dpiX = widthPx / (widthPt / 72.0);
        double dpiY = heightPx / (heightPt / 72.0);

        return $"{dpiX:F0} x {dpiY:F0}";
    }
}
