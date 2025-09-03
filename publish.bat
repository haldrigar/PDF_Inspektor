@echo off
cd PDF_Inspektor
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=false

cd PDF_Inspektor_Updater
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true

pause