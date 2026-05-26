<#
.SYNOPSIS
Downloads the Zenodo sample dataset and extracts the nested archives.

.DESCRIPTION
The Zenodo endpoint returns one archive, for example 20393649.zip. That
archive contains dataset archives such as Dresden.zip. Each nested archive is
extracted into src/Resources, resulting in folders such as src/Resources/Dresden.

.EXAMPLE
.\sample_data\download_zenodo_dataset.ps1

.EXAMPLE
.\sample_data\download_zenodo_dataset.ps1 -Overwrite
#>

[CmdletBinding()]
param(
    [string]$Url = "https://zenodo.org/api/records/20393649/files-archive",
    [string]$ResourcesPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "src\Resources"),
    [string]$DownloadPath = (Join-Path ([System.IO.Path]::GetTempPath()) "reflex-deepzoom-sample-data\20393649.zip"),
    [switch]$Overwrite,
    [switch]$KeepDownload
)

$ErrorActionPreference = "Stop"

function New-CleanDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Expand-ArchiveToDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArchivePath,
        [Parameter(Mandatory = $true)]
        [string]$DestinationPath,
        [switch]$Force
    )

    $arguments = @{
        LiteralPath     = $ArchivePath
        DestinationPath = $DestinationPath
    }

    if ($Force) {
        $arguments.Force = $true
    }

    Expand-Archive @arguments
}

$resourcesFullPath = [System.IO.Path]::GetFullPath($ResourcesPath)
$downloadFullPath = [System.IO.Path]::GetFullPath($DownloadPath)
$downloadDirectory = Split-Path -Parent $downloadFullPath
$workDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "reflex-deepzoom-sample-data\extract"

Write-Host "Resource target: $resourcesFullPath"
Write-Host "Download target: $downloadFullPath"

New-Item -ItemType Directory -Path $resourcesFullPath -Force | Out-Null
New-Item -ItemType Directory -Path $downloadDirectory -Force | Out-Null
New-CleanDirectory -Path $workDirectory

try {
    Write-Host "Downloading Zenodo archive..."
    
    # disable progress for faster download
    $oldProgressPreference = $ProgressPreference
    $ProgressPreference = "SilentlyContinue"

    try {
        Invoke-WebRequest -Uri $Url -OutFile $downloadFullPath
    }
    finally {
        # reset progress setting to original value
        $ProgressPreference = $oldProgressPreference
    }

    Write-Host "Extracting Zenodo archive..."
    Expand-ArchiveToDirectory -ArchivePath $downloadFullPath -DestinationPath $workDirectory -Force

    $nestedArchives = Get-ChildItem -LiteralPath $workDirectory -Filter "*.zip" -File -Recurse |
        Sort-Object -Property FullName

    if ($nestedArchives.Count -eq 0) {
        throw "No nested ZIP archives were found in $downloadFullPath."
    }

    foreach ($archive in $nestedArchives) {
        Write-Host "Extracting nested archive: $($archive.Name)"
        Expand-ArchiveToDirectory `
            -ArchivePath $archive.FullName `
            -DestinationPath $resourcesFullPath `
            -Force:$Overwrite
    }

    Write-Host "Done. Extracted $($nestedArchives.Count) nested archive(s) into $resourcesFullPath."
}
finally {
    if (Test-Path -LiteralPath $workDirectory) {
        Remove-Item -LiteralPath $workDirectory -Recurse -Force
    }

    if (-not $KeepDownload -and (Test-Path -LiteralPath $downloadFullPath)) {
        Remove-Item -LiteralPath $downloadFullPath -Force
    }

    # Remove the temporary root if it is empty.
    if (Test-Path -LiteralPath $downloadDirectory) {

        Write-Host "Cleanup. Remove temporary download directory: $($downloadDirectory)."
        $remainingItems = Get-ChildItem -LiteralPath $downloadDirectory -Force

        if ($remainingItems.Count -eq 0) {
            Remove-Item -LiteralPath $downloadDirectory -Force
        }
    }
}
