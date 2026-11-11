param(
    [string]$Sdk,
    [string]$Jdk
)

$ErrorActionPreference = 'Stop'

function Find-Sdk {
    foreach ($candidate in @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, 'C:\Program Files (x86)\Android\android-sdk', (Join-Path $env:LOCALAPPDATA 'Android\Sdk'))) {
        if ($candidate -and (Test-Path (Join-Path $candidate 'build-tools'))) {
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
$project = Join-Path $root 'Template.MobileApp\Template.MobileApp.csproj'
$manifest = Join-Path $root 'Template.MobileApp\Platforms\Android\AndroidManifest.xml'
$apk = Join-Path $root 'Template.MobileApp\bin\Release\net10.0-android\android-x64\template.kioskapp-Signed.apk'
$output = Join-Path $root '__Emulator\update'

$marker = 'android:testOnly="${testOnly}"'
$original = [System.IO.File]::ReadAllBytes($manifest)
$text = [System.Text.Encoding]::UTF8.GetString($original)
if (([regex]::Matches($text, [regex]::Escape($marker))).Count -ne 1) {
    throw 'The manifest has changed.'
}

try {
    [System.IO.File]::WriteAllText($manifest, $text.Replace($marker, "$marker android:usesCleartextTraffic=""true"""), (New-Object System.Text.UTF8Encoding($false)))
    foreach ($version in @(@{ Code = 1; Name = '1.0'; Folder = 'v1' }, @{ Code = 2; Name = '1.1'; Folder = 'v2' })) {
        dotnet build $project -c Release -f net10.0-android -p:RuntimeIdentifier=android-x64 --no-incremental "-p:ApplicationVersion=$($version.Code)" "-p:ApplicationDisplayVersion=$($version.Name)" -v:minimal
        if ($LASTEXITCODE -ne 0) { throw "Build of $($version.Name) failed." }
        $folder = Join-Path $output $version.Folder
        New-Item -ItemType Directory -Force $folder | Out-Null
        Copy-Item $apk $folder -Force
    }
}
finally {
    [System.IO.File]::WriteAllBytes($manifest, $original)
}

$other = Join-Path $output 'other'
New-Item -ItemType Directory -Force $other | Out-Null
$keystore = Join-Path $output 'other.keystore'
$password = Join-Path $output 'other.pass'
if (-not (Test-Path $keystore)) {
    [System.IO.File]::WriteAllText($password, [guid]::NewGuid().ToString('N'))
    & (Join-Path $Jdk 'bin\keytool.exe') -genkeypair -keystore $keystore -storepass:file $password -keypass:file $password -alias other -keyalg RSA -keysize 2048 -validity 365 -dname 'CN=Other'
    if ($LASTEXITCODE -ne 0) { throw 'keytool failed.' }
}

$buildTools = Get-ChildItem (Join-Path $Sdk 'build-tools') -Directory | Sort-Object { [version]($_.Name -replace '[^0-9.].*$', '') } | Select-Object -Last 1
$otherApk = Join-Path $other 'template.kioskapp-Signed.apk'
Copy-Item (Join-Path $output 'v2\template.kioskapp-Signed.apk') $otherApk -Force
& (Join-Path $Jdk 'bin\java.exe') -jar (Join-Path $buildTools.FullName 'lib\apksigner.jar') sign --ks $keystore --ks-pass "file:$password" --ks-key-alias other $otherApk
if ($LASTEXITCODE -ne 0) { throw 'apksigner failed.' }

Get-ChildItem $output -Recurse -Filter '*.apk' | ForEach-Object { Write-Output "$($_.FullName) $($_.Length)" }
