#Requires -Version 7
[CmdletBinding()]
param(
    [string]$SitePath = (Join-Path $PSScriptRoot '_site')
)

$ErrorActionPreference = 'Stop'
$failures = @()

function Assert-FileExists {
    param([string]$Path, [string]$Because)
    if (-not (Test-Path -LiteralPath $Path)) {
        $script:failures += "MISSING FILE: $Path ($Because)"
        return $false
    }
    return $true
}

function Assert-FileContains {
    param([string]$Path, [string]$Pattern, [string]$Because)
    if (-not (Assert-FileExists -Path $Path -Because $Because)) { return }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch [regex]::Escape($Pattern)) {
        $script:failures += "MISSING TEXT in $($Path): '$Pattern' ($Because)"
    }
}

function Assert-FileMatches {
    param([string]$Path, [string]$Pattern, [string]$Because)
    if (-not (Assert-FileExists -Path $Path -Because $Because)) { return }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch $Pattern) {
        $script:failures += "PATTERN NOT FOUND in $($Path): '$Pattern' ($Because)"
    }
}

# Asserts that a struct field's Doxygen summary-table description row is the
# one immediately following that field's own name cell — i.e. that a
# trailing `/**<` placed *before* a declaration (which Doxygen attaches to
# the *previous* member) has not shifted the description onto the wrong
# field. Doxygen renders an undocumented-detail field as `<b>NAME</b>` and a
# field with an @see/detailed section as `<a ...>NAME</a>`; array fields get
# a trailing `[N]` after the closing tag.
function Assert-StructFieldDoc {
    param([string]$Path, [string]$Field, [string]$Description, [string]$Because)
    $pattern = '(?:<b>|<a[^>]*>)' + [regex]::Escape($Field) + '(?:</b>|</a>)(?:\s*\[[^\]]*\])?</td></tr>\s*' +
               '<tr class="memdesc:[^"]*"><td class="mdescLeft">[^<]*</td><td class="mdescRight">' +
               [regex]::Escape($Description)
    Assert-FileMatches -Path $Path -Pattern $pattern -Because $Because
}

# --- Engine (Doxygen) subsite ---
$engine = Join-Path $SitePath 'engine'
$null = Assert-FileExists -Path (Join-Path $engine 'index.html') -Because 'Doxygen must emit a landing page'
Assert-FileContains -Path (Join-Path $engine 'annotated.html') -Pattern 'IAGSEngine' -Because 'the run-time interface must be listed'

# Doxygen owns the engine plugin narrative: its main page is the overview,
# and the version reference is an extra page beside it.
Assert-FileContains -Path (Join-Path $engine 'index.html') -Pattern 'AGS_EngineStartup' -Because 'the Doxygen main page must be the engine plugin overview'
Assert-FileContains -Path (Join-Path $engine 'index.html') -Pattern 'THIS_IS_THE_PLUGIN' -Because 'the overview must explain how to get the exported declarations'
$versionPage = Get-ChildItem -LiteralPath $engine -Filter '*engine_plugin_versions*.html' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $versionPage) {
    $failures += "MISSING PAGE: no engine_plugin_versions page in $engine (check USE_MDFILE_AS_MAINPAGE and the @page label)"
} else {
    Assert-FileContains -Path $versionPage.FullName -Pattern 'PLUGIN_API_VERSION' -Because 'the version reference must name where the current version lives'
    Assert-FileContains -Path $versionPage.FullName -Pattern '3.6.2.5' -Because 'the interface-to-engine version mapping must be present'
}
Assert-FileContains -Path (Join-Path $engine 'annotated.html') -Pattern 'IAGSStream' -Because 'the stream interface must be listed'
Assert-FileContains -Path (Join-Path $engine 'annotated.html') -Pattern 'AGSCharacter' -Because 'plain data structs must be listed'

# --- Landing pages ---
Assert-FileContains -Path (Join-Path $SitePath 'index.html') -Pattern 'Engine plugins' -Because 'the landing page must introduce both plugin models'
Assert-FileContains -Path (Join-Path $SitePath 'index.html') -Pattern 'Editor plugins' -Because 'the landing page must introduce both plugin models'
Assert-FileContains -Path (Join-Path $SitePath 'index.html') -Pattern 'two different interfaces named' -Because 'the IAGSEditor name collision must be called out'
Assert-FileContains -Path (Join-Path $SitePath 'editor-plugins.html') -Pattern 'IAGSEditorPlugin' -Because 'the managed entry point must be named'

