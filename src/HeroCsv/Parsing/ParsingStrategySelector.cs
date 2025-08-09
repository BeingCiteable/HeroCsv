using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HeroCsv.Core;
using HeroCsv.Models;

namespace HeroCsv.Parsing;

/// <summary>
/// Selects the most appropriate parsing strategy for a given CSV line
/// </summary>
public sealed class ParsingStrategySelector
{
    private readonly List<IParsingStrategy> _strategies;
    private readonly IParsingStrategy _fallbackStrategy;

    public ParsingStrategySelector(StringBuilderPool? stringBuilderPool = null)
    {
        _strategies = new List<IParsingStrategy>();

#if NET9_0_OR_GREATER
        // Add Vector512 strategy if hardware supports it
        if (System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated)
        {
            _strategies.Add(new Vector512ParsingStrategy());
        }
#endif

#if NET8_0_OR_GREATER
        // Add optimized SIMD strategy for .NET 8+
        if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
        {
            _strategies.Add(new SIMDOptimizedParsingStrategy());
        }
#endif

        // Add standard strategies
        _strategies.Add(new SimpleCommaParsingStrategy());

        // Fallback strategy for complex cases
        _fallbackStrategy = new QuotedFieldParsingStrategy(stringBuilderPool);
        _strategies.Add(_fallbackStrategy);

        // Sort by priority (highest first)
        _strategies = [.. _strategies.OrderByDescending(s => s.Priority)];
    }

    /// <summary>
    /// Selects and executes the best parsing strategy for the given line
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string[] ParseLine(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Quick check for empty line
        if (line.IsEmpty)
            return [];

        // Find the first strategy that can handle this line
        foreach (var strategy in _strategies)
        {
            if (strategy.CanHandle(line, options))
            {
                return strategy.Parse(line, options);
            }
        }

        // Fallback to quoted field parser if no strategy matches
        return _fallbackStrategy.Parse(line, options);
    }

    /// <summary>
    /// Gets the available strategies for diagnostics
    /// </summary>
    public IReadOnlyList<IParsingStrategy> Strategies => _strategies.AsReadOnly();
}