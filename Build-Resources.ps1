param(
    [string]$Unity = 'C:\Program Files\Unity\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Unity -PathType Leaf)) { throw 'Specify the Unity 2019.1.8f1 executable with -Unity.' }
$projectPath = Join-Path $PSScriptRoot 'UnityResourcesProject'
$logDirectory = Join-Path $PSScriptRoot 'build-logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
foreach ($platform in @('Windows', 'Linux')) {
    $logPath = Join-Path $logDirectory ('assets-' + $platform + '.log')
    $process = Start-Process -FilePath $Unity -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $projectPath + '"'), '-executeMethod', ('BuildEctAssets.Build' + $platform), '-logFile', ('"' + $logPath + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $logPath -SimpleMatch ('ECT_ASSETS_OK platform=' + $platform))) { throw "Asset build failed: $platform. See $logPath" }
    dotnet build (Join-Path $PSScriptRoot 'src/EnhancedCarTuning.Resources/EnhancedCarTuning.Resources.csproj') -c Release "-p:ResourcesPlatform=$platform"
    if ($LASTEXITCODE -ne 0) { throw "Resource assembly build failed: $platform" }
}
