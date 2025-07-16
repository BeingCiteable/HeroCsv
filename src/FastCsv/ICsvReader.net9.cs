#if NET9_0_OR_GREATER
using System;

namespace FastCsv;

/// <summary>
/// Advanced hardware acceleration and diagnostics enhancements for ICsvReader
/// </summary>
public partial interface ICsvReader
{
    /// <summary>
    /// Whether advanced hardware acceleration is available
    /// </summary>
    bool IsVector512Supported { get; }

    /// <summary>
    /// Enable advanced profiling and diagnostics
    /// </summary>
    void EnableProfiling(bool enabled);
}
#endif