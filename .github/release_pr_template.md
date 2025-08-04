## Release v[VERSION]

### Release Type
- [ ] Major (Breaking changes)
- [ ] Minor (New features, backwards compatible)
- [ ] Patch (Bug fixes only)
- [ ] Pre-release (Alpha/Beta/RC)

### What's Changed
<!-- Summary of changes in this release -->

### New Features
<!-- List new features if any -->
- 

### Bug Fixes
<!-- List bug fixes if any -->
- 

### Breaking Changes
<!-- List breaking changes if any -->
- None

### Release Checklist
- [ ] All feature branches merged to `dev`
- [ ] All tests passing on `dev` branch
- [ ] Version updated in `Directory.Build.props`
- [ ] CHANGELOG updated (if applicable)
- [ ] Documentation updated for new features
- [ ] Benchmarks run (for performance changes)
- [ ] Manual testing completed
- [ ] Release notes drafted

### Release Notes
<!-- Draft release notes for GitHub release -->
```markdown
## What's New

## Bug Fixes

## Breaking Changes

## Contributors
```

### Post-Merge Actions Required
1. Tag the release on master: `git tag v[VERSION]`
2. Push the tag: `git push origin v[VERSION]`
3. Sync dev with master: `git checkout dev && git merge master && git push`

/cc @maintainers