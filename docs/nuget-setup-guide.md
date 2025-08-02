# NuGet API Key Setup - Visual Guide

## Step-by-Step Guide to Get Your NuGet API Key

### 1. Sign in to NuGet.org

1. Navigate to https://www.nuget.org/
2. Click **"Sign in"** in the top right corner
3. Sign in with your Microsoft account

![Sign in button location]
(Screenshot: NuGet.org homepage with arrow pointing to Sign in button)

### 2. Access API Keys Section

1. After signing in, click on your **username** in the top right
2. Select **"API Keys"** from the dropdown menu

Alternatively, go directly to: https://www.nuget.org/account/apikeys

![API Keys menu]
(Screenshot: Dropdown menu showing API Keys option)

### 3. Create New API Key

Click the **"+ Create"** button to create a new API key.

![Create button]
(Screenshot: API Keys page with Create button highlighted)

### 4. Configure Your API Key

Fill out the form with these recommended settings:

```
Key Name: FastCsv-GitHub-Actions
Expiration: 365 days (1 year)
Package Owner: [Your Username]
Scopes: 
  ✅ Push
  ✅ Push new packages and package versions
Glob Pattern: FastCsv*
```

![API Key form]
(Screenshot: API key creation form with fields filled)

#### Important Fields Explained:

- **Key Name**: A descriptive name to identify this key
- **Expiration**: How long the key remains valid
- **Scopes**: 
  - Select "Push" for basic package updates
  - Select "Push new packages" if this is a new package
- **Glob Pattern**: 
  - Use `FastCsv*` to limit to FastCsv packages only (recommended)
  - Use `*` if you want to use this key for all your packages

### 5. Copy Your API Key

After clicking **"Create"**, you'll see your API key **ONCE**.

⚠️ **IMPORTANT**: Copy it immediately! You cannot view it again.

The key format looks like:
```
oy2jxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

![API Key displayed]
(Screenshot: Success page showing the API key with copy button)

### 6. Add to GitHub Repository

1. Go to your GitHub repository: https://github.com/BeingCiteable/FastCsv
2. Click **Settings** tab
3. In the left sidebar, find **Secrets and variables** → **Actions**
4. Click **"New repository secret"**

![GitHub Settings]
(Screenshot: GitHub repo settings page)

### 7. Create the Secret

Add your NuGet API key:

```
Name: NUGET_API_KEY
Secret: [Paste your API key here]
```

Click **"Add secret"**

![Add Secret]
(Screenshot: GitHub secret creation form)

### 8. Verify the Secret

You should see `NUGET_API_KEY` listed in your repository secrets.

![Secret Added]
(Screenshot: List showing NUGET_API_KEY in secrets)

## Quick Checklist

- [ ] Created NuGet.org account
- [ ] Generated API key with push permissions
- [ ] Copied API key immediately
- [ ] Added key to GitHub as `NUGET_API_KEY`
- [ ] Key has appropriate expiration date
- [ ] Key is limited to FastCsv packages (glob pattern)

## Testing Your Setup

To test if your API key works:

1. Create a test tag:
   ```bash
   git tag v0.0.1-test
   git push origin v0.0.1-test
   ```

2. Go to the Actions tab in GitHub
3. Watch the "Release to NuGet" workflow
4. Check for any errors in the "Push to NuGet" step

## Common Issues

### "The API key is invalid"
- Double-check you copied the entire key
- Ensure no extra spaces or line breaks
- Verify the key hasn't expired

### "403 Forbidden"
- Check the glob pattern matches your package name
- Ensure the key has push permissions
- Verify you're the package owner

### "409 Conflict"
- The package version already exists
- Our workflow uses `--skip-duplicate` to handle this

## Security Tips

1. **Set expiration dates** - Don't use keys that never expire
2. **Use glob patterns** - Limit keys to specific packages
3. **Separate keys** - Use different keys for different projects
4. **Monitor usage** - Check your NuGet account regularly
5. **Rotate keys** - Replace keys every 6-12 months

## Need Help?

- NuGet Documentation: https://docs.microsoft.com/en-us/nuget/
- NuGet Support: https://www.nuget.org/policies/Contact
- GitHub Secrets Docs: https://docs.github.com/en/actions/security-guides/encrypted-secrets