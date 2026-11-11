param(
    [Parameter(Mandatory = $true)]
    [string]$Root,
    [int]$Port = 18080
)

$ErrorActionPreference = 'Stop'

$folder = (Resolve-Path $Root).Path
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://127.0.0.1:$Port/")
$listener.Start()
Write-Output "Serving $folder at http://127.0.0.1:$Port/ (Ctrl+C to stop)"
try {
    while ($listener.IsListening) {
        $task = $listener.GetContextAsync()
        while (-not $task.AsyncWaitHandle.WaitOne(500)) {
        }

        $context = $task.Result
        $name = [System.IO.Path]::GetFileName($context.Request.Url.LocalPath)
        $path = Join-Path $folder $name
        if ($name -and (Test-Path $path -PathType Leaf)) {
            $stream = [System.IO.File]::OpenRead($path)
            try {
                $context.Response.ContentType = 'application/octet-stream'
                $context.Response.ContentLength64 = $stream.Length
                $stream.CopyTo($context.Response.OutputStream)
            }
            finally {
                $stream.Dispose()
            }
        }
        else {
            $context.Response.StatusCode = 404
        }

        Write-Output "$($context.Request.HttpMethod) $($context.Request.Url.LocalPath) $($context.Response.StatusCode)"
        $context.Response.Close()
    }
}
finally {
    $listener.Stop()
}
