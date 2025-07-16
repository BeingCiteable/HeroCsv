#if NET8_0_OR_GREATER
using System;
using System.Buffers;

namespace FastCsv.Navigation;

/// <summary>
/// Optimized line counting enhancements for IPositionHandler
/// </summary>
public partial interface IPositionHandler
{
    /// <summary>
    /// Count lines in a span with custom line terminators
    /// </summary>
    int CountLines(ReadOnlySpan<char> data, SearchValues<char> lineTerminators);

    /// <summary>
    /// Seek to a specific line with custom line terminators
    /// </summary>
    bool SeekToLine(int targetLine, ReadOnlySpan<char> data, SearchValues<char> lineTerminators);
}
#endif