@echo off
rem Ustawia bie��cy katalog na folder, w kt�rym znajduje si� skrypt
cd /d "%~dp0"

echo Publikowanie PDF_Inspektor i PDF_Inspektor_Updater...

rem Publikuje ca�� solucj� dla architektury x64
dotnet publish PDF_Inspektor.sln -c Release -r win-x64 --self-contained false /p:PublishSingleFile=false /p:PublishSingleFileUpdater=true

echo.
echo Publikowanie zako�czone.
pause