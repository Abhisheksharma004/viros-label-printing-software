param([string]$Runtime = "win-x64")
dotnet restore
dotnet publish -c Release -r $Runtime --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:PublishTrimmed=false
Write-Host "Published to: bin\Release\net9.0-windows\$Runtime\publish"
