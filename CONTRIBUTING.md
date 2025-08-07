# Contributing to HeroCsv

## Simple Development Workflow

1. **Fork & Clone**

   ```bash
   git clone https://github.com/BeingCiteable/HeroCsv.git
   cd HeroCsv
   ```

2. **Create Feature Branch**

   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/issue-description
   ```

3. **Make Changes**

   - Follow existing code style
   - Add tests for new features
   - Update documentation if needed

4. **Test Locally**

   ```bash
   dotnet test
   dotnet build -c Release
   ```

5. **Submit PR**
   - Push to your fork
   - Create PR to `master` branch
   - Fill out PR template
   - Wait for CI and review

## Branch Naming

- `feature/` - New features
- `fix/` - Bug fixes
- `perf/` - Performance improvements
- `doc/` - Documentation only
- `refactor/` - Code refactoring

## Commit Messages

Follow conventional commits:

```
feat: add CSV writer support
fix: resolve memory leak in parser
perf: optimize SearchValues for NET8
docs: update README examples
```

## Version Management (Maintainers)

### Continuous Development

- PRs are merged to master continuously
- Version in `Directory.Build.props` stays as-is between releases
- No version bumps until ready to release

### When Ready to Release

1. **Decide on version number** based on changes since last release:

   - Major: Breaking changes (1.0.0 → 2.0.0)
   - Minor: New features (1.0.0 → 1.1.0)
   - Patch: Bug fixes only (1.0.0 → 1.0.1)

2. **Create Release PR**

   ```bash
   git checkout master
   git pull origin master
   git checkout -b release/v1.0.1
   ```

3. **Update version** in `Directory.Build.props`:

   ```xml
   <Version>1.0.1</Version>
   ```

4. **Create PR** with title "Release v1.0.1"

   - Include summary of changes
   - Wait for CI to pass
   - Merge to master

5. **Tag and Release**

   ```bash
   git checkout master
   git pull origin master
   git tag v1.0.1
   git push origin v1.0.1
   ```

6. **GitHub Actions** automatically:
   - Builds and tests
   - Publishes to NuGet
   - Creates GitHub release

### Between Releases

Keep version in `Directory.Build.props` as:

- Current released version, OR
- Next version with `-dev` suffix (e.g., `1.0.2-dev`)

This way:

- Master always builds and passes tests
- Version only changes for releases
- Clear separation between development and release