# --- C# (DocFX) reference ---
$api = Join-Path $SitePath 'api'
Assert-FileContains -Path (Join-Path $api 'AGS.Types.IAGSEditor.html') -Pattern 'Adds a new component to the editor' -Because 'existing /// prose must reach the site'
$null = Assert-FileExists   -Path (Join-Path $api 'AGS.Types.IAGSEditorPlugin.html') -Because 'the plugin entry interface must be documented'
$null = Assert-FileExists   -Path (Join-Path $api 'AGS.Types.IEditorComponent.html') -Because 'the component interface must be documented'
Assert-FileContains -Path (Join-Path $api 'index.html') -Pattern 'Editor Plugin API Reference' -Because 'the API reference needs its own landing page'

# toc.yml must mount api/toc.yml, not link api/index.md as a leaf, or the
# namespace and type tree never appears in the nav.
Assert-FileContains -Path (Join-Path $api 'toc.html') -Pattern 'AGS.Types' -Because 'the generated C# toc must list the namespaces'
Assert-FileContains -Path (Join-Path $SitePath 'toc.json') -Pattern '"includedFrom":"~/api/toc.yml"' -Because 'the root toc must mount the generated C# toc as a subtree'
Assert-FileContains -Path (Join-Path $SitePath 'toc.json') -Pattern '"name":"IAGSEditor"' -Because 'the mounted subtree must expand to individual types, not just namespaces'

# AGS.Controls vendors ScintillaNET and AddressBarExt into its own assembly.
# filterConfig.yml must keep them out; without it they outnumber the real API.
$foreign = Get-ChildItem -LiteralPath $api -Filter *.html -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch '^(AGS|index|toc)' }
if ($foreign) {
    $failures += "NON-AGS PAGES in api/: $($foreign.Count) file(s), e.g. $($foreign[0].Name). Check filterConfig.yml."
}

# --- Failure paths ---
$buildScript = Join-Path $PSScriptRoot 'build.ps1'

# A configuration with no built assemblies must fail with an actionable message.
$bogus = & pwsh -NoProfile -File $buildScript -Configuration 'NoSuchConfiguration' -SkipDoxygen 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    $failures += "build.ps1 succeeded with configuration 'NoSuchConfiguration'; it must fail when no assemblies are found"
}
if ($bogus -notmatch 'Build the editor first') {
    $failures += "build.ps1 failed for a missing configuration without telling the user to build the editor first"
}

# --- Internal link check ---
# Full-site scan hits DocFX template links (e.g. api/AGS.html); scan authored pages only.
$authorPages = @('index.html', 'editor-plugins.html')
$siteFiles = $authorPages | ForEach-Object {
    Get-Item -LiteralPath (Join-Path $SitePath $_) -ErrorAction SilentlyContinue
}
foreach ($file in $siteFiles) {
    $html = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($m in [regex]::Matches($html, 'href="([^"#?]+)')) {
        $target = $m.Groups[1].Value
        if ($target -match '^(https?:|mailto:|javascript:|/)') { continue }
        $resolved = Join-Path $file.DirectoryName $target
        if ($target.EndsWith('/')) {
            $dir = $resolved
            $resolved = Join-Path $dir 'index.html'
            if (-not (Test-Path -LiteralPath $resolved)) {
                $resolved = Join-Path $dir 'toc.html'
            }
        }
        if (-not (Test-Path -LiteralPath $resolved)) {
            $failures += "BROKEN LINK in $($file.Name): '$target'"
        }
    }
}

# --- agsplugin.h port: versions 1-5 ---
$engineClass = Join-Path (Join-Path $SitePath 'engine') 'class_i_a_g_s_engine.html'
Assert-FileContains -Path $engineClass -Pattern 'Interface version 1' -Because 'version 1 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'Interface version 5' -Because 'version 5 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'parser dictionary' -Because 'LookupParserWord must have ported prose'

# --- agsplugin.h port: versions 6-9 ---
Assert-FileContains -Path $engineClass -Pattern 'Interface version 6' -Because 'version 6 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'Interface version 9' -Because 'version 9 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'walk-behind' -Because 'GetWalkbehindBaseline must have ported prose'

# --- agsplugin.h port: versions 10-13 ---
Assert-FileContains -Path $engineClass -Pattern 'Interface version 10' -Because 'version 10 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'Interface version 13' -Because 'version 13 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'alpha channel' -Because 'IsSpriteAlphaBlended must have ported prose'

# --- agsplugin.h port: versions 14-16 ---
Assert-FileContains -Path $engineClass -Pattern 'Interface version 14' -Because 'version 14 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'Interface version 16' -Because 'version 16 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'CanRunScriptFunctionNow' -Because 'the script-timing caveat must be cross-referenced'

# --- agsplugin.h port: versions 17-23 ---
Assert-FileContains -Path $engineClass -Pattern 'Interface version 17' -Because 'version 17 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'Interface version 23' -Because 'version 23 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'reference count' -Because 'the ref-count methods must have ported prose'

