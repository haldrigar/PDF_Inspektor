// ====================================================================================================
// <copyright file="App.xaml.cs" company="Grzegorz Gogolewski">
// Copyright (c) Grzegorz Gogolewski. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// ====================================================================================================

namespace PDF_Inspektor;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
public partial class App
{
    /// <summary>
    /// Inicjalizuje nową instancję klasy <see cref="App"/> i rejestruje licencję Syncfusion.
    /// </summary>
    public App()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cXmRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdcc3RQRmNYWUR2W0NWYEk=");
    }
}
