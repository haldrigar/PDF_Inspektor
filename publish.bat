@echo off
rem Ustawia bie¿¹cy katalog na folder, w którym znajduje siê skrypt
cd /d "%~dp0"

echo Publikowanie PDF_Inspektor (razem z Updaterem)...

rem Publikuje g³ówny projekt. Updater zostanie opublikowany automatycznie dziêki referencji.
rem Wszystkie ustawienia (x64, SelfContained, PublishSingleFile) s¹ wczytywane z plików .csproj.
dotnet publish PDF_Inspektor.sln -c Release

echo.
echo Publikowanie zakoñczone. Pliki znajduj¹ siê w folderze:
echo PDF_Inspektor\bin\Release\net8.0-windows\win-x64\publish\
echo.
pause