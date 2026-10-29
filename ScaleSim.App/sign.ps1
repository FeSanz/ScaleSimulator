param(
    [Parameter(Mandatory)][string]$TargetDir
)

# Signs the app binaries with a local code-signing certificate so Windows 11 Smart App Control
# lets the build run. Prefers a dedicated "ScaleSimulator Dev Signing" certificate; falls back to
# the "NotiLink Dev Signing" one already trusted on this machine. Never fails the build.

$subjects = @("CN=ScaleSimulator Dev Signing", "CN=NotiLink Dev Signing")
$cert = foreach ($s in $subjects) {
    $c = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $s -and $_.HasPrivateKey } | Select-Object -First 1
    if ($c) { $c; break }
}

if (-not $cert) {
    Write-Warning "No dev signing certificate found in Cert:\CurrentUser\My - skipping signing. Smart App Control may block the app."
    exit 0
}

foreach ($name in @("ScaleSimulator.exe", "ScaleSimulator.dll", "ScaleSim.Core.dll", "ScaleSim.Radwag.dll")) {
    $path = Join-Path $TargetDir $name
    if (Test-Path $path) {
        $result = Set-AuthenticodeSignature -FilePath $path -Certificate $cert
        if ($result.Status -ne "Valid") {
            Write-Warning "Signing $name did not result in a valid signature: $($result.StatusMessage)"
        }
    }
}
exit 0
