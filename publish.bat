@echo off
rem Ustawia bie¿¹cy katalog na folder, w którym znajduje siê skrypt
cd /d "%~dp0"

echo Publikowanie PDF_Inspektor i PDF_Inspektor_Updater...

rem Publikuje ca³¹ solucjê dla architektury x64
dotnet publish PDF_Inspektor.sln -c Release -r win-x64 --self-contained false /p:PublishSingleFile=false /p:PublishSingleFileUpdater=true

echo.
echo Publikowanie zakoñczone.
pause