<# 
Fix-MSB3030-Here.ps1  (PowerShell 5.1 compatible)
Run from a project folder. Auto-detects the .csproj/.sln in the current directory, builds once,
parses MSB3030 errors, and fixes them by copying real files from your NuGet cache—or creating a
placeholder for HybridWebView.js as a last resort.

Usage (from project folder):
  powershell -ExecutionPolicy Bypass -File .\Fix-MSB3030-Here.ps1
Optional:
  powershell -ExecutionPolicy Bypass -File .\Fix-MSB3030-Here.ps1 -Project .\MyProj.csproj -Solution .\MyProj.sln
#>

param(
  [string]$Project,
  [string]$Solution
)

$ErrorActionPreference = 'Stop'

function Resolve-InCurrentDir {
  param(
    [string]$Pattern,
    [string]$Kind # "project" or "solution"
  )
  $items = Get-ChildItem -LiteralPath . -Filter $Pattern -File | Sort-Object Name
  if ($items.Count -eq 1) { return $items[0].FullName }

  if ($items.Count -gt 1) {
    $folder = Split-Path -Leaf (Get-Location)
    $preferred = $items | Where-Object { $_.BaseName -ieq $folder -or $_.BaseName -like "$folder*" } | Select-Object -First 1
    if ($preferred) { return $preferred.FullName }

    Write-Host "Found multiple $Kind files in $(Get-Location):"
    $items | ForEach-Object { Write-Host " - $($_.Name)" }
    if ($Kind -eq 'project') {
      throw "Multiple project files found. Re-run with -Project <path>."
    } else {
      throw "Multiple solution files found. Re-run with -Solution <path>."
    }
  }
  return $null
}

function Get-Msb3030Paths {
  param([string[]]$Lines)
  $out = @()
  foreach ($line in $Lines) {
    if ($line -match 'MSB3030') {
      $m = [regex]::Match($line, '"([^"]+)"')
      if ($m.Success) { $out += $m.Groups[1].Value }
    }
  }
  $out | Sort-Object -Unique
}

function Find-AnyWebView2Loader {
  param([string]$Rid) # win-x64 / win-x86 / win-arm64
  $nuget = Join-Path $env:USERPROFILE ".nuget\packages"
  $root = Join-Path $nuget "microsoft.web.webview2"
  $roots = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending
  foreach ($r in $roots) {
    $p = Join-Path $r.FullName ("runtimes\{0}\native\WebView2Loader.dll" -f $Rid)
    if (Test-Path $p) { return $p }
  }
  # fallback: any WebView2Loader.dll in NuGet cache for that RID
  $fallback = Get-ChildItem -Path $nuget -Recurse -Filter "WebView2Loader.dll" -ErrorAction SilentlyContinue |
              Where-Object { $_.FullName -match "\\runtimes\\$Rid\\native\\WebView2Loader\.dll$" } |
              Select-Object -First 1
  if ($fallback) { return $fallback.FullName }

  # last resort: any WebView2Loader.dll at all
  $any = Get-ChildItem -Path $nuget -Recurse -Filter "WebView2Loader.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($any) { return $any.FullName }
  return $null
}

function Find-HybridWebViewJs {
  $nuget = Join-Path $env:USERPROFILE ".nuget\packages"
  $controlsRoots = Get-ChildItem -Path (Join-Path $nuget "microsoft.maui.controls*") -Directory -ErrorAction SilentlyContinue
  foreach ($r in $controlsRoots) {
    $hit = Get-ChildItem -Path $r.FullName -Recurse -Filter "HybridWebView.js" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($hit) { return $hit.FullName }
  }
  $packs = Join-Path ${env:ProgramFiles} "dotnet\packs\Microsoft.Maui.Sdk"
  if (Test-Path $packs) {
    $hit2 = Get-ChildItem -Path $packs -Recurse -Filter "HybridWebView.js" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($hit2) { return $hit2.FullName }
  }
  $hit3 = Get-ChildItem -Path $nuget -Recurse -Filter "HybridWebView.js" -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($hit3) { return $hit3.FullName }
  return $null
}

function Ensure-Placeholder {
  param([string]$DestPath, [string]$Content)
  $dir = Split-Path $DestPath -Parent
  if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
  Set-Content -LiteralPath $DestPath -Value $Content -Encoding UTF8
}

