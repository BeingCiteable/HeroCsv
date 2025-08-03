# NuGet API Key Setup Guide

## Step-by-Step Instructions

### 1. Create NuGet Account (if needed)
- Go to https://www.nuget.org/
- Click "Sign in" → "Register"
- Use your Microsoft, GitHub, or create a new account

### 2. Create API Key
1. Go to https://www.nuget.org/account/apikeys
2. Click the **"+ Create"** button
3. Fill in the form EXACTLY as shown:

```
Key Name: FastCsv-GitHub-Actions
Expiration: 365 days (or select "Non-expiring")
Package Owner: [Your username will be pre-selected]

Select scopes:
☑ Push
☑ Push new packages and package versions
☐ Unlist package

Glob Pattern: FastCsv
```

**CRITICAL**: 
- ✅ Check BOTH "Push" AND "Push new packages and package versions"
- ✅ Use exactly `FastCsv` for glob pattern (case-sensitive, no wildcards)
- ❌ Don't use `fastcsv`, `FastCsv*`, or `*`

4. Click **"Create"**
5. **IMMEDIATELY COPY THE KEY!** You won't see it again!

### 3. Test Your Key Locally
```powershell
# Run the validation script
.\validate-nuget-key.ps1

# Or test manually
dotnet pack src/FastCsv/FastCsv.csproj -c Release -o ./artifacts
dotnet nuget push ./artifacts/FastCsv.1.0.0-prerelease.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
```

### 4. Add to GitHub
1. Go to your repository: https://github.com/BeingCiteable/FastCsv
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"** (or update existing)
4. Add:
   - Name: `NUGET_API_KEY`
   - Secret: [Paste your API key]
5. Click **"Add secret"**

### 5. Re-run Failed Workflow
1. Go to **Actions** tab
2. Click on the failed workflow run
3. Click **"Re-run all jobs"**

## Common Issues

### "403 Forbidden" Error
- **Wrong permissions**: Need BOTH push permissions checked
- **Wrong account**: Using different NuGet account than package owner
- **Glob mismatch**: Pattern doesn't match exactly `FastCsv`

### "401 Unauthorized" Error  
- **Invalid key**: Key was copied incorrectly
- **Expired key**: Key has expired

### Still Not Working?
Try creating an unrestricted key:
- Glob Pattern: `*` (asterisk)
- All permissions checked
- Test locally first!

## Delete After Setup
Once your key is working:
```powershell
Remove-Item ./validate-nuget-key.ps1
Remove-Item ./test-nuget-key.ps1
Remove-Item ./verify-nuget.ps1
Remove-Item ./NUGET-KEY-SETUP.md
```