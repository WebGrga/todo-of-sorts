param(
    [string]$AndroidSdkDirectory,
    [string]$JavaSdkDirectory,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$CompactBeta,
    [ValidateRange(1, 2100000000)][int]$ApplicationVersion = 1
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (!$AndroidSdkDirectory) {
    if ($env:ANDROID_HOME) { $AndroidSdkDirectory = $env:ANDROID_HOME }
}
if (!$JavaSdkDirectory) { $JavaSdkDirectory = $env:JAVA_HOME }
$taskArgs = @('build', (Join-Path $taskRoot 'src/ToDoOfSorts.App/ToDoOfSorts.App.csproj'), '-f', 'net10.0-android', '-c', $Configuration)
if ($PSBoundParameters.ContainsKey('ApplicationVersion')) { $taskArgs += "-p:ApplicationVersion=$ApplicationVersion" }
if ($CompactBeta) {
    if ($Configuration -ne 'Release') { throw 'CompactBeta requires the Release configuration.' }
    # Profile common startup paths: modest size overhead, much quicker cold launch.
    $taskArgs += @('-p:RunAOTCompilation=true', '-p:AndroidEnableProfiledAot=true', '-p:AndroidKeyStore=false')
}
if ($AndroidSdkDirectory) { $taskArgs += "-p:AndroidSdkDirectory=$AndroidSdkDirectory" }
if ($JavaSdkDirectory) { $taskArgs += "-p:JavaSdkDirectory=$JavaSdkDirectory" }
& dotnet @taskArgs
if ($LASTEXITCODE -ne 0) { throw 'Android build failed.' }
$taskOutput = Join-Path $taskRoot 'artifacts/android'
New-Item -ItemType Directory -Force $taskOutput | Out-Null
[xml]$taskProject = Get-Content -LiteralPath (Join-Path $taskRoot 'src/ToDoOfSorts.App/ToDoOfSorts.App.csproj') -Raw
$taskPackageName = [string]$taskProject.Project.PropertyGroup.ApplicationId
$taskApk = Join-Path $taskRoot "src/ToDoOfSorts.App/bin/$Configuration/net10.0-android/$taskPackageName-Signed.apk"
if (!(Test-Path -LiteralPath $taskApk)) { throw "No signed APK was produced at $taskApk." }
Copy-Item -LiteralPath $taskApk -Destination (Join-Path $taskOutput 'ToDoOfSorts-0.1.0.apk') -Force
Copy-Item -LiteralPath $taskApk -Destination (Join-Path $taskOutput 'ToDoOfSorts-latest.apk') -Force
Write-Output "APK ready: $taskOutput"
