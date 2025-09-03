cd "PDF Inspektor WPF"
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=false

PDF_Inspektor_Updater
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
@pause