# Test NuGet API key locally
# DO NOT COMMIT THIS FILE!

$apiKey = Read-Host "Enter your NuGet API key" -AsSecureString
$apiKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))

Write-Host "Building package..." -ForegroundColor Yellow
dotnet pack src/FastCsv/FastCsv.csproj -c Release -o ./test-artifacts

Write-Host "`nTesting API key..." -ForegroundColor Yellow
# Note: There's no --dry-run flag for dotnet nuget push
# We'll use --skip-duplicate to avoid errors if package exists
$testResult = dotnet nuget push ./test-artifacts/*.nupkg `
    --api-key $apiKeyPlain `
    --source https://api.nuget.org/v3/index.json `
    --skip-duplicate 2>&1 | Out-String

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ API key is valid!" -ForegroundColor Green
} else {
    Write-Host "`n❌ API key test failed!" -ForegroundColor Red
}

# Cleanup
Remove-Item -Path ./test-artifacts -Recurse -Force -ErrorAction SilentlyContinue