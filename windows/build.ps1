$ErrorActionPreference = 'Stop'

$windowsFolder = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputFolder = Join-Path $windowsFolder 'dist'
$compilerCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $compiler) {
    throw 'The Windows C# compiler was not found.'
}

New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null

& $compiler `
    /nologo `
    /target:winexe `
    /optimize+ `
    /platform:anycpu `
    "/win32manifest:$windowsFolder\app.manifest" `
    "/out:$outputFolder\FluxPointer.exe" `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "$windowsFolder\FluxPointer.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Flux Pointer build failed with exit code $LASTEXITCODE."
}

Write-Host "Built $outputFolder\FluxPointer.exe"
