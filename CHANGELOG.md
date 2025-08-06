# Changelog

All notable changes to HeroCsv will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive performance benchmarks covering all major features
- Full transparency competitor comparison benchmarks against CsvHelper, Sylvan, Sep, and others
- CI/CD benchmark automation with GitHub Actions
- Benchmark results publishing to GitHub Pages
- XML documentation for all public APIs
- Improved error messages with field preview context
- `leaveOpen` parameter for stream operations
- Dedicated benchmark documentation (BENCHMARKS.md)
- Automated benchmark regression detection in pull requests

### Changed
- Enhanced error messages to include line numbers and field previews
- Improved type converter error messages with valid format examples
- Updated README with performance transparency information
- Optimized stream reading to use reader directly instead of materializing content

### Fixed
- Stream disposal issues when using `ReadStream` methods
- Test reliability for async cancellation scenarios

## [1.0.0-prerelease] - 2024-XX-XX

### Added
- Initial release of HeroCsv
- Ultra-fast CSV parsing with zero-allocation design
- Simple static API for easy usage
- Generic object mapping with auto, manual, and mixed modes
- Comprehensive validation system
- Async operations for .NET 7+
- Auto-detection features for .NET 8+
- Multi-framework support (.NET Standard 2.0, .NET 6-9)
- Rich extension methods for type conversion
- Stream processing with encoding support
- Field iterator for ultra-fast processing
- Row enumeration with zero allocations
- Builder pattern for advanced configuration
- Error handling with detailed error types
- DateTimeOffset support for timezone awareness
- Custom type converters
- Attribute-based column mapping

### Performance Features
- ReadOnlySpan<char> throughout for zero allocations
- Hardware acceleration with Vector operations (.NET 6+)
- SearchValues optimization (.NET 8+)
- Vector512 support (.NET 9+)
- Pre-allocated collections
- Aggressive inlining
- Ref struct enumerators

[Unreleased]: https://github.com/BeingCiteable/HeroCsv/compare/v1.0.0...HEAD
[1.0.0-prerelease]: https://github.com/BeingCiteable/HeroCsv/releases/tag/v1.0.0-prerelease