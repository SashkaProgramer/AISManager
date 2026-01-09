cd ..
dotnet publish -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained true /p:EnableCompressionInSingleFile=true /p:PublishReadyToRun=true /p:StripSymbols=true 
pause