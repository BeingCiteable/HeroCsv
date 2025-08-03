# Verify NuGet source and test API key
Write-Host "Checking NuGet sources..." -ForegroundColor Yellow
dotnet nuget list source

Write-Host "`nVerifying NuGet.org is accessible..." -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "https://api.nuget.org/v3/index.json" -UseBasicParsing
if ($response.StatusCode -eq 200) {
    Write-Host "✅ NuGet.org API is accessible" -ForegroundColor Green
} else {
    Write-Host "❌ Cannot reach NuGet.org API" -ForegroundColor Red
}

Write-Host "`nTo test your API key manually:" -ForegroundColor Yellow
Write-Host "1. Build the package:"
Write-Host "   dotnet pack src/FastCsv/FastCsv.csproj -c Release -o ./artifacts" -ForegroundColor Cyan
Write-Host "`n2. Try pushing with your API key:"
Write-Host "   dotnet nuget push ./artifacts/FastCsv.1.0.0-prerelease.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Cyan
Write-Host "`n3. Or with verbose output:"
Write-Host "   dotnet nuget push ./artifacts/FastCsv.1.0.0-prerelease.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json --verbosity detailed" -ForegroundColor Cyan