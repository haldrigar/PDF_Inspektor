@echo off
rem Ustawia bie��cy katalog na folder, w kt�rym znajduje si� skrypt
cd /d "%~dp0"

echo Publikowanie PDF_Inspektor (razem z Updaterem)...

rem Publikuje g��wny projekt. Updater zostanie opublikowany automatycznie dzi�ki referencji.
rem Wszystkie ustawienia (x64, SelfContained, PublishSingleFile) s� wczytywane z plik�w .csproj.
dotnet publish PDF_Inspektor.sln -c Release

echo.
echo Publikowanie zako�czone. Pliki znajduj� si� w folderze:
echo PDF_Inspektor\bin\Release\net8.0-windows\win-x64\publish\
echo.
pause