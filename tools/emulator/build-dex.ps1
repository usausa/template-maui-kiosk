param(
    [string]$Sdk,
    [string]$Jdk
)

$ErrorActionPreference = 'Stop'

function Find-Sdk {
    foreach ($candidate in @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, 'C:\Program Files (x86)\Android\android-sdk', (Join-Path $env:LOCALAPPDATA 'Android\Sdk'))) {
        if ($candidate -and (Test-Path (Join-Path $candidate 'platforms'))) {
            return $candidate
        }
    }
    throw 'Android SDK not found. Pass -Sdk.'
}

function Find-Jdk {
    $candidates = @($env:JAVA_HOME, 'C:\Program Files\Android\Android Studio\jbr')
    $candidates += Get-ChildItem 'C:\Program Files\Microsoft', 'C:\Program Files\Eclipse Adoptium' -Directory -Filter 'jdk-*' -ErrorAction SilentlyContinue | Sort-Object Name -Descending | ForEach-Object FullName
    foreach ($candidate in $candidates) {
        $javac = if ($candidate) { Join-Path $candidate 'bin\javac.exe' } else { $null }
        if ($javac -and (Test-Path $javac)) {
            $ErrorActionPreference = 'Continue'
            $version = (& $javac -version 2>&1 | ForEach-Object { "$_" }) -join ' '
            $ErrorActionPreference = 'Stop'
            if ($version -match 'javac (\d+)' -and [int]$Matches[1] -ge 11) {
                return $candidate
            }
        }
    }
    throw 'JDK 11 or later not found. Pass -Jdk.'
}

if (-not $Sdk) { $Sdk = Find-Sdk }
if (-not $Jdk) { $Jdk = Find-Jdk }

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$output = Join-Path $root '__Emulator'
$classes = Join-Path $output 'classes'
New-Item -ItemType Directory -Force $classes | Out-Null

$platform = Get-ChildItem (Join-Path $Sdk 'platforms') -Directory -Filter 'android-*' | Sort-Object Name | Select-Object -Last 1
$androidJar = Join-Path $platform.FullName 'android.jar'
$buildTools = Get-ChildItem (Join-Path $Sdk 'build-tools') -Directory | Sort-Object { [version]($_.Name -replace '[^0-9.].*$', '') } | Select-Object -Last 1
$d8 = Join-Path $buildTools.FullName 'lib\d8.jar'

$sources = Get-ChildItem $PSScriptRoot -Filter '*.java' | ForEach-Object FullName
& (Join-Path $Jdk 'bin\javac.exe') --release 11 -cp $androidJar -d $classes @sources
if ($LASTEXITCODE -ne 0) { throw 'javac failed.' }

$compiled = Get-ChildItem $classes -Filter '*.class' | ForEach-Object FullName
& (Join-Path $Jdk 'bin\java.exe') -cp $d8 com.android.tools.r8.D8 --lib $androidJar --output $output @compiled
if ($LASTEXITCODE -ne 0) { throw 'd8 failed.' }

$dex = Join-Path $output 'kiosk-tools.dex'
Move-Item (Join-Path $output 'classes.dex') $dex -Force
Write-Output $dex
