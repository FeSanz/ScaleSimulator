<#
  Makes THIS Windows user trust the "ScaleSimulator Dev Signing" certificate, so the signed
  installer and exe are allowed by Smart App Control / SmartScreen.

  It adds only that one public certificate to the current user's Trusted Root and Trusted
  Publishers stores. Windows shows its own confirmation dialog for the Root store; nothing
  else is changed (Smart App Control itself is never turned off).

  Usage:  powershell -ExecutionPolicy Bypass -File tools\trust-signing-cert.ps1 [-CertFile path\to\ScaleSimulator-Signing.cer]
  Remove: certutil -user -delstore Root "ScaleSimulator Dev Signing"   (same for TrustedPublisher)
#>
param([string]$CertFile = (Join-Path (Split-Path -Parent $PSScriptRoot) "dist\ScaleSimulator-Signing.cer"))

$ErrorActionPreference = "Stop"
if (-not (Test-Path $CertFile)) { throw "Certificate file not found: $CertFile (run build-installer.ps1 first)" }

certutil -user -f -addstore Root $CertFile
certutil -user -f -addstore TrustedPublisher $CertFile
Write-Host "Done. The ScaleSimulator Dev Signing certificate is now trusted for this user."
