# HeroCsv Release Guide

## Quick Start

### 1. Set Up NuGet API Key (One-time setup)

1. Sign in to https://www.nuget.org/account/apikeys
2. Click "+ Create"
3. Use these settings:
   ```
   Key Name: HeroCsv-GitHub-Actions
   Expiration: 365 days
   Scopes: ✅ Push, ✅ Push new packages
   Glob Pattern: HeroCsv*
   ```
4. **Copy the key immediately!**
5. Add to GitHub: Settings → Secrets → Actions → New secret
   ```
   Name: NUGET_API_KEY
   Secret: [paste your key]
   ```

### 2. Create a Release

#### Option A: Using GitHub UI (Recommended)
1. Update version in `Directory.Build.props`
2. Commit and push changes
3. Go to GitHub → Releases → "Draft a new release"
4. Create tag: `v1.0.0` (with 'v' prefix)
5. Publish release (workflow runs automatically)

#### Option B: Using Command Line
```bash
# Update version in Directory.Build.props
# Then commit and push

# Create and push tag
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# Go to GitHub and create release from tag
```

## Version Management

Version is centrally managed in `Directory.Build.props`:
```xml
<Version>1.0.0</Version>
```

### Version Format
- **MAJOR.MINOR.PATCH** (e.g., 1.0.0)
- **Prerelease**: 1.0.0-preview1, 1.0.0-beta1

### When to Update
- **MAJOR**: Breaking API changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes

## CI/CD Workflows

### Automatic Triggers
- **CI Build**: On every push and PR
- **Release**: On GitHub release creation
- **Benchmarks**: On push to main/master

### Manual Release
1. Actions → "Release to NuGet" → "Run workflow"
2. Enter version (without 'v' prefix)

## Troubleshooting

### Release Failed
- Check Actions logs for errors
- Verify NUGET_API_KEY is set correctly
- Ensure version doesn't already exist

### Common Fixes
```bash
# Delete tag if needed
git push --delete origin v1.0.0
git tag -d v1.0.0

# Re-run failed workflow
# Go to Actions → Click failed run → "Re-run all jobs"
```

## First Release Checklist

- [ ] NuGet account created
- [ ] API key added to GitHub secrets
- [ ] Version set to 0.0.1 in Directory.Build.props
- [ ] All changes committed and pushed
- [ ] Tag created: `git tag -a v0.0.1 -m "Initial release"`
- [ ] Tag pushed: `git push origin v0.0.1`
- [ ] GitHub release created
- [ ] Workflow completed successfully
- [ ] Package visible on NuGet.org

## Links
- [NuGet Package](https://www.nuget.org/packages/HeroCsv/)
- [GitHub Actions](https://github.com/BeingCiteable/HeroCsv/actions)
- [API Keys](https://www.nuget.org/account/apikeys)