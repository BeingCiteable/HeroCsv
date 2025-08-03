@echo off
REM Simple test for NuGet push
echo Building package...
dotnet pack src\FastCsv\FastCsv.csproj -c Release -o .\artifacts

echo.
echo Package created:
dir .\artifacts\*.nupkg

echo.
echo To test your API key, run:
echo dotnet nuget push .\artifacts\FastCsv.1.0.0-prerelease.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate

echo.
echo Or with verbose output:
echo dotnet nuget push .\artifacts\FastCsv.1.0.0-prerelease.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate --verbosity detailed