# --- agsplugin.h port: versions 25-30 ---
Assert-FileContains -Path $engineClass -Pattern 'Interface version 25' -Because 'version 25 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'Interface version 30' -Because 'version 30 methods must carry @since'
Assert-FileContains -Path $engineClass -Pattern 'must fill' -Because 'the mandatory Version field caveat must survive the port'

# --- agsplugin.h port: editor interface, exports, structs ---
$editorClass = Join-Path (Join-Path $SitePath 'engine') 'class_i_a_g_s_editor.html'
Assert-FileContains -Path $editorClass -Pattern 'design time' -Because 'the C++ IAGSEditor must say when it is used'
Assert-FileContains -Path $editorClass -Pattern 'AGS.Types.IAGSEditor' -Because 'the name collision must be disambiguated in the header itself'
Assert-FileContains -Path (Join-Path (Join-Path $SitePath 'engine') 'struct_a_g_s_character.html') -Pattern 'character' -Because 'data structs must be documented'

# --- agsplugin.h port: callback interfaces and constants ---
Assert-FileContains -Path (Join-Path (Join-Path $SitePath 'engine') 'class_i_a_g_s_stream.html') -Pattern 'caller owns the returned stream' -Because 'stream ownership must be documented'
Assert-FileContains -Path (Join-Path (Join-Path $SitePath 'engine') 'class_i_a_g_s_font_renderer.html') -Pattern 'The engine calls these methods' -Because 'the font renderer callback must be documented'
Assert-FileContains -Path (Join-Path (Join-Path $SitePath 'engine') 'class_i_a_g_s_script_managed_object.html') -Pattern 'serializes the managed object' -Because 'the managed object callback must document serialization'
Assert-FileContains -Path (Join-Path (Join-Path $SitePath 'engine') 'class_i_a_g_s_font_renderer2.html') -Pattern 'GetVersion' -Because 'documented private font-renderer callbacks must render'
$engineHeader = Join-Path (Join-Path $SitePath 'engine') 'agsplugin_8h.html'
Assert-FileContains -Path $engineHeader -Pattern 'Room area mask types' -Because 'room mask constants must render as a member group'
Assert-FileContains -Path $engineHeader -Pattern 'File stream open modes' -Because 'file stream constants must render as a member group'
Assert-FileContains -Path $engineHeader -Pattern 'File stream access modes' -Because 'stream access constants must render as a member group'
Assert-FileContains -Path $engineHeader -Pattern 'Log message levels' -Because 'log constants must render as a member group'

# --- agsplugin.h port: struct field-to-description attachment ---
# Regression guard for the `/**<` leading-marker bug: each assertion pins one
# field's name to prose that belongs to it and only it, so a member shifted
# onto its neighbour (or a duplicated trailing copy silently reattached)
# fails the build instead of shipping wrong documentation.
$engineDir = Join-Path $SitePath 'engine'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_color.html') -Field 'g' -Description 'Green component.' -Because 'AGSColor.g must not carry AGSColor.r or AGSColor.b prose'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_game_options.html') -Field 'fast_forward' -Description 'Whether the player is skipping a cutscene.' -Because 'AGSGameOptions.fast_forward must carry its own description'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_character.html') -Field 'walking' -Description 'Walking state.' -Because 'AGSCharacter.walking must carry its own description'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_object.html') -Field 'moving' -Description 'Movement state.' -Because 'AGSObject.moving must carry its own description'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_view_frame.html') -Field 'sound' -Description 'Sound played when this frame is reached.' -Because 'AGSViewFrame.sound must carry its own description'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_mouse_cursor.html') -Field 'hoty' -Description 'Vertical hotspot coordinate.' -Because 'AGSMouseCursor.hoty must not carry AGSMouseCursor.hotx prose'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_render_matrixes.html') -Field 'ViewMatrix' -Description 'View transformation matrix, with 16 floating-point values.' -Because 'AGSRenderMatrixes.ViewMatrix must not carry WorldMatrix or ProjMatrix prose'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_render_stage_desc.html') -Field 'Matrixes' -Description 'World, view, and projection matrices for 3D rendering.' -Because 'AGSRenderStageDesc.Matrixes must not carry Version prose'
Assert-StructFieldDoc -Path (Join-Path $engineDir 'struct_a_g_s_game_info.html') -Field 'Guid' -Description 'Unique string identifying the game.' -Because 'AGSGameInfo.Guid must not carry GameName or UniqueId prose'

# The manual caveat for fast_forward must survive as detailed (non-brief)
# text, not just the one-line summary checked above.
Assert-FileContains -Path (Join-Path $engineDir 'struct_a_g_s_game_options.html') -Pattern 'must still run while this is set' -Because 'the fast_forward cutscene-skip-desync caveat must be documented'

if ($failures.Count -gt 0) {
    Write-Host "`nverify.ps1 FAILED with $($failures.Count) problem(s):`n" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "`nverify.ps1 PASSED" -ForegroundColor Green
exit 0
