<#
.SYNOPSIS  Builds the mod and assembles dist/CompetentAI (ready to drop into BladeAndSorcery_Data/StreamingAssets/Mods).
.PARAMETER Deploy   Also copies the result into the game's Mods folder (default path auto-detected from $env:BAS_GAME or the Steam path below).
.PARAMETER Test     Runs the unit tests first and aborts on failure.
#>
param([switch]$Deploy, [switch]$Test, [string]$Game = $(if ($env:BAS_GAME) { $env:BAS_GAME } else { 'D:\SteamLibrary\steamapps\common\Blade & Sorcery' }))
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
if ($Test) { dotnet test tests/CompetentAI.Core.Tests/CompetentAI.Core.Tests.csproj -c Release; if ($LASTEXITCODE) { throw 'unit tests failed' } }
$env:BAS_MANAGED = Join-Path $Game 'BladeAndSorcery_Data\Managed'
dotnet build src/CompetentAI.Mod/CompetentAI.Mod.csproj -c Release; if ($LASTEXITCODE) { throw 'build failed' }
$dist = Join-Path $root 'dist\CompetentAI'
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Force $dist | Out-Null
$bin = Join-Path $root 'src\CompetentAI.Mod\bin\Release\netstandard2.1'
Copy-Item (Join-Path $bin 'CompetentAI.dll') $dist
Copy-Item (Join-Path $bin 'CompetentAI.pdb') $dist -ErrorAction SilentlyContinue
Copy-Item (Join-Path $root 'mod\CompetentAI\*') $dist -Recurse
Copy-Item (Join-Path $root 'README.md') (Join-Path $dist 'README.md') -ErrorAction SilentlyContinue
Write-Host "Built: $dist"
if ($Deploy) {
  $mods = Join-Path $Game 'BladeAndSorcery_Data\StreamingAssets\Mods\CompetentAI'
  $keep = Join-Path $mods 'CompetentAI.config.json'
  $savedConfig = $null
  if (Test-Path $keep) { $savedConfig = Get-Content $keep -Raw }
  if (Test-Path $mods) { Remove-Item $mods -Recurse -Force }
  Copy-Item $dist $mods -Recurse
  if ($savedConfig) { Set-Content (Join-Path $mods 'CompetentAI.config.json') $savedConfig -NoNewline; Write-Host 'Kept your existing CompetentAI.config.json' }
  Write-Host "Deployed to: $mods"
}
