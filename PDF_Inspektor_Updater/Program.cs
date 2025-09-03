// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace PDF_Inspektor_Updater;

using System.Diagnostics;

/// <summary>
/// Program aktualizujący aplikację PDF Inspektor.
/// </summary>
public static class Program
{
    private static int Main(string[] args)
    {
        /*if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }*/

        if (args.Length < 3)
        {
            Console.WriteLine("Za mało argumentów!");
            return 1;
        }

        string updatePath = args[0];
        string localPath = args[1];
        string mainExeFile = args[2];

        try
        {
            foreach (Process proc in Process.GetProcessesByName("PDF_Inspektor"))
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit();
                }
                catch
                {
                    // ignored
                }
            }

            Thread.Sleep(1000);

            foreach (string updateFilePath in Directory.GetFiles(updatePath, "*.*", SearchOption.AllDirectories))
            {
                string updateFileName = Path.GetFileName(updateFilePath);

                string localFilePath = Path.Combine(localPath, updateFileName);

                if (!File.Exists(localFilePath) || File.GetLastWriteTime(updateFilePath) > File.GetLastWriteTime(localFilePath))
                {
                    File.Copy(updateFilePath, localFilePath, true);

                    Console.WriteLine($"Zaktualizowano: {updateFileName}");
                }
            }

            Console.WriteLine("Aktualizacja zakończona pomyślnie.");
            Console.WriteLine("Naciśnij dowolny klawisz, aby uruchomić program...");
            Console.ReadKey(false);

            // Uruchom program główny
            Process.Start(mainExeFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Błąd podczas aktualizacji: " + ex.Message);

            return 2; // Zwróć kod błędu
        }

        return 0; // Zwróć kod sukcesu
    }
}