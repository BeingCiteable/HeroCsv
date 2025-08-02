# Setting Up GitHub Secrets for FastCsv CI/CD

This guide walks you through obtaining and configuring the required secrets for the FastCsv CI/CD pipeline.

## 1. NuGet.org API Key (Required)

### Step 1: Create a NuGet.org Account
1. Go to https://www.nuget.org/
2. Click "Sign in" (top right)
3. Sign in with a Microsoft account, or create one
4. Complete your profile information

### Step 2: Generate API Key
1. Click on your username (top right) → "API Keys"
   - Direct link: https://www.nuget.org/account/apikeys
2. Click "Create" or "+ Create"
3. Fill in the form:
   - **Key Name**: `FastCsv-GitHub-Actions` (or any descriptive name)
   - **Expiration**: Choose appropriate duration (365 days recommended)
   - **Package Owner**: Select your username
   - **Scopes**: 
     - ✅ Push
     - ✅ Push new packages and package versions
   - **Glob Pattern**: `FastCsv*` (to limit key to FastCsv packages)
     - Or use `*` for all packages
4. Click "Create"
5. **IMPORTANT**: Copy the key immediately! You won't see it again.
   - It looks like: `oy2jxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

### Step 3: Add to GitHub Secrets
1. Go to your GitHub repository
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Add:
   - **Name**: `NUGET_API_KEY`
   - **Secret**: Paste your API key
5. Click "Add secret"

### Security Best Practices for NuGet API Keys:
- Use package-specific glob patterns
- Set reasonable expiration dates
- Regenerate keys periodically
- Never commit keys to source control
- Use separate keys for different purposes

## 2. Codecov Token (Optional, for Coverage Reports)

### Step 1: Sign Up for Codecov
1. Go to https://codecov.io/
2. Click "Sign Up" 
3. Choose "Sign up with GitHub"
4. Authorize Codecov to access your GitHub account

### Step 2: Add Your Repository
1. Once logged in, click "Add a repository"
2. Find your repository in the list
   - You may need to click "Configure" to grant access to specific repos
3. Click on your repository name

### Step 3: Get the Upload Token
1. In your repository settings on Codecov
2. Look for "Upload Token" section
3. Copy the token (looks like a UUID: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)

### Step 4: Add to GitHub Secrets
1. Go to your GitHub repository
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Add:
   - **Name**: `CODECOV_TOKEN`
   - **Secret**: Paste your Codecov token
5. Click "Add secret"

## 3. Verifying Your Setup

### Check NuGet API Key
After your first release, verify at:
- https://www.nuget.org/packages/FastCsv
- You should see your package listed

### Check Codecov Integration
After your first CI run with tests:
- Go to https://codecov.io/gh/[your-username]/FastCsv
- You should see coverage reports

## 4. GitHub Token (Automatic)

The `GITHUB_TOKEN` is automatically provided by GitHub Actions. You don't need to create it. It's used for:
- Publishing to GitHub Packages
- Creating releases
- Updating repository content

## 5. Additional Optional Secrets

### For Slack Notifications
If you want build notifications:
```
SLACK_WEBHOOK_URL: https://hooks.slack.com/services/YOUR/WEBHOOK/URL
```

### For Discord Notifications
```
DISCORD_WEBHOOK_URL: https://discord.com/api/webhooks/YOUR/WEBHOOK
```

## Troubleshooting

### NuGet Push Fails with 401
- API key may be expired
- Key might not have push permissions
- Package ID might not match glob pattern

### NuGet Push Fails with 409
- Version already exists
- Use `--skip-duplicate` in push command (already configured)

### Codecov Not Showing Reports
- Ensure tests are generating coverage files
- Check if token is valid
- Verify repository is activated on Codecov

## Quick Reference

| Secret Name | Required | Purpose | Where to Get |
|------------|----------|---------|--------------|
| `NUGET_API_KEY` | ✅ Yes | Push packages to NuGet.org | https://www.nuget.org/account/apikeys |
| `CODECOV_TOKEN` | ❌ No | Upload coverage reports | https://codecov.io/ → Your repo → Settings |
| `GITHUB_TOKEN` | Automatic | GitHub operations | Provided by GitHub Actions |

## Security Reminders

1. **Never share secrets** in issues, commits, or logs
2. **Rotate keys periodically** (every 6-12 months)
3. **Use minimal permissions** (only what's needed)
4. **Monitor usage** through NuGet.org dashboard
5. **Revoke compromised keys immediately**

## Example Workflow Test

Once you've added the secrets, test the workflow:

```bash
# Create a test release
git tag v0.0.1-test
git push origin v0.0.1-test

# Go to GitHub Actions to monitor the workflow
# Delete test tag afterward:
git push --delete origin v0.0.1-test
git tag -d v0.0.1-test
```