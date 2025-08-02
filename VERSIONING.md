# Version Management Guide for FastCsv

## Overview

FastCsv uses semantic versioning (SemVer) and automated CI/CD pipelines for releases. Version numbers follow the format `MAJOR.MINOR.PATCH` (e.g., 1.0.0).

## Version Number Structure

- **MAJOR**: Incompatible API changes
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible
- **PRERELEASE**: Optional suffix (e.g., 1.0.0-preview1)

## How to Update Version

### Method 1: Centralized Version (Recommended)

The version is managed centrally in `Directory.Build.props`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
</PropertyGroup>
```

To update the version:
1. Edit `Directory.Build.props`
2. Update the `<Version>` element
3. Commit and push changes

### Method 2: GitHub Release

1. Create a new release on GitHub
2. Use tag format `v1.0.0` (with 'v' prefix)
3. The CI/CD pipeline will automatically:
   - Update the version in packages
   - Build and test
   - Publish to NuGet

### Method 3: Manual Workflow Dispatch

1. Go to Actions → Release to NuGet
2. Click "Run workflow"
3. Enter version number (without 'v' prefix)
4. The workflow will create a tag and release

## Version Management Best Practices

### When to Update Versions

- **MAJOR version**: Breaking changes
  - Removing public APIs
  - Changing method signatures
  - Changing behavior in incompatible ways

- **MINOR version**: New features
  - Adding new public APIs
  - Adding new optional parameters
  - Performance improvements

- **PATCH version**: Bug fixes
  - Fixing bugs without changing API
  - Documentation updates
  - Internal refactoring

### Prerelease Versions

For preview releases, use suffixes:
- `1.0.0-preview1` - First preview
- `1.0.0-rc1` - Release candidate
- `1.0.0-beta1` - Beta release

## CI/CD Pipeline

### Continuous Integration (CI)

The CI pipeline runs on:
- Every push to main/master/develop
- Every pull request
- Manual workflow dispatch

It performs:
- Multi-OS testing (Windows, Linux, macOS)
- Multi-framework testing (.NET 6-9)
- Code coverage reporting
- Package creation

### Release Pipeline

The release pipeline:
1. Validates version format
2. Updates version in project files
3. Builds and tests
4. Creates NuGet packages
5. Publishes to:
   - NuGet.org
   - GitHub Packages
6. Creates GitHub release with assets

## Setting Up Secrets

Required GitHub secrets:
- `NUGET_API_KEY`: Your NuGet.org API key
- `CODECOV_TOKEN`: (Optional) For coverage reports

To add secrets:
1. Go to Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Add the required secrets

## Version Update Examples

### Example 1: Bug Fix Release

Current version: 1.0.0
```bash
# Update Directory.Build.props
<Version>1.0.1</Version>

# Commit and push
git add Directory.Build.props
git commit -m "chore: bump version to 1.0.1"
git push

# Create release
git tag v1.0.1
git push origin v1.0.1
```

### Example 2: New Feature Release

Current version: 1.0.1
```bash
# Update Directory.Build.props
<Version>1.1.0</Version>

# Create and push tag
git tag v1.1.0 -m "feat: add async stream processing"
git push origin v1.1.0
```

### Example 3: Preview Release

```bash
# Update Directory.Build.props
<Version>2.0.0-preview1</Version>

# Create prerelease on GitHub
# Mark as "prerelease" in GitHub UI
```

## Automation Features

### Auto-versioning with GitVersion (Future Enhancement)

Consider adding GitVersion for automatic versioning based on git history:
- Automatic version bumping based on commit messages
- Branch-based versioning strategies
- No manual version updates needed

### Conventional Commits (Future Enhancement)

Using conventional commits can enable automatic version bumping:
- `fix:` commits trigger patch version bump
- `feat:` commits trigger minor version bump
- `BREAKING CHANGE:` triggers major version bump

## Troubleshooting

### Version Conflicts

If you get version conflicts:
1. Ensure Directory.Build.props has the correct version
2. Clean and rebuild: `dotnet clean && dotnet build`
3. Clear NuGet cache: `dotnet nuget locals all --clear`

### Failed Releases

If a release fails:
1. Check GitHub Actions logs
2. Verify API keys are valid
3. Ensure version doesn't already exist on NuGet
4. Check version format is valid SemVer

### Rollback

To rollback a release:
1. Delete the release on GitHub
2. Delete the tag: `git push --delete origin v1.0.0`
3. Unlist the package on NuGet.org (cannot delete)

## Quick Reference

```bash
# Check current version
grep -n "<Version>" Directory.Build.props

# Update version
sed -i 's/<Version>.*<\/Version>/<Version>1.0.2<\/Version>/' Directory.Build.props

# Create release tag
git tag -a v1.0.2 -m "Release version 1.0.2"
git push origin v1.0.2

# Trigger manual release
# Go to GitHub Actions → Release to NuGet → Run workflow
```