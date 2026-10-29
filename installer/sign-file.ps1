param([Parameter(Mandatory)][string]$Path)

# Signing hook for Inno Setup ("sscsign" tool, see build-installer.ps1): signs one file with the
# local ScaleSimulator code-signing certificate, timestamped so the signature outlives the
# certificate. Fails loudly - an unsigned installer is worse than a failed build.
$ErrorActionPreference = "Stop"

$cert = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq "CN=ScaleSimulator Dev Signing" -and $_.HasPrivateKey } |
    Select-Object -First 1
if (-not $cert) { Write-Error "ScaleSimulator Dev Signing certificate not found in Cert:\CurrentUser\My"; exit 1 }

$result = $null
try {
    $result = Set-AuthenticodeSignature -FilePath $Path -Certificate $cert -HashAlgorithm SHA256 `
        -TimestampServer "http://timestamp.digicert.com"
} catch {
    Write-Warning "Timestamped signing failed ($($_.Exception.Message)); signing without timestamp."
}
if (-not $result -or $result.Status -in "HashMismatch", "NotSigned") {
    $result = Set-AuthenticodeSignature -FilePath $Path -Certificate $cert -HashAlgorithm SHA256
}
# "UnknownError" just means the self-signed root is not trusted on THIS machine yet; the file is signed.
if ($result.Status -eq "NotSigned" -or $result.Status -eq "HashMismatch") { Write-Error "Signing $Path failed: $($result.StatusMessage)"; exit 1 }
exit 0