function Run-BuildAndCapture {
  param([string]$Project, [string]$Solution)
  $projDir = $null
  if ($Project) { $projDir = Split-Path $Project -Parent }
  if ($projDir) {
    Remove-Item "$projDir\bin","$projDir\obj" -Recurse -Force -ErrorAction SilentlyContinue
  }
  if ($Solution) { dotnet restore --nologo $Solution | Out-Null }
  elseif ($Project) { dotnet restore --nologo $Project | Out-Null }

  $log = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), ("msbuild-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date)))
  if ($Project) {
    dotnet build --nologo --no-incremental $Project 2>&1 | Tee-Object -FilePath $log | Out-Null
  } elseif ($Solution) {
    dotnet build --nologo --no-incremental $Solution 2>&1 | Tee-Object -FilePath $log | Out-Null
  } else {
    throw "Nothing to build. Provide a project or solution."
  }
  return $log
}

# ---------- autodetect if not provided ----------
if (-not $Project) {
  $Project = Resolve-InCurrentDir -Pattern "*.csproj" -Kind "project"
  if (-not $Project) { throw "No .csproj found in $(Get-Location)." }
}
if (-not $Solution) {
  $Solution = Resolve-InCurrentDir -Pattern "*.sln" -Kind "solution"
  # Solution optional
}

Write-Host "Using project: $Project"
if ($Solution) { Write-Host "Using solution: $Solution" }

# ---------- build and parse ----------
$logPath = Run-BuildAndCapture -Project $Project -Solution $Solution
$lines = Get-Content -LiteralPath $logPath
$targets = Get-Msb3030Paths -Lines $lines

if (-not $targets -or $targets.Count -eq 0) {
  Write-Host "No MSB3030 file-not-found errors detected in $logPath."
  exit 0
}

Write-Host "Found $($targets.Count) missing file(s). Fixing…"

$copied = 0
$failed = 0
$report = @()

foreach ($dest in $targets) {
  $leaf = Split-Path $dest -Leaf
  $src = $null

  if ($leaf -ieq 'WebView2Loader.dll') {
    # choose RID from dest path
    $rid = 'win-x64'
    $m = [regex]::Match($dest, 'runtimes\\(win-(x86|x64|arm64))\\native', 'IgnoreCase')
    if ($m.Success) { $rid = $m.Groups[1].Value }
    $src = Find-AnyWebView2Loader -Rid $rid
  } elseif ($leaf -ieq 'HybridWebView.js') {
    $src = Find-HybridWebViewJs
    if (-not $src) {
      # create a placeholder to satisfy the copy step
      Ensure-Placeholder -DestPath $dest -Content "// placeholder HybridWebView.js to satisfy MSBuild copy; review package versions."
      $report += New-Object psobject -Property @{ Status="PLACEHOLDER"; Target=$dest; Source="(generated)" }
      continue
    }
  } else {
    # generic fallback (rare)
    $nuget = Join-Path $env:USERPROFILE ".nuget\packages"
    $hit = Get-ChildItem -Path $nuget -Recurse -Filter $leaf -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($hit) { $src = $hit.FullName }
  }

  if (-not $src -or !(Test-Path $src)) {
    $failed++
    $report += New-Object psobject -Property @{ Status="NOT FOUND"; Target=$dest; Source=$null }
    continue
  }

  $destDir = Split-Path $dest -Parent
  if (!(Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }

  try {
    Copy-Item -LiteralPath $src -Destination $dest -Force
    $copied++
    $report += New-Object psobject -Property @{ Status="COPIED"; Target=$dest; Source=$src }
  }
  catch {
    $failed++
    $report += New-Object psobject -Property @{ Status="COPY FAILED"; Target=$dest; Source=$src; Error=$_.Exception.Message }
  }
}

Write-Host ""
Write-Host "Done. Copied: $copied  Placeholders: $($report | Where-Object {$_.Status -eq 'PLACEHOLDER'} | Measure-Object | Select-Object -ExpandProperty Count)  Failed: $failed"
$report | Format-Table -AutoSize

# optional: re-run build once to confirm
Write-Host ""
Write-Host "Rebuilding to confirm…"
$log2 = Run-BuildAndCapture -Project $Project -Solution $Solution
$lines2 = Get-Content -LiteralPath $log2
$leftovers = Get-Msb3030Paths -Lines $lines2
if ($leftovers.Count -gt 0) {
  Write-Host "Still missing $($leftovers.Count) file(s). Check the table above."
  exit 1
} else {
  Write-Host "No MSB3030 errors after fix."
  exit 0
}
