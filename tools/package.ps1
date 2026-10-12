<# Builds, tests and zips the mod: dist/CompetentAI-<version>.zip (the zip contains the CompetentAI folder). #>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'build.ps1') -Test
$manifest = Get-Content (Join-Path $root 'mod\CompetentAI\manifest.json') -Raw | ConvertFrom-Json
$zip = Join-Path $root ("dist\CompetentAI-" + $manifest.ModVersion + ".zip")
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $root 'dist\CompetentAI') -DestinationPath $zip
Write-Host "Package: $zip"
