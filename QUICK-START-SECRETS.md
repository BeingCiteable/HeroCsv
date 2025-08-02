# Quick Start: Setting Up Secrets for FastCsv

## 🔑 NuGet API Key (5 minutes)

### Get the Key:
1. Go to https://www.nuget.org/account/apikeys
2. Sign in with Microsoft account
3. Click "+ Create"
4. Use these settings:
   ```
   Key Name: FastCsv-GitHub-Actions
   Expiration: 365 days
   Scopes: ✅ Push, ✅ Push new packages
   Glob Pattern: FastCsv*
   ```
5. Click "Create" and **COPY THE KEY IMMEDIATELY!**

### Add to GitHub:
1. Go to your repo → Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Add:
   ```
   Name: NUGET_API_KEY
   Secret: [paste your key]
   ```

## 📊 Codecov Token (Optional, 3 minutes)

### Get the Token:
1. Go to https://codecov.io/
2. Sign in with GitHub
3. Click "Add a repository"
4. Select your repository
5. Copy the upload token

### Add to GitHub:
1. Same location as above
2. Add:
   ```
   Name: CODECOV_TOKEN
   Secret: [paste your token]
   ```

## ✅ Verify Everything Works

Run this test:
```bash
# Windows PowerShell
./update-version.ps1 -Version 0.0.1-test -CreateTag -Push

# Linux/macOS
./update-version.sh 0.0.1-test --create-tag --push
```

Then check:
- GitHub Actions tab → "Release to NuGet" workflow should run
- Look for green checkmarks
- Delete test tag after: `git push --delete origin v0.0.1-test`

## 🚨 Common Mistakes to Avoid

1. **Forgetting to copy the NuGet key** - You can't see it again!
2. **Wrong glob pattern** - Use `FastCsv*` not just `FastCsv`
3. **No push permissions** - Make sure both push scopes are checked
4. **Spaces in the secret** - Don't include extra spaces when pasting

## 📋 Complete Checklist

- [ ] NuGet account created
- [ ] API key generated with correct permissions
- [ ] API key added to GitHub as `NUGET_API_KEY`
- [ ] (Optional) Codecov token added
- [ ] Test workflow run successfully

## 🆘 Troubleshooting

**Error: 401 Unauthorized**
- API key is wrong or expired
- Re-create the key and update the secret

**Error: 403 Forbidden**
- Package name doesn't match glob pattern
- You're not the package owner

**Error: 409 Conflict**
- Version already exists (this is OK, workflow handles it)

---

**Time Required**: ~8 minutes total
**Difficulty**: Easy
**Prerequisites**: GitHub repository access, Microsoft account