<#
.SYNOPSIS
    Builds every C# template shipped in skills/*/templates and runs static checks on them.

.DESCRIPTION
    Each project is copied to tests/.build and restored from nuget.org ONLY, into an isolated
    package folder, so a package that exists only on a private feed or in a local NuGet cache
    fails the test the same way it would for a customer.

    Checks:
      1. Every project builds with zero errors.
      2. The build output contains only the project's own DLLs (no Relativity.* or kCura.*),
         which is the list a developer uploads as resource files.
      3. Static rules: fully qualified Guid attribute, Description/Name attribute, Kepler
         interface attributes, net462 target, no deprecated/private-feed packages, every
         ScriptFileNames entry exists, no em dashes.
      4. The agent builds with the optional Workload Discovery [Path] attribute enabled.

    Requires the .NET SDK (6 or later). Works on Windows, macOS and Linux.

.EXAMPLE
    pwsh ./tests/Test-Templates.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot  = Split-Path -Parent $PSScriptRoot
$buildRoot = Join-Path $PSScriptRoot '.build'
$failures  = [System.Collections.Generic.List[string]]::new()

function Fail([string]$message) { $failures.Add($message); Write-Host "  FAIL: $message" -ForegroundColor Red }
function Pass([string]$message) { Write-Host "  ok:   $message" -ForegroundColor Green }

if (Test-Path $buildRoot) { Remove-Item $buildRoot -Recurse -Force }
New-Item -ItemType Directory -Path $buildRoot | Out-Null

# nuget.org only. The key name is unusual on purpose: a user-level NuGet.Config can disable a
# source called "nuget.org", which would make this test depend on the machine it runs on.
@'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="public-nuget-org-only" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
  <config>
    <add key="globalPackagesFolder" value=".packages" />
  </config>
</configuration>
'@ | Set-Content -Path (Join-Path $buildRoot 'nuget.config') -Encoding utf8

# ---------------------------------------------------------------- static checks
Write-Host "`n== Static checks" -ForegroundColor Cyan
$serviceNames = @{}

