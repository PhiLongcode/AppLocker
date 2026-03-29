# Publish AppLocker to Desktop\App Locker and create shortcuts (normal + --background).
# Run:  powershell -ExecutionPolicy Bypass -File scripts\Publish-ToDesktop.ps1

param(
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$desktop = [Environment]::GetFolderPath('Desktop')
$outDir = Join-Path $desktop 'App Locker'
$proj = Join-Path $repoRoot 'src\AppLocker.Presentation\AppLocker.Presentation.csproj'

$publishArgs = @(
    'publish', $proj,
    '-c', 'Release',
    '-o', $outDir,
    '-p:PublishSingleFile=false'
)

if ($SelfContained) {
    $publishArgs += @('--self-contained', 'true', '-r', 'win-x64', '-p:IncludeNativeLibrariesForSelfExtract=true')
}
else {
    $publishArgs += @('--self-contained', 'false')
}

Write-Host "Publishing to: $outDir"
& dotnet @publishArgs

$exeName = 'AppLocker.Presentation.exe'
$exePath = Join-Path $outDir $exeName
if (-not (Test-Path $exePath)) {
    throw "Missing output: $exePath"
}

$shell = New-Object -ComObject WScript.Shell

$scNormal = Join-Path $desktop 'AppLocker.lnk'
$s = $shell.CreateShortcut($scNormal)
$s.TargetPath = $exePath
$s.WorkingDirectory = $outDir
$s.Description = 'AppLocker full UI'
$s.Save()
Write-Host "Shortcut: $scNormal"

$scBg = Join-Path $desktop 'AppLocker (background).lnk'
$s2 = $shell.CreateShortcut($scBg)
$s2.TargetPath = $exePath
$s2.Arguments = '--background'
$s2.WorkingDirectory = $outDir
$s2.Description = 'AppLocker tray + auto monitor'
$s2.Save()
Write-Host "Shortcut: $scBg"

$dataPath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'AppLocker'
Write-Host ""
Write-Host "Done. Data folder (AppData): $dataPath"
