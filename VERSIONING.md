# FastCsv Versioning

## Version Format

FastCsv uses [Semantic Versioning](https://semver.org/): **MAJOR.MINOR.PATCH**

- **MAJOR**: Breaking API changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes

Prerelease versions: `1.0.0-preview1`, `1.0.0-beta1`, `1.0.0-rc1`

## Version Location

Version is managed in `Directory.Build.props`:
```xml
<Version>1.0.0</Version>
```

## Creating Releases

See [RELEASE-GUIDE.md](RELEASE-GUIDE.md) for detailed release instructions.

### Quick Process
1. Update version in `Directory.Build.props`
2. Commit and push
3. Create GitHub release with tag `v1.0.0`
4. CI/CD automatically publishes to NuGet

## Version Guidelines

### When to Increment

**MAJOR++ (1.0.0 → 2.0.0)**
- Removing public APIs
- Changing method signatures
- Breaking behavior changes

**MINOR++ (1.0.0 → 1.1.0)**
- Adding new features
- Adding new public APIs
- Performance improvements

**PATCH++ (1.0.0 → 1.0.1)**
- Bug fixes
- Documentation updates
- Internal refactoring

### Prerelease Naming
- `-preview#`: Early development
- `-beta#`: Feature complete, testing
- `-rc#`: Release candidate

## Automation

The release workflow (`release.yml`) handles:
- Version validation
- Package building
- NuGet publishing
- GitHub release creation

For manual releases: Actions → "Release to NuGet" → Run workflow