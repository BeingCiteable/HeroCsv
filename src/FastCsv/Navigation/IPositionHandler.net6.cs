#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;

namespace FastCsv.Navigation;

/// <summary>
/// .NET 6+ performance tracking enhancements for IPositionHandler
/// </summary>
public partial interface IPositionHandler
{
    /// <summary>
    /// Get processing rate (records per second)
    /// </summary>
    double GetProcessingRate();
    
    /// <summary>
    /// Get elapsed processing time
    /// </summary>
    TimeSpan GetElapsedTime();
    
    /// <summary>
    /// Start performance measurement
    /// </summary>
    void StartMeasurement();
    
    /// <summary>
    /// Stop performance measurement
    /// </summary>
    void StopMeasurement();
}
#endif