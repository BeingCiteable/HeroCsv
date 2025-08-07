# 🚀 Release v[VERSION]

## 🎯 Release Type
- [ ] Major (Breaking changes) - X.0.0
- [ ] Minor (New features, backwards compatible) - 0.X.0
- [ ] Patch (Bug fixes only) - 0.0.X
- [ ] Pre-release (preview/beta/rc)

## 📋 Pre-Release Checklist
- [ ] All PRs for this release merged to `master`
- [ ] All CI checks passing
- [ ] Version updated in `Directory.Build.props`
- [ ] CHANGELOG.md updated
- [ ] Documentation updated for new features
- [ ] No build warnings
- [ ] All tests passing: `dotnet test`

## ⚡ Performance Validation
- [ ] Benchmarks run locally: `dotnet run -c Release -- quick`
- [ ] No performance regressions detected
- [ ] Performance improvements documented (if any)

## 📦 What's Included

### ✨ New Features
<!-- List new features with PR links -->
- 

### 🐛 Bug Fixes
<!-- List bug fixes with issue links -->
- 

### 💥 Breaking Changes
<!-- Describe any breaking changes -->
- None

### 📚 Documentation
<!-- List documentation updates -->
- 

## 📝 Release Notes Draft
<!-- This will be used for the GitHub release -->
```markdown
## 🎉 Highlights

## 📊 Performance
View detailed benchmarks: https://beingciteable.github.io/HeroCsv/releases/v[VERSION]/

## ✨ What's New

## 🐛 Bug Fixes

## 💥 Breaking Changes

## 🙏 Contributors
```

## 🚀 Post-Merge Actions
<!-- These will be handled by GitHub Actions -->
1. ✅ Automatic: Tag creation via release workflow
2. ✅ Automatic: NuGet package publication
3. ✅ Automatic: Performance benchmarks run
4. ✅ Automatic: Benchmark page published
5. 🔄 Manual: Create GitHub release from draft

## 🔗 Links
- **Benchmark Results**: Will be available at `https://beingciteable.github.io/HeroCsv/releases/v[VERSION]/`
- **NuGet Package**: Will be at `https://www.nuget.org/packages/HeroCsv/[VERSION]`

/cc @maintainers