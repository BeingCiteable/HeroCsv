#if NET9_0_OR_GREATER
using System;

namespace FastCsv.Navigation;

/// <summary>
/// .NET 9+ advanced line counting enhancements for IPositionHandler
/// </summary>
public partial interface IPositionHandler
{
    /// <summary>
    /// Count lines with advanced processing
    /// </summary>
    int CountLinesAdvanced(ReadOnlySpan<char> data);
}
#endif