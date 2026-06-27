param(
    [string]$Version,
    [switch]$NoIncrement
)

$ErrorActionPreference = "Stop"

$csprojPath = Join-Path $PSScriptRoot "Resizer3.csproj"
$content = Get-Content $csprojPath -Raw

if ($Version) {
    if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw "Version must be x.y.z.w"
    }
    $newVersion = $Version
}
elseif ($content -match '<AssemblyVersion>(\d+)\.(\d+)\.(\d+)\.(\d+)</AssemblyVersion>') {
    if ($NoIncrement) {
        $newVersion = "$($matches[1]).$($matches[2]).$($matches[3]).$($matches[4])"
    }
    else {
        $newVersion = "$($matches[1]).$($matches[2]).$($matches[3]).$([int]$matches[4] + 1)"
    }
}
else {
    throw "AssemblyVersion not found in $csprojPath"
}

$content = $content -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$newVersion</AssemblyVersion>"
$content = $content -replace '<FileVersion>.*?</FileVersion>', "<FileVersion>$newVersion</FileVersion>"
Set-Content $csprojPath -Value $content -NoNewline

dotnet clean $csprojPath -c Release
dotnet publish $csprojPath -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=false

$publishExe = Join-Path $PSScriptRoot "bin\Release\net10.0-windows7.0\win-x64\publish\Resizer3.exe"
$artifactDir = Join-Path $PSScriptRoot "artifacts"
$artifactExe = Join-Path $artifactDir "Resizer3.exe"
$artifactSha = Join-Path $artifactDir "Resizer3.exe.sha256"

New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
Copy-Item $publishExe $artifactExe -Force

$hash = (Get-FileHash $artifactExe -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  Resizer3.exe" | Set-Content $artifactSha

Write-Host "Published $artifactExe ($newVersion)" -ForegroundColor Green
