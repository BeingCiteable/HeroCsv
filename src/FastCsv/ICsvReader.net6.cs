#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastCsv;

/// <summary>
/// IAsyncEnumerable operations for ICsvReader (.NET 6.0+)
/// </summary>
public partial interface ICsvReader
{
    /// <summary>
    /// Get records as async enumerable
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of CSV records as string arrays</returns>
    IAsyncEnumerable<string[]> GetRecordsAsync(CancellationToken cancellationToken = default);

}
#endif