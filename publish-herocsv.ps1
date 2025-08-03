# Quick publish script for HeroCsv
Write-Host "Building HeroCsv package..." -ForegroundColor Cyan

# Clean previous artifacts
Remove-Item -Path ./artifacts -Recurse -Force -ErrorAction SilentlyContinue

# Build and pack
dotnet pack src/FastCsv/FastCsv.csproj -c Release -o ./artifacts

# Show created package
Write-Host "`nPackage created:" -ForegroundColor Green
Get-ChildItem ./artifacts/*.nupkg | ForEach-Object {
    Write-Host "  📦 $($_.Name)" -ForegroundColor Yellow
}

Write-Host "`nTo publish to NuGet:" -ForegroundColor Cyan
Write-Host "1. Create API key at https://www.nuget.org/account/apikeys with:" -ForegroundColor White
Write-Host "   - Glob pattern: HeroCsv" -ForegroundColor Yellow
Write-Host "   - Scopes: Push + Push new packages" -ForegroundColor Yellow
Write-Host "`n2. Run:" -ForegroundColor White
Write-Host "   dotnet nuget push .\artifacts\HeroCsv.1.0.0-prerelease.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Green