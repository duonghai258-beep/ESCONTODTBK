#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$PktPath = "",
    [string]$WorkDir = "$env:USERPROFILE\Documents\SAC2022_BTXM5"
)

$ErrorActionPreference = "Stop"

Write-Host "=== SAC 2022 BTXM 5.0m launcher ===" -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null

# Locate Subassembly Composer 2022 without assuming a single language/install path.
$candidates = @(
    "$env:ProgramFiles\Autodesk\AutoCAD 2022\C3D\SubassemblyComposer.exe",
    "$env:ProgramFiles\Autodesk\AutoCAD 2022\C3D\SubassemblyComposer\SubassemblyComposer.exe",
    "$env:ProgramFiles\Autodesk\AutoCAD 2022\C3D\SubassemblyComposer.exe",
    "$env:ProgramFiles\Autodesk\Civil 3D 2022\SubassemblyComposer.exe",
    "$env:ProgramFiles\Autodesk\Civil 3D 2022\C3D\SubassemblyComposer.exe",
    "$env:ProgramFiles(x86)\Autodesk\AutoCAD 2022\C3D\SubassemblyComposer.exe",
    "$env:ProgramFiles(x86)\Autodesk\Civil 3D 2022\SubassemblyComposer.exe"
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

if (-not $candidates) {
    $found = Get-ChildItem "$env:ProgramFiles\Autodesk" -Filter "SubassemblyComposer.exe" -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
    if ($found) { $candidates = @($found) }
}

if (-not $candidates) {
    throw "Khong tim thay SubassemblyComposer.exe. Cai/kiem tra Autodesk Civil 3D 2022 + Subassembly Composer truoc."
}

$sacExe = $candidates[0]
Write-Host "SAC: $sacExe" -ForegroundColor Green

if ([string]::IsNullOrWhiteSpace($PktPath)) {
    $defaultPkt = Join-Path $WorkDir "BTXM5_REHAB_2022.pkt"
    if (Test-Path -LiteralPath $defaultPkt) {
        $PktPath = $defaultPkt
    }
}

if ($PktPath -and (Test-Path -LiteralPath $PktPath)) {
    Write-Host "Mo PKT: $PktPath" -ForegroundColor Green
    Start-Process -FilePath $sacExe -ArgumentList ('"{0}"' -f (Resolve-Path -LiteralPath $PktPath))
    exit 0
}

Write-Host ""
Write-Host "CHUA CO PKT: $WorkDir\BTXM5_REHAB_2022.pkt" -ForegroundColor Yellow
Write-Host "SAC 2022 se duoc mo de tao/kiem tra PKT." -ForegroundColor Yellow
Write-Host ""
Write-Host "11 INPUTS DA KHOA:" -ForegroundColor Cyan
@(
"W_New=2.50","Thick_BTXM=0.20","W_Old=1.50","Thick_Old=0.18",
"Slope_ThietKe=-2.0%","Thick_CPDD=0.12","Thick_HC=0.10",
"W_Le=1.00","Slope_Le=-4.0%","Slope_Cut=1:1","Slope_Fill=1:1.5"
) | ForEach-Object { Write-Host "  $_" }

Write-Host ""
Write-Host "TARGETS:" -ForegroundColor Cyan
@("Design_Profile=Elevation","EG=Surface","Old_Road=Surface") |
    ForEach-Object { Write-Host "  $_" }

Write-Host ""
Write-Host "Khong tu tao PKT/XML khong duoc Autodesk xac nhan." -ForegroundColor Yellow
Write-Host "Autodesk xac nhan SAC dung flowchart/properties/preview va Save as PKT." -ForegroundColor Yellow
Start-Process -FilePath $sacExe
