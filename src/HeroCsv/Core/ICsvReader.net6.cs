#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeroCsv.Core;

/// <summary>
/// IAsyncEnumerable operations for ICsvReader (.NET 6.0+)
/// </summary>
public partial interface ICsvReader
{
    /// <summary>
    /// Read records as async enumerable
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of CSV records as string arrays</returns>
    IAsyncEnumerable<string[]> ReadRecordsAsync(CancellationToken cancellationToken = default);
}

#endif