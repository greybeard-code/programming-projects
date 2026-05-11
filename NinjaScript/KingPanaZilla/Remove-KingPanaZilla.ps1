#Requires -Version 5.1
<#
.SYNOPSIS
    Removes KingPanaZilla suite files from NinjaTrader 8's bin\Custom folder.

.DESCRIPTION
    Searches recursively under Documents\NinjaTrader 8\bin\Custom for every file
    in the KingPanaZilla suite and deletes each one found.  A dry-run mode
    (-WhatIf) lists what WOULD be deleted without touching anything.

.PARAMETER CustomFolder
    Override the default bin\Custom path.  Useful if NinjaTrader is installed
    in a non-standard location or on a different drive.

.PARAMETER WhatIf
    List matching files without deleting them.

.EXAMPLE
    # Preview — see what would be deleted
    .\Remove-KingPanaZilla.ps1 -WhatIf

.EXAMPLE
    # Actually delete
    .\Remove-KingPanaZilla.ps1

.EXAMPLE
    # Custom install path
    .\Remove-KingPanaZilla.ps1 -CustomFolder "D:\NinjaTrader 8\bin\Custom"
#>

[CmdletBinding(SupportsShouldProcess)]
param (
    [string] $CustomFolder = (Join-Path $env:USERPROFILE "Documents\NinjaTrader 8\bin\Custom")
)

# ── Files to remove ────────────────────────────────────────────────────────────
$TargetFiles = @(
    'gbBarStatus.cs',
    'gbKingOrderBlock.cs',
    'gbKingPanaZilla.cs',
    'gbKingPanaZillaKillah.cs',
    'gbPANAKanal.cs',
    'gbSumoPullback.cs',
    'gbSuperJumpBoost.cs',
    'gbThunderZilla.cs',
    'GodZilla.cs',
    'GodZillaKilla.cs',
    'NewsSignals.cs'
)

# ── Validate target folder ─────────────────────────────────────────────────────
if (-not (Test-Path $CustomFolder)) {
    Write-Error "bin\Custom folder not found: $CustomFolder`nUse -CustomFolder to specify the correct path."
    exit 1
}

Write-Host ""
Write-Host "KingPanaZilla — File Removal Utility" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Scanning: $CustomFolder" -ForegroundColor Gray
Write-Host ""

# ── Search and delete ──────────────────────────────────────────────────────────
$found   = 0
$deleted = 0
$errors  = 0

foreach ($fileName in $TargetFiles) {
    $matches = Get-ChildItem -Path $CustomFolder -Filter $fileName -Recurse -File -ErrorAction SilentlyContinue

    if ($matches.Count -eq 0) {
        Write-Host "  [NOT FOUND]  $fileName" -ForegroundColor DarkGray
        continue
    }

    foreach ($file in $matches) {
        $found++
        $relativePath = $file.FullName.Substring($CustomFolder.Length).TrimStart('\','/')

        if ($PSCmdlet.ShouldProcess($file.FullName, 'Delete')) {
            try {
                Remove-Item -Path $file.FullName -Force
                $deleted++
                Write-Host "  [DELETED]    $relativePath" -ForegroundColor Green
            }
            catch {
                $errors++
                Write-Host "  [ERROR]      $relativePath — $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        else {
            # -WhatIf path
            Write-Host "  [WOULD DELETE] $relativePath" -ForegroundColor Yellow
        }
    }
}

# ── Summary ────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "────────────────────────────────────────" -ForegroundColor Gray

if ($WhatIfPreference) {
    Write-Host "Dry run complete.  $found file(s) would be deleted." -ForegroundColor Yellow
    Write-Host "Re-run without -WhatIf to perform the actual deletion." -ForegroundColor Yellow
}
else {
    if ($deleted -gt 0) {
        Write-Host "Done.  $deleted file(s) deleted." -ForegroundColor Green
    }
    else {
        Write-Host "Done.  No files were found to delete." -ForegroundColor Gray
    }
    if ($errors -gt 0) {
        Write-Host "$errors file(s) could not be deleted (see errors above)." -ForegroundColor Red
    }
}

Write-Host ""
