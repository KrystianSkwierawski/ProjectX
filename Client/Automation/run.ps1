param(
    [string]$UnityPath = "",
    [string]$Configuration = "Debug",
    [string]$ApiExePath = "",
    [string]$ServerBuildPath = "",
    [int]$ApiPort = 5001,
    [int]$ClientDelaySeconds = 5,
    [switch]$SkipApi,
    [switch]$SkipServerBuild,
    [switch]$SkipServerRun,
    [switch]$SkipClientPlay,
    [switch]$RestartExisting
)

$ErrorActionPreference = "Stop"

$clientPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repoRoot = (Resolve-Path (Join-Path $clientPath "..")).Path
$apiProjectPath = Join-Path $repoRoot "API\src\API\API.csproj"

if ([string]::IsNullOrWhiteSpace($ApiExePath)) {
    $ApiExePath = Join-Path $repoRoot "API\src\API\bin\$Configuration\net9.0\ProjectX.API.exe"
}

if ([string]::IsNullOrWhiteSpace($ServerBuildPath)) {
    $ServerBuildPath = Join-Path $clientPath "Builds\Server\ProjectXServer.exe"
}

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Resolve-UnityPath {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        if (Test-Path $RequestedPath) {
            return (Resolve-Path $RequestedPath).Path
        }

        throw "Unity executable was not found at '$RequestedPath'."
    }

    $projectVersionPath = Join-Path $clientPath "ProjectSettings\ProjectVersion.txt"
    $version = $null
    if (Test-Path $projectVersionPath) {
        $versionLine = Get-Content $projectVersionPath | Where-Object { $_ -match "^m_EditorVersion:\s*(.+)$" } | Select-Object -First 1
        if ($versionLine -match "^m_EditorVersion:\s*(.+)$") {
            $version = $Matches[1].Trim()
        }
    }

    if ($version) {
        $versionUnityPath = Join-Path "C:\Program Files\Unity\Hub\Editor" "$version\Editor\Unity.exe"
        if (Test-Path $versionUnityPath) {
            return $versionUnityPath
        }
    }

    $hubRoot = "C:\Program Files\Unity\Hub\Editor"
    if (Test-Path $hubRoot) {
        $latest = Get-ChildItem $hubRoot -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "Editor\Unity.exe" } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1

        if ($latest) {
            return $latest
        }
    }

    throw "Unity.exe was not found. Pass -UnityPath `"C:\Path\To\Unity.exe`"."
}

function Get-ProcessesByExecutablePath {
    param([string]$ExecutablePath)

    $resolvedPath = [System.IO.Path]::GetFullPath($ExecutablePath)
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object {
            try {
                $_.Path -and ([System.IO.Path]::GetFullPath($_.Path) -ieq $resolvedPath)
            }
            catch {
                $false
            }
        }
}

function Stop-ProcessByExecutablePath {
    param(
        [string]$ExecutablePath,
        [string]$Name
    )

    $processes = @(Get-ProcessesByExecutablePath $ExecutablePath)
    if ($processes.Count -eq 0) {
        return
    }

    Write-Host "Stopping existing $Name process(es) from $ExecutablePath"
    foreach ($process in $processes) {
        Stop-Process -Id $process.Id -Force
    }
}

function Ensure-ApiBuilt {
    if (Test-Path $ApiExePath) {
        return
    }

    Write-Step "API executable missing, building $Configuration"
    dotnet build $apiProjectPath -c $Configuration
}

function Start-Executable {
    param(
        [string]$ExecutablePath,
        [string]$WorkingDirectory,
        [string]$Name,
        [string]$Arguments = ""
    )

    $processes = @(Get-ProcessesByExecutablePath $ExecutablePath)
    if ($processes.Count -gt 0) {
        Write-Host "$Name is already running from $ExecutablePath"
        return $processes[0].Id
    }

    Write-Host "Starting $Name"
    if ([string]::IsNullOrWhiteSpace($Arguments)) {
        $process = Start-Process -FilePath $ExecutablePath -WorkingDirectory $WorkingDirectory -PassThru
    }
    else {
        $process = Start-Process -FilePath $ExecutablePath -WorkingDirectory $WorkingDirectory -ArgumentList $Arguments -PassThru
    }

    return $process.Id
}

function Wait-TcpPort {
    param(
        [string]$HostName,
        [int]$Port,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $client = New-Object System.Net.Sockets.TcpClient
        try {
            $connect = $client.BeginConnect($HostName, $Port, $null, $null)
            if ($connect.AsyncWaitHandle.WaitOne(500)) {
                $client.EndConnect($connect)
                return $true
            }
        }
        catch {
        }
        finally {
            $client.Close()
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Invoke-UnityServerBuild {
    param(
        [string]$UnityExe,
        [string]$OutputPath
    )

    $unityProcessesForProject = @(Get-UnityProcessesForClientProject)
    if ($unityProcessesForProject.Count -gt 0) {
        Invoke-UnityServerBuildViaOpenEditor -OutputPath $OutputPath
        return
    }

    $logPath = Join-Path $clientPath "Logs\ProjectXServerBuild.log"
    New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
    New-Item -ItemType Directory -Force -Path (Split-Path $OutputPath) | Out-Null

    Write-Host "Unity build log: $logPath"
    $arguments = @(
        "-batchmode",
        "-quit",
        "-nographics",
        "-projectPath", $clientPath,
        "-executeMethod", "ProjectX.Editor.ProjectXDevAutomation.BuildDedicatedServerFromCommandLine",
        "-buildOutputPath", $OutputPath,
        "-logFile", $logPath
    )

    $process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Unity server build failed with exit code $($process.ExitCode). See $logPath"
    }
}

function Invoke-UnityServerBuildViaOpenEditor {
    param([string]$OutputPath)

    $requestDirectory = Join-Path $clientPath "Temp\ProjectXAutomation"
    $requestPath = Join-Path $requestDirectory "build-server.request"
    $statusPath = Join-Path $requestDirectory "build-server.status"
    New-Item -ItemType Directory -Force -Path $requestDirectory | Out-Null
    Remove-Item -Path $statusPath -Force -ErrorAction SilentlyContinue
    Set-Content -Path $requestPath -Value $OutputPath

    Write-Host "Unity Editor is already open. Server build request was queued."
    Write-Host "Waiting for Unity Editor build status..."

    $deadline = (Get-Date).AddMinutes(15)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $statusPath) {
            $status = Get-Content -Raw $statusPath
            if ($status.StartsWith("Succeeded|")) {
                Write-Host "Unity Editor server build succeeded."
                return
            }

            throw "Unity Editor server build failed: $status"
        }

        Start-Sleep -Seconds 1
    }

    throw "Timed out waiting for Unity Editor to build the server. Check Unity Console for compilation/build errors."
}

function Get-UnityProcessesForClientProject {
    try {
        return @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" |
            Where-Object {
                $_.CommandLine -and $_.CommandLine.IndexOf($clientPath, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
            })
    }
    catch {
        return @(Get-Process Unity -ErrorAction SilentlyContinue)
    }
}

function Request-ClientPlay {
    param([string]$UnityExe)

    $requestDirectory = Join-Path $clientPath "Temp\ProjectXAutomation"
    $requestPath = Join-Path $requestDirectory "play-client.request"
    New-Item -ItemType Directory -Force -Path $requestDirectory | Out-Null
    Set-Content -Path $requestPath -Value ([DateTimeOffset]::Now.ToString("O"))

    $unityProcessesForProject = @(Get-UnityProcessesForClientProject)

    if ($unityProcessesForProject.Count -gt 0) {
        Write-Host "Unity Editor is already open for this project. Play request file was queued."
        return
    }

    Write-Host "Opening Unity Editor and entering Play Mode"
    $arguments = @(
        "-projectPath", $clientPath,
        "-executeMethod", "ProjectX.Editor.ProjectXDevAutomation.PlayClientFromBootstrap"
    )

    Start-Process -FilePath $UnityExe -ArgumentList $arguments | Out-Null
}

$unityExe = Resolve-UnityPath $UnityPath
Write-Host "Repo:  $repoRoot"
Write-Host "Unity: $unityExe"

if (-not $SkipApi) {
    $apiAlreadyListening = Wait-TcpPort -HostName "127.0.0.1" -Port $ApiPort -TimeoutSeconds 1

    if ($apiAlreadyListening -and -not $RestartExisting) {
        Write-Step "Using running API"
        Write-Host "API is already listening on port $ApiPort. Skipping API build and startup."
    }
    else {
        Ensure-ApiBuilt

        if ($RestartExisting) {
            Stop-ProcessByExecutablePath -ExecutablePath $ApiExePath -Name "ProjectX API"

            if (Wait-TcpPort -HostName "127.0.0.1" -Port $ApiPort -TimeoutSeconds 1) {
                throw "Port $ApiPort is still in use. Stop the API process that owns it or run without -RestartExisting."
            }
        }

        Write-Step "Starting API"
        $oldAspNetEnvironment = $env:ASPNETCORE_ENVIRONMENT
        $oldDotNetEnvironment = $env:DOTNET_ENVIRONMENT
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:DOTNET_ENVIRONMENT = "Development"
        try {
            Start-Executable -ExecutablePath $ApiExePath -WorkingDirectory (Split-Path $ApiExePath) -Name "ProjectX API" | Out-Null
        }
        finally {
            $env:ASPNETCORE_ENVIRONMENT = $oldAspNetEnvironment
            $env:DOTNET_ENVIRONMENT = $oldDotNetEnvironment
        }
    }

    if (Wait-TcpPort -HostName "127.0.0.1" -Port $ApiPort -TimeoutSeconds 30) {
        Write-Host "API is listening on port $ApiPort"
    }
    else {
        Write-Warning "API did not start listening on port $ApiPort within 30 seconds. Continuing anyway."
    }
}

$serverAlreadyRunning = @(Get-ProcessesByExecutablePath $ServerBuildPath).Count -gt 0
if ($serverAlreadyRunning -and $RestartExisting) {
    Stop-ProcessByExecutablePath -ExecutablePath $ServerBuildPath -Name "ProjectX server"
    $serverAlreadyRunning = $false
}

if ($serverAlreadyRunning) {
    Write-Step "Using running Unity dedicated server"
    Write-Host "ProjectX server is already running. Skipping server build and startup."
}
else {
    if (-not $SkipServerBuild) {
        Write-Step "Building Unity dedicated server"
        Invoke-UnityServerBuild -UnityExe $unityExe -OutputPath $ServerBuildPath
    }

    if (-not $SkipServerRun) {
        if (-not (Test-Path $ServerBuildPath)) {
            throw "Server executable was not found at '$ServerBuildPath'. Run without -SkipServerBuild first."
        }

        Write-Step "Starting Unity dedicated server"
        if ([string]::IsNullOrWhiteSpace($env:PROJECTX_SERVER_USERNAME)) {
            $env:PROJECTX_SERVER_USERNAME = "server1@localhost"
        }

        if ([string]::IsNullOrWhiteSpace($env:PROJECTX_SERVER_PASSWORD)) {
            $env:PROJECTX_SERVER_PASSWORD = "Server1!"
        }

        Start-Executable -ExecutablePath $ServerBuildPath -WorkingDirectory (Split-Path $ServerBuildPath) -Name "ProjectX server" -Arguments "-projectx-direct" | Out-Null
    }
}

if (-not $SkipClientPlay) {
    if ($ClientDelaySeconds -gt 0) {
        Write-Host "Waiting $ClientDelaySeconds second(s) before starting client Play Mode"
        Start-Sleep -Seconds $ClientDelaySeconds
    }

    Write-Step "Starting Unity client Play Mode"
    Request-ClientPlay -UnityExe $unityExe
}

Write-Host ""
Write-Host "ProjectX run finished." -ForegroundColor Green
