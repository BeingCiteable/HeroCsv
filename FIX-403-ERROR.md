# Fix NuGet 403 Error - Step by Step

## Error Message
> Response status code does not indicate success: 403 (The specified API key is invalid, has expired, or does not have permission to access the specified package.)

## Step 1: Verify Package Ownership
First, check if the package already exists on NuGet:
- Go to: https://www.nuget.org/packages/FastCsv/
- If it exists, note who owns it
- If it doesn't exist, you're creating a new package

## Step 2: Create the Correct API Key

### For a NEW Package (FastCsv doesn't exist yet):
1. Go to https://www.nuget.org/account/apikeys
2. Sign in with your NuGet account
3. Click **"+ Create"**
4. Use these EXACT settings:
   ```
   Key Name: FastCsv-GitHub-Actions-New
   Expiration: 365 days or Non-expiring
   Package Owner: [Your username - should be auto-selected]
   
   Select scopes (MUST have BOTH):
   ☑ Push
   ☑ Push new packages and package versions
   
   Glob Pattern: *
   ```
   **Note**: Using `*` for now to eliminate glob pattern issues

5. Click **"Create"**
6. **COPY THE KEY IMMEDIATELY!**

### For an EXISTING Package:
You must be logged in as the package owner or have been given co-owner permissions.

## Step 3: Test the Key Locally

### Option A: PowerShell Test
```powershell
# Set your key (be careful not to expose it)
$key = "YOUR_API_KEY_HERE"

# Build package
dotnet pack src/FastCsv/FastCsv.csproj -c Release -o ./artifacts

# Test push with verbose output
dotnet nuget push ./artifacts/FastCsv.1.0.0-prerelease.nupkg `
    --api-key $key `
    --source https://api.nuget.org/v3/index.json `
    --verbosity detailed
```

### Option B: Command Prompt Test
```cmd
REM Build package
dotnet pack src\FastCsv\FastCsv.csproj -c Release -o .\artifacts

REM Test push
dotnet nuget push .\artifacts\FastCsv.1.0.0-prerelease.nupkg ^
    --api-key YOUR_API_KEY_HERE ^
    --source https://api.nuget.org/v3/index.json ^
    --verbosity detailed
```

## Step 4: Analyze the Error

### If you get "403 Forbidden":
- API key doesn't have "Push new packages" permission
- Wrong NuGet account (not the owner)
- Package is owned by someone else

### If you get "409 Conflict":
- Package version already exists (this is good! Key works!)
- Use `--skip-duplicate` flag

### If push succeeds:
- Your key is working!
- Proceed to update GitHub

## Step 5: Update GitHub Secret

1. Go to: https://github.com/BeingCiteable/FastCsv/settings/secrets/actions
2. Click on `NUGET_API_KEY` (or create it)
3. Paste your working API key
4. Save

## Step 6: Re-run Workflow

1. Go to: https://github.com/BeingCiteable/FastCsv/actions
2. Find the failed workflow
3. Click "Re-run all jobs"

## Alternative: Create Unrestricted Key

If still having issues, create a completely unrestricted key:
1. Key Name: `FastCsv-Unrestricted-Temp`
2. Expiration: `365 days`
3. Scopes: **ALL checkboxes checked**
4. Glob Pattern: `*`

Test this locally first, then update GitHub.

## Common Mistakes

1. **Wrong Account**: Make sure you're logged into NuGet with the right account
2. **Copy Error**: API keys often have dashes - make sure you copied the whole key
3. **Expired Key**: Check the expiration date on your API keys page
4. **Missing Permission**: MUST have "Push new packages" for first-time packages

## Still Not Working?

Try this diagnostic command:
```powershell
# This will show detailed HTTP communication
$env:NUGET_SHOW_STACK=true
dotnet nuget push ./artifacts/FastCsv.1.0.0-prerelease.nupkg `
    --api-key YOUR_KEY `
    --source https://api.nuget.org/v3/index.json `
    --verbosity diagnostic
```