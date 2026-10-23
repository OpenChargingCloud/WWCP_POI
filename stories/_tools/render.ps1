# Renders every SVG of the series to a PNG next to it, 1080 x 1350, with headless Chrome or Edge.
#
#   powershell -ExecutionPolicy Bypass -File render.ps1              every SVG
#   powershell -ExecutionPolicy Bypass -File render.ps1 register vault   those whose path contains one of them
#
# The browser runs with a profile of its own in %TEMP%, never with yours.

param([Parameter(ValueFromRemainingArguments = $true)] [string[]] $Only = @())

$root    = Split-Path -Parent $PSScriptRoot
$browser = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $browser) { throw "Neither Chrome nor Edge found." }

$userDataDir = Join-Path $env:TEMP "wwcp-poi-stories-render"

Get-ChildItem $root -Recurse -Filter *.svg | Where-Object { $_.FullName -notlike "*\_tools\*" } | ForEach-Object {
    $svg = $_.FullName
    if ($Only.Count -gt 0 -and -not ($Only | Where-Object { $svg -like "*$_*" })) { return }
    $png = [IO.Path]::ChangeExtension($svg, ".png")
    $url = "file:///" + ($svg -replace '\\', '/')
    & $browser --headless=new --disable-gpu --hide-scrollbars --no-first-run --no-default-browser-check `
               "--user-data-dir=$userDataDir" --force-device-scale-factor=1 --window-size=1080,1350 `
               "--screenshot=$png" $url 2>$null | Out-Null
    Write-Host $png
}
