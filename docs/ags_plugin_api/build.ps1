#Requires -Version 7
<#
.SYNOPSIS
    Generates the AGS plugin API documentation site.
.DESCRIPTION
    Runs Doxygen over Engine/plugin/agsplugin.h, then DocFX over the editor's
    built .NET assemblies, producing a self-contained static site in _site/.
    Doxygen must run first: docfx build wipes its output directory, and the
    Doxygen subsite is copied in as a DocFX resource.
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$Serve,
    [switch]$SkipDoxygen
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$DocRoot  = $PSScriptRoot
$RepoRoot = (Resolve-Path (Join-Path $DocRoot '..' '..')).Path

Push-Location $DocRoot
try {
    # ---------- Stage 1: Doxygen (C++ / agsplugin.h) ----------
    if (-not $SkipDoxygen) {
        if (-not (Get-Command doxygen -ErrorAction SilentlyContinue)) {
            throw "doxygen not found on PATH. Install it from https://www.doxygen.nl/download.html " +
                  "or via 'winget install DimitriVanHeesch.Doxygen', then reopen your shell."
        }

        $engineOut = Join-Path $DocRoot 'engine'
        if (Test-Path $engineOut) { Remove-Item $engineOut -Recurse -Force }

        Write-Host '==> Running Doxygen over Engine/plugin/agsplugin.h' -ForegroundColor Cyan
        doxygen Doxyfile
        if ($LASTEXITCODE -ne 0) { throw "doxygen exited with code $LASTEXITCODE" }
    }

    # ---------- Stage 2: locate the editor's built assemblies ----------
    $assemblies = @(
        @{ Name = 'AGS.Types';             Project = 'Editor/AGS.Types' }
        @{ Name = 'AGS.Controls';          Project = 'Editor/AGS.Controls' }
        @{ Name = 'AGS.CScript.Compiler';  Project = 'Editor/AGS.CScript.Compiler' }
        @{ Name = 'AGS.Native';            Project = 'Editor/AGS.Native' }
    )

    # Resolve everything before touching the staging directory, so a run that
    # cannot find any assemblies fails without destroying the previous run's
    # inputs. verify.ps1 deliberately invokes this script with a bogus
    # configuration to exercise that failure path.
    $resolved = @()
    foreach ($asm in $assemblies) {
        $candidates = @(
            Join-Path $RepoRoot "Solutions/.build/$Configuration/$($asm.Name).dll"
            Join-Path $RepoRoot "$($asm.Project)/bin/$Configuration/$($asm.Name).dll"
        )
        $dll = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

        if (-not $dll) {
            Write-Warning "$($asm.Name).dll not found for configuration '$Configuration'. That section will be missing from the site. Looked in:`n    $($candidates -join "`n    ")"
            continue
        }
        $resolved += $dll
    }

    if ($resolved.Count -eq 0) {
        throw "No editor assemblies found for configuration '$Configuration'. " +
              "Build the editor first, e.g.: msbuild Solutions/AGS.Editor.Full.sln /p:Configuration=$Configuration /p:Platform=Win32"
    }

    $staging = Join-Path $DocRoot '_assemblies'
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
    New-Item -ItemType Directory -Path $staging | Out-Null

    foreach ($dll in $resolved) {
        Copy-Item $dll $staging
        $xml = [System.IO.Path]::ChangeExtension($dll, '.XML')
        if (Test-Path $xml) {
            Copy-Item $xml $staging
        } else {
            Write-Warning "$([System.IO.Path]::GetFileName($dll)) has no sibling .XML. Its members will render without descriptions."
        }
    }

    # Stale yml from an earlier run would otherwise survive and be published
    # even when this run staged different assemblies.
    $apiOut = Join-Path $DocRoot 'api'
    if (Test-Path $apiOut) { Remove-Item $apiOut -Recurse -Force }

    # ---------- Stage 3: DocFX (C#) ----------
    Write-Host '==> Restoring DocFX' -ForegroundColor Cyan
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed with code $LASTEXITCODE" }

    # Metadata and build run as separate phases so the API landing page can be
    # placed between them: `docfx metadata` populates api/ with generated yml,
    # and api/ is gitignored, so the tracked source of that page is api-index.md.
    Write-Host '==> Running DocFX (metadata)' -ForegroundColor Cyan
    dotnet docfx metadata docfx.json
    if ($LASTEXITCODE -ne 0) { throw "docfx metadata exited with code $LASTEXITCODE" }

    Copy-Item (Join-Path $DocRoot 'api-index.md') (Join-Path $DocRoot 'api/index.md') -Force

    # docfx build does not clean its output directory: pages dropped from the
    # metadata (by filterConfig.yml, or by an assembly no longer being staged)
    # would otherwise linger in _site indefinitely.
    $siteOut = Join-Path $DocRoot '_site'
    if (Test-Path $siteOut) { Remove-Item $siteOut -Recurse -Force }

    Write-Host '==> Running DocFX (build)' -ForegroundColor Cyan
    if ($Serve) {
        dotnet docfx build docfx.json --serve
    } else {
        dotnet docfx build docfx.json
    }
    if ($LASTEXITCODE -ne 0) { throw "docfx build exited with code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

Write-Host "`nBuild complete." -ForegroundColor Green
