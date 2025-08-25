// ====================================================================================================
// <copyright file="Program.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

using PDF_Inspektor_OLD;

namespace PDF_Inspektor;

/// <summary>
/// KLasa g³ówna programu.
/// </summary>
internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cXmRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdccnRTQmZfV0Z+X0RWYEk=");

        ApplicationConfiguration.Initialize();
        Application.Run(new FormMain());
    }
}