#!/usr/bin/env pwsh
# Script to validate NuGet API key before using in GitHub Actions
# DO NOT COMMIT THIS FILE - IT WILL CONTAIN YOUR API KEY!

param(
    [Parameter(Mandatory=$false)]
    [string]$ApiKey
)

Write-Host "=== NuGet API Key Validator ===" -ForegroundColor Cyan
Write-Host "This script will help validate your NuGet API key" -ForegroundColor Yellow
Write-Host ""

# Get API key if not provided
if ([string]::IsNullOrEmpty($ApiKey)) {
    $secureKey = Read-Host "Enter your NuGet API key" -AsSecureString
    $ApiKey = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey))
}

# Validate key format
Write-Host "1. Checking API key format..." -ForegroundColor Yellow
if ($ApiKey.Length -lt 32) {
    Write-Host "   ❌ API key seems too short. NuGet keys are usually 32+ characters" -ForegroundColor Red
    exit 1
} else {
    Write-Host "   ✅ API key format looks valid" -ForegroundColor Green
}

# Check NuGet source
Write-Host "`n2. Checking NuGet source configuration..." -ForegroundColor Yellow
$sources = dotnet nuget list source 2>&1 | Out-String
if ($sources -match "nuget\.org.*https://api\.nuget\.org/v3/index\.json") {
    Write-Host "   ✅ NuGet.org source is configured" -ForegroundColor Green
} else {
    Write-Host "   ❌ NuGet.org source not found" -ForegroundColor Red
    Write-Host "   Adding NuGet.org source..." -ForegroundColor Yellow
    dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
}

# Build package
Write-Host "`n3. Building package..." -ForegroundColor Yellow
Remove-Item -Path ./test-artifacts -Recurse -Force -ErrorAction SilentlyContinue
$buildResult = dotnet pack src/FastCsv/FastCsv.csproj -c Release -o ./test-artifacts 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Package built successfully" -ForegroundColor Green
    $packageFile = Get-ChildItem ./test-artifacts/*.nupkg | Select-Object -First 1
    Write-Host "   📦 Package: $($packageFile.Name)" -ForegroundColor Cyan
} else {
    Write-Host "   ❌ Build failed:" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

# Test API key with service index
Write-Host "`n4. Testing API key with NuGet service..." -ForegroundColor Yellow
try {
    $headers = @{
        "X-NuGet-ApiKey" = $ApiKey
        "X-NuGet-Protocol-Version" = "4.1.0"
    }
    $response = Invoke-RestMethod -Uri "https://www.nuget.org/api/v2/package" -Method Get -Headers $headers -ErrorAction Stop
    Write-Host "   ✅ API key is valid and can connect to NuGet" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq "Unauthorized") {
        Write-Host "   ❌ API key is invalid or expired" -ForegroundColor Red
    } else {
        Write-Host "   ⚠️  Could not validate key via service index (this is normal)" -ForegroundColor Yellow
    }
}

# Test push with dry run (if available)
Write-Host "`n5. Testing package push..." -ForegroundColor Yellow
Write-Host "   Attempting to push package (will skip if already exists)..." -ForegroundColor Gray

$pushResult = dotnet nuget push $packageFile.FullName `
    --api-key $ApiKey `
    --source https://api.nuget.org/v3/index.json `
    --skip-duplicate `
    --verbosity detailed 2>&1 | Out-String

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Push test successful!" -ForegroundColor Green
    if ($pushResult -match "already exists") {
        Write-Host "   ℹ️  Package version already exists (this is OK)" -ForegroundColor Cyan
    }
} else {
    Write-Host "   ❌ Push test failed!" -ForegroundColor Red
    
    # Analyze error
    if ($pushResult -match "403|Forbidden") {
        Write-Host "`n   Possible issues:" -ForegroundColor Yellow
        Write-Host "   - API key doesn't have 'Push new packages' permission" -ForegroundColor White
        Write-Host "   - API key glob pattern doesn't match 'FastCsv'" -ForegroundColor White
        Write-Host "   - You're not the package owner (for existing packages)" -ForegroundColor White
        Write-Host "   - API key is from wrong NuGet account" -ForegroundColor White
    } elseif ($pushResult -match "401|Unauthorized") {
        Write-Host "`n   Issue: API key is invalid or expired" -ForegroundColor Yellow
    } elseif ($pushResult -match "409|Conflict") {
        Write-Host "`n   Issue: Package version already exists" -ForegroundColor Yellow
    }
    
    Write-Host "`n   Full error:" -ForegroundColor Yellow
    Write-Host $pushResult
}

# Show API key info
Write-Host "`n6. API Key Checklist:" -ForegroundColor Yellow
Write-Host "   When creating your NuGet API key, ensure:" -ForegroundColor White
Write-Host "   ✓ Key Name: Something descriptive like 'FastCsv-GitHub-Actions'" -ForegroundColor Gray
Write-Host "   ✓ Expiration: 365 days or non-expiring" -ForegroundColor Gray
Write-Host "   ✓ Package Owner: Your NuGet username" -ForegroundColor Gray
Write-Host "   ✓ Scopes: BOTH 'Push' AND 'Push new packages and package versions'" -ForegroundColor Gray
Write-Host "   ✓ Glob Pattern: Exactly 'FastCsv' (case-sensitive)" -ForegroundColor Gray

# Cleanup
Write-Host "`n7. Cleaning up..." -ForegroundColor Yellow
Remove-Item -Path ./test-artifacts -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "   ✅ Cleanup complete" -ForegroundColor Green

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Your API key appears to be working correctly!" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Go to GitHub → Settings → Secrets → Actions" -ForegroundColor White
    Write-Host "2. Update NUGET_API_KEY with this key" -ForegroundColor White
    Write-Host "3. Re-run the failed workflow" -ForegroundColor White
} else {
    Write-Host "❌ Your API key validation failed" -ForegroundColor Red
    Write-Host "`nPlease create a new API key at:" -ForegroundColor Yellow
    Write-Host "https://www.nuget.org/account/apikeys" -ForegroundColor Cyan
}

Write-Host "`n⚠️  Remember to delete this script or clear your PowerShell history!" -ForegroundColor Yellow
Write-Host "   Run: Remove-Item ./validate-nuget-key.ps1" -ForegroundColor Gray