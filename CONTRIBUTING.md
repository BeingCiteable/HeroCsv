# Contributing to HeroCsv

## Development Workflow

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
- `docs/` - Documentation only
- `refactor/` - Code refactoring

## Commit Messages

Follow conventional commits:

```
feat: add CSV writer support
fix: resolve memory leak in parser
perf: optimize SearchValues for NET8
docs: update README examples
```

## Code Style

- Use latest C# features
- Follow existing patterns
- Add XML documentation for public APIs
- No unnecessary comments

## Testing

- Add unit tests for new features
- Maintain or improve code coverage
- Test across all target frameworks
- Include benchmarks for performance changes

## Release Process (Maintainers)

1. Ensure all PRs for release are merged
2. Update version in `Directory.Build.props`
3. Create and push tag:
   ```bash
   git tag v1.0.1
   git push --tags
   ```
4. GitHub Actions handles the rest
