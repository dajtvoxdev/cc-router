# Build two release flavors for Windows x64:
#   1. Self-contained (compressed)  — no runtime needed on target, ~80 MB zip
#   2. Framework-dependent          — needs .NET 8 Desktop Runtime, ~2-5 MB zip
#
# Output:
#   ./release/CCRouter-win-x64.zip            (self-contained)
#   ./release/CCRouter-win-x64-fxdep.zip      (framework-dependent)

$ErrorActionPreference = "Stop"

$Root      = $PSScriptRoot
$SelfDir   = Join-Path $Root "release\CCRouter-win-x64"
$SelfZip   = Join-Path $Root "release\CCRouter-win-x64.zip"
$FxDir     = Join-Path $Root "release\CCRouter-win-x64-fxdep"
$FxZip     = Join-Path $Root "release\CCRouter-win-x64-fxdep.zip"

Write-Host "Cleaning previous release..." -ForegroundColor Cyan
foreach ($p in @($SelfDir, $SelfZip, $FxDir, $FxZip)) {
    if (Test-Path $p) { Remove-Item $p -Recurse -Force }
}

# ---------------- Self-contained (compressed single-file) ----------------
Write-Host "Publishing self-contained (compressed)..." -ForegroundColor Cyan
dotnet publish $Root/CCRouter.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $SelfDir
if ($LASTEXITCODE -ne 0) { throw "self-contained publish failed" }

Copy-Item (Join-Path $Root "README.md") $SelfDir
Copy-Item (Join-Path $Root "LICENSE")   $SelfDir

Write-Host "Zipping self-contained..." -ForegroundColor Cyan
Compress-Archive -Path "$SelfDir\*" -DestinationPath $SelfZip -Force

# ---------------- Framework-dependent (single-file) ----------------
Write-Host "Publishing framework-dependent..." -ForegroundColor Cyan
dotnet publish $Root/CCRouter.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $FxDir
if ($LASTEXITCODE -ne 0) { throw "framework-dependent publish failed" }

Copy-Item (Join-Path $Root "README.md") $FxDir
Copy-Item (Join-Path $Root "LICENSE")   $FxDir

$note = @"
This is the framework-dependent build of CCRouter.

REQUIREMENT: .NET 8 Desktop Runtime (x64) must be installed on the target machine.
Download: https://dotnet.microsoft.com/download/dotnet/8.0

If you don't want to install the runtime, use CCRouter-win-x64.zip (self-contained) instead.
"@
Set-Content -Path (Join-Path $FxDir "RUNTIME-REQUIRED.txt") -Value $note -Encoding UTF8

Write-Host "Zipping framework-dependent..." -ForegroundColor Cyan
Compress-Archive -Path "$FxDir\*" -DestinationPath $FxZip -Force

# ---------------- Report ----------------
$selfExeSize = [math]::Round((Get-Item (Join-Path $SelfDir "CCRouter.exe")).Length / 1MB, 1)
$selfZipSize = [math]::Round((Get-Item $SelfZip).Length / 1MB, 1)
$fxExeSize   = [math]::Round((Get-Item (Join-Path $FxDir   "CCRouter.exe")).Length / 1MB, 1)
$fxZipSize   = [math]::Round((Get-Item $FxZip).Length / 1MB, 1)

Write-Host ""
Write-Host "Releases built successfully" -ForegroundColor Green
Write-Host "  self-contained:"
Write-Host ("    exe : {0}\CCRouter.exe  [{1} MB]" -f $SelfDir, $selfExeSize)
Write-Host ("    zip : {0}  [{1} MB]" -f $SelfZip, $selfZipSize)
Write-Host "  framework-dependent (needs .NET 8 Desktop Runtime):"
Write-Host ("    exe : {0}\CCRouter.exe  [{1} MB]" -f $FxDir, $fxExeSize)
Write-Host ("    zip : {0}  [{1} MB]" -f $FxZip, $fxZipSize)
