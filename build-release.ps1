# Build self-contained single-file release for Windows x64.
# Output: ./release/CCRouter-win-x64/CCRouter.exe
#         ./release/CCRouter-win-x64.zip

$ErrorActionPreference = "Stop"

$Root      = $PSScriptRoot
$OutDir    = Join-Path $Root "release\CCRouter-win-x64"
$ZipPath   = Join-Path $Root "release\CCRouter-win-x64.zip"

Write-Host "Cleaning previous release..." -ForegroundColor Cyan
if (Test-Path $OutDir)  { Remove-Item $OutDir  -Recurse -Force }
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

Write-Host "Publishing..." -ForegroundColor Cyan
dotnet publish $Root/CCRouter.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $OutDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Copy README and LICENSE next to the exe
Copy-Item (Join-Path $Root "README.md") $OutDir
Copy-Item (Join-Path $Root "LICENSE")   $OutDir

Write-Host "Zipping..." -ForegroundColor Cyan
Compress-Archive -Path "$OutDir\*" -DestinationPath $ZipPath -Force

$exeSize = [math]::Round((Get-Item (Join-Path $OutDir "CCRouter.exe")).Length / 1MB, 1)
$zipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)

Write-Host ""
Write-Host "Release built successfully" -ForegroundColor Green
Write-Host ("  exe : {0}\CCRouter.exe  [{1} MB]" -f $OutDir, $exeSize)
Write-Host ("  zip : {0}  [{1} MB]" -f $ZipPath, $zipSize)
