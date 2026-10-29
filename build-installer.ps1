<#
  Builds the distributable Windows installer: dist\ScaleSimulator-Setup-<version>.exe

  Steps: run the tests -> publish the self-contained single-file app into a clean staging folder
  -> drop debug-only files -> sign the app binaries (when a dev certificate is available, see
  ScaleSim.App\sign.ps1) -> compile installer\ScaleSimulator.iss with Inno Setup.

  Inno Setup 6 is required:  winget install JRSoftware.InnoSetup
#>

param([switch]$SkipTests)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csproj = Join-Path $root "ScaleSim.App\ScaleSimulator.App.csproj"
$outDir = Join-Path $root "dist"
$stagingDir = Join-Path $outDir "staging"
$signScript = Join-Path $root "ScaleSim.App\sign.ps1"

$iscc = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 not found. Install it with: winget install JRSoftware.InnoSetup" }

$version = ([xml](Get-Content $csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { $version = "1.0.0" }
Write-Host "Simulador de Basculas $version"

if (-not $SkipTests) {
    Write-Host "Running tests..."
    dotnet test (Join-Path $root "ScaleSim.Tests\ScaleSim.Tests.csproj") --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Tests failed - not building an installer from a broken build." }
}

if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null

Write-Host "Publishing (Release, self-contained, single file)..."
dotnet publish $csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $stagingDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Debug symbols and API doc XML are build leftovers, not something a user needs installed.
Get-ChildItem $stagingDir -File | Where-Object { $_.Extension -in ".pdb", ".xml" } | Remove-Item -Force

Write-Host "Signing app binaries (skipped with a warning if no certificate)..."
& $signScript -TargetDir $stagingDir

$isccArgs = @("/DAppVersion=$version", "/DStagingDir=$stagingDir", "/DOutputDir=$outDir")
$cert = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq "CN=ScaleSimulator Dev Signing" -and $_.HasPrivateKey } | Select-Object -First 1
if ($cert) {
    # Public half only: the file a user imports (tools\trust-signing-cert.ps1) to trust this build.
    $certFile = Join-Path $outDir "ScaleSimulator-Signing.cer"
    Export-Certificate -Cert $cert -FilePath $certFile -Type CERT | Out-Null
    $isccArgs += "/DSignToolName=sscsign"
    $isccArgs += "/Ssscsign=powershell.exe -NoProfile -ExecutionPolicy Bypass -File `$q$(Join-Path $root 'installer\sign-file.ps1')`$q `$f"
} else {
    Write-Warning "No 'ScaleSimulator Dev Signing' certificate: building an UNSIGNED installer."
}

Write-Host "Compiling installer..."
& $iscc @isccArgs (Join-Path $root "installer\ScaleSimulator.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }

Remove-Item $stagingDir -Recurse -Force
$setup = Join-Path $outDir "ScaleSimulator-Setup-$version.exe"
$sizeMb = [math]::Round((Get-Item $setup).Length / 1MB, 1)
Write-Host ""
Write-Host "Installer: $setup ($sizeMb MB)"
Write-Host "Signature: $((Get-AuthenticodeSignature $setup).Status)"
