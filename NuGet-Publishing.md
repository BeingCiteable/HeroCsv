# Publishing FastCsv to NuGet.org

## Prerequisites

1. Create a NuGet.org account at https://www.nuget.org/
2. Generate an API key from your account settings
3. Install the NuGet CLI or use dotnet CLI

## Building the Package

```bash
# Build in Release mode
dotnet build -c Release

# Create the NuGet package
dotnet pack -c Release
```

The package files will be created in `src/FastCsv/bin/Release/`:
- `FastCsv.1.0.0.nupkg` - The main NuGet package
- `FastCsv.1.0.0.snupkg` - Symbol package for debugging

## Publishing to NuGet.org

### Using dotnet CLI

```bash
# Set your API key (do this once)
dotnet nuget push --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Push the package
cd src/FastCsv/bin/Release
dotnet nuget push FastCsv.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# The symbol package will be pushed automatically
```

### Using NuGet CLI

```bash
# Push the package
nuget push FastCsv.1.0.0.nupkg -ApiKey YOUR_API_KEY -Source https://api.nuget.org/v3/index.json
```

## Testing the Package Locally

Before publishing, you can test the package locally:

```bash
# Create a local feed
dotnet nuget add source C:\projects\csv\src\FastCsv\bin\Release --name FastCsvLocal

# Install from local feed
dotnet add package FastCsv --source FastCsvLocal
```

## Version Management

Update the version in `FastCsv.csproj`:

```xml
<PackageVersion>1.0.1</PackageVersion>
```

Follow semantic versioning:
- Major version: Breaking changes
- Minor version: New features, backward compatible
- Patch version: Bug fixes

## GitHub Actions for Automated Publishing

You can automate the publishing process using GitHub Actions. Create `.github/workflows/publish.yml`:

```yaml
name: Publish to NuGet

on:
  release:
    types: [published]

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Build
      run: dotnet build -c Release
    - name: Pack
      run: dotnet pack -c Release
    - name: Push to NuGet
      run: dotnet nuget push src/FastCsv/bin/Release/*.nupkg --api-key ${{secrets.NUGET_API_KEY}} --source https://api.nuget.org/v3/index.json
```

Remember to add your NuGet API key as a secret in your GitHub repository settings.