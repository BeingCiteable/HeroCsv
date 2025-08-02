#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates the version number in Directory.Build.props

.DESCRIPTION
    This script updates the version number for the FastCsv project.
    It modifies the Directory.Build.props file and optionally creates a git tag.

.PARAMETER Version
    The new version number (e.g., 1.0.1, 2.0.0-preview1)

.PARAMETER CreateTag
    If specified, creates a git tag for the version

.PARAMETER Push
    If specified with CreateTag, pushes the tag to origin

.EXAMPLE
    .\update-version.ps1 -Version 1.0.1
    Updates the version to 1.0.1

.EXAMPLE
    .\update-version.ps1 -Version 1.0.1 -CreateTag -Push
    Updates version, creates tag v1.0.1, and pushes to origin
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [switch]$CreateTag,
    
    [switch]$Push
)

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?$') {
    Write-Error "Invalid version format. Expected: X.Y.Z or X.Y.Z-suffix (e.g., 1.0.0 or 1.0.0-preview1)"
    exit 1
}

# Path to Directory.Build.props
$propsFile = Join-Path $PSScriptRoot "Directory.Build.props"

if (-not (Test-Path $propsFile)) {
    Write-Error "Directory.Build.props not found at: $propsFile"
    exit 1
}

# Read the file
$content = Get-Content $propsFile -Raw

# Update version
$oldVersionPattern = '<Version>.*?</Version>'
$newVersionTag = "<Version>$Version</Version>"
$updatedContent = $content -replace $oldVersionPattern, $newVersionTag

# Also update FileVersion and AssemblyVersion (X.Y.Z.0 format)
$versionParts = $Version.Split('-')[0]  # Remove prerelease suffix
$fileVersion = "$versionParts.0"

$oldFileVersionPattern = '<FileVersion>.*?</FileVersion>'
$newFileVersionTag = "<FileVersion>$fileVersion</FileVersion>"
$updatedContent = $updatedContent -replace $oldFileVersionPattern, $newFileVersionTag

$oldAssemblyVersionPattern = '<AssemblyVersion>.*?</AssemblyVersion>'
$newAssemblyVersionTag = "<AssemblyVersion>$fileVersion</AssemblyVersion>"
$updatedContent = $updatedContent -replace $oldAssemblyVersionPattern, $newAssemblyVersionTag

# Write the updated content
Set-Content -Path $propsFile -Value $updatedContent -NoNewline

Write-Host "✅ Updated version to $Version in Directory.Build.props" -ForegroundColor Green

# Create git tag if requested
if ($CreateTag) {
    $tagName = "v$Version"
    
    # Check if we're in a git repository
    if (-not (Test-Path ".git")) {
        Write-Warning "Not in a git repository. Skipping tag creation."
    }
    else {
        # Check if tag already exists
        $existingTag = git tag -l $tagName
        if ($existingTag) {
            Write-Error "Tag $tagName already exists"
            exit 1
        }
        
        # Create the tag
        git tag -a $tagName -m "Release version $Version"
        Write-Host "✅ Created git tag: $tagName" -ForegroundColor Green
        
        # Push if requested
        if ($Push) {
            Write-Host "Pushing tag to origin..." -ForegroundColor Yellow
            git push origin $tagName
            Write-Host "✅ Pushed tag $tagName to origin" -ForegroundColor Green
        }
        else {
            Write-Host "💡 To push the tag, run: git push origin $tagName" -ForegroundColor Cyan
        }
    }
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Commit the version change: git add Directory.Build.props && git commit -m `"chore: bump version to $Version`"" -ForegroundColor Cyan
Write-Host "2. Push the commit: git push" -ForegroundColor Cyan
if (-not $CreateTag) {
    Write-Host "3. Create a release on GitHub with tag v$Version" -ForegroundColor Cyan
}