$templateFiles = Get-ChildItem -Path (Join-Path $repoRoot 'skills') -Recurse -File |
    Where-Object { $_.FullName -match '[\\/]templates[\\/]' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($file in $templateFiles | Where-Object Extension -eq '.cs') {
    $text = Get-Content $file.FullName -Raw
    $rel  = $file.FullName.Substring($repoRoot.Length + 1)

    if ($text -match '(?m)^\s*\[Guid\(') { Fail "$rel uses a bare [Guid(...)]; use [System.Runtime.InteropServices.Guid(...)]" }

    $isAgent = $text -match ':\s*AgentBase\b'
    $isHandler = $text -match ':\s*(PreSave|PostSave|PreDelete|PreCascadeDelete|PreMassDelete|PreLoad|Console|PageInteraction|ListPageInteraction|PostInstall|PreInstall|PreUninstall)EventHandler\b|:\s*(PostWorkspaceCreate|PreWorkspaceDelete)EventHandlerBase\b|:\s*MassOperationHandler\b'

    if ($isAgent -or $isHandler) {
        if ($text -notmatch '\[System\.Runtime\.InteropServices\.Guid\("') { Fail "$rel is missing [System.Runtime.InteropServices.Guid]" }
    }
    if ($isAgent -and $text -notmatch '\[Name\("') { Fail "$rel agent is missing [Name(...)]" }
    # Verified on a live instance: event handlers register their name from
    # kCura.EventHandler.CustomAttributes.Description, mass operation handlers from
    # System.ComponentModel.Description (the other attribute leaves the name blank).
    $isMassOp = $text -match ':\s*MassOperationHandler\b'
    if ($isHandler -and -not $isMassOp) {
        if ($text -notmatch '\[(kCura\.EventHandler\.CustomAttributes\.)?Description\("') { Fail "$rel handler is missing [kCura.EventHandler.CustomAttributes.Description]" }
        if ($text -match 'using System\.ComponentModel;|\[System\.ComponentModel\.Description') { Fail "$rel uses System.ComponentModel.Description; event handlers need kCura.EventHandler.CustomAttributes.Description" }
    }
    if ($isMassOp -and $text -notmatch '\[System\.ComponentModel\.Description\("') { Fail "$rel mass operation handler needs [System.ComponentModel.Description]" }
    # Relativity.Services.Objects lives in Relativity.Services.Interfaces.dll, which Relativity
    # provides at runtime. The Relativity.ObjectManager.V1 SDK's DLLs are not on Relativity's
    # provided-assemblies list, so code that uses it would need extra uploads.
    if ($text -match 'kCura\.Relativity\.Client') { Fail "$rel uses RSAPI (kCura.Relativity.Client)" }
    if ($text -match 'Relativity\.ObjectManager\.V1') { Fail "$rel uses Relativity.ObjectManager.V1; use Relativity.Services.Objects (platform-provided)" }
    if ($text -match 'Log\w*\(\s*\$"') { Fail "$rel passes an interpolated string to a log method; use a message template" }

    if ($text -match '\[WebService\(') {
        foreach ($attr in 'ServiceAudience', 'RoutePrefix') {
            if ($text -notmatch "\[$attr\(") { Fail "$rel Kepler service is missing [$attr]" }
        }
        if ($text -notmatch 'interface\s+\w+\s*:\s*IDisposable') { Fail "$rel Kepler service interface must inherit IDisposable" }
        if ($text -match '\[(WebService|RoutePrefix)\(""') { Fail "$rel has an empty WebService/RoutePrefix" }
        # Kepler will not host two services with the same interface name in one application, and
        # the failure stops every service in that application. Templates must model specific names.
        foreach ($m in [regex]::Matches($text, 'interface\s+(\w+)\s*:\s*IDisposable')) {
            $iface = $m.Groups[1].Value
            if ($iface -in @('IService', 'IWorkloadDiscoveryService', 'IWorkloadService', 'IMyService')) {
                Fail "$rel uses the generic service interface name $iface"
            }
            if ($serviceNames.ContainsKey($iface)) { Fail "$rel reuses service interface name $iface (also in $($serviceNames[$iface]))" }
            else { $serviceNames[$iface] = $rel }
        }
    }
    if ($text -match '\[RoutePrefix\("[^"]*[Rr]elativity') { Fail "$rel RoutePrefix contains 'Relativity'" }

    # Every ScriptFileNames entry must ship next to the handler.
    foreach ($m in [regex]::Matches($text, 'ScriptFileNames\s*=>\s*new\[\]\s*\{([^}]*)\}')) {
        foreach ($name in [regex]::Matches($m.Groups[1].Value, '"([^"]+)"')) {
            if (-not (Test-Path (Join-Path $file.DirectoryName $name.Groups[1].Value))) {
                Fail "$rel names script file '$($name.Groups[1].Value)' that is not in the templates folder"
            }
        }
    }
}

foreach ($proj in $templateFiles | Where-Object Extension -eq '.csproj') {
    $text = Get-Content $proj.FullName -Raw
    $rel  = $proj.FullName.Substring($repoRoot.Length + 1)
    if ($text -notmatch '<TargetFramework>net462</TargetFramework>') { Fail "$rel must target net462" }
    if ($text -match 'Include="Relativity\.Services\.Interfaces"|Include="Relativity\.ObjectManager(\.SDK)?"') { Fail "$rel references a private-feed, deprecated, or non-platform-provided package" }
}

foreach ($file in $templateFiles | Where-Object Extension -eq '.js') {
    $text = Get-Content $file.FullName -Raw
    $rel  = $file.FullName.Substring($repoRoot.Length + 1)
    # Relativity Forms scripts must be ES5.1.
    if ($text -match '=>|`|(?m)^\s*(let|const)\s') { Fail "$rel uses ES6+ syntax; Relativity Forms scripts must be ES5.1" }
    if ($text -notmatch '\(function\s*\(\s*eventNames\s*,\s*convenienceApi\s*\)') { Fail "$rel is not an IIFE taking (eventNames, convenienceApi)" }
}

if ($failures.Count -eq 0) { Pass 'all static rules' }

# ---------------------------------------------------------------- builds
function Invoke-Build([string]$name, [string]$sourceDir, [string[]]$projects, [scriptblock]$prepare, [string[]]$expectedDlls) {
    Write-Host "`n== Build: $name" -ForegroundColor Cyan
    $dest = Join-Path $buildRoot $name
    Copy-Item -Path $sourceDir -Destination $dest -Recurse
    Get-ChildItem $dest -Recurse -Directory -Include bin, obj | Remove-Item -Recurse -Force
    if ($prepare) { & $prepare $dest }

    foreach ($p in $projects) {
        $projPath = Join-Path $dest $p
        $output = & dotnet build $projPath -c Release --nologo -v q 2>&1
        if ($LASTEXITCODE -ne 0) {
            Fail "$name/$p failed to build"
            $output | Where-Object { $_ -match 'error' } | Select-Object -First 15 | ForEach-Object { Write-Host "    $_" }
            continue
        }
        $warnings = $output | Where-Object { $_ -match 'warning (CS|NU)\d+' } | Sort-Object -Unique
        if ($warnings) { $warnings | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow } }
        # Compiler warnings (obsolete APIs, nullable, unused) are failures: customers copy this code.
        if ($warnings | Where-Object { $_ -match 'warning CS\d+' }) { Fail "$name/$p has compiler warnings"; continue }
        Pass "$name/$p builds"
    }

    $dlls = Get-ChildItem $dest -Recurse -Filter *.dll |
        Where-Object { $_.FullName -match '[\\/]bin[\\/]Release[\\/]' -and $_.FullName -notmatch 'TestClient' } |
        Select-Object -ExpandProperty Name -Unique | Sort-Object
    $platform = $dlls | Where-Object { $_ -match '^(Relativity|kCura)\.' }
    if ($platform) { Fail "$name output includes platform DLLs: $($platform -join ', ')" }
    foreach ($dll in $expectedDlls) { if ($dlls -notcontains $dll) { Fail "$name output is missing $dll" } }
    $unexpected = $dlls | Where-Object { $expectedDlls -notcontains $_ }
    if ($unexpected) { Fail "$name output has unexpected DLLs to upload: $($unexpected -join ', ')" }
    else { Pass "$name output to upload: $($dlls -join ', ')" }
}

