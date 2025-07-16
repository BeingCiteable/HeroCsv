#if NET6_0_OR_GREATER
using System;
using System.Numerics;

namespace FastCsv.Fields;

/// <summary>
/// .NET 6+ vectorized enhancements for IFieldHandler
/// </summary>
public partial interface IFieldHandler
{
    /// <summary>
    /// Whether hardware acceleration is available for field processing
    /// </summary>
    bool IsVectorizedProcessingAvailable { get; }
    
    /// <summary>
    /// Enable or disable hardware-accelerated field processing
    /// </summary>
    void SetVectorizedProcessing(bool enabled);
}
#endif