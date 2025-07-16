#if NET9_0_OR_GREATER
using System;

namespace FastCsv.Fields;

/// <summary>
/// Advanced hardware acceleration enhancements for IFieldHandler
/// </summary>
public partial interface IFieldHandler
{
    /// <summary>
    /// Whether advanced hardware acceleration is available for field processing
    /// </summary>
    bool IsVector512Available { get; }

    /// <summary>
    /// Enable or disable advanced hardware acceleration
    /// </summary>
    void SetVector512Enabled(bool enabled);
}
#endif