$skills = Join-Path $repoRoot 'skills'

Invoke-Build 'agent' (Join-Path $skills 'agent-builder/templates') @('Agents/MyCompany.MyApp.Agents.csproj') $null @('MyCompany.MyApp.Agents.dll')

Invoke-Build 'agent-workload-discovery' (Join-Path $skills 'agent-builder/templates') @(
    'Agents/MyCompany.MyApp.Agents.csproj',
    'WorkloadDiscovery/Services/MyCompany.MyApp.Services.csproj'
) {
    param($dest)
    $agent = Join-Path $dest 'Agents/SampleAgent.cs'
    (Get-Content $agent -Raw) -replace '//\[WorkloadDiscovery', '[WorkloadDiscovery' | Set-Content $agent
    $proj = Join-Path $dest 'Agents/MyCompany.MyApp.Agents.csproj'
    (Get-Content $proj -Raw) -replace '(<PackageReference Include="Relativity.Agent"[^>]*/>)',
        "`$1`n    <PackageReference Include=`"Relativity.Platform.Agent.WorkloadDiscovery.SDK`" Version=`"0.4.1`" />" | Set-Content $proj
} @('MyCompany.MyApp.Agents.dll', 'MyCompany.MyApp.Interfaces.dll', 'MyCompany.MyApp.Services.dll', 'WorkloadDiscovery.dll')

Invoke-Build 'event-handlers' (Join-Path $skills 'event-handler-builder/templates') @('MyCompany.MyApp.EventHandlers.csproj') $null @('MyCompany.MyApp.EventHandlers.dll')

Invoke-Build 'kepler' (Join-Path $skills 'kepler-service-builder/templates') @('MyCompany.MyApp.sln') $null @('MyCompany.MyApp.Interfaces.dll', 'MyCompany.MyApp.Services.dll')

# ---------------------------------------------------------------- result
Write-Host ''
if ($failures.Count -gt 0) {
    Write-Host "$($failures.Count) failure(s):" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
Write-Host 'All template checks passed.' -ForegroundColor Green
