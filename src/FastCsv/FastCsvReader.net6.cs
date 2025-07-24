#if NET6_0_OR_GREATER
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace FastCsv;

/// <summary>
/// Async operations for FastCsvReader
/// </summary>
internal sealed partial class FastCsvReader
{
    /// <inheritdoc />
    public async IAsyncEnumerable<string[]> ReadRecordsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Reset();
        
        if (_stream != null)
        {
            // True async for streams
            await foreach (var record in GetRecordsAsyncFromStream(cancellationToken))
            {
                yield return record;
            }
        }
        else
        {
            // Sync wrapped in async for string content
            while (!cancellationToken.IsCancellationRequested && TryReadRecord(out var record))
            {
                yield return record.ToArray();
            }
        }
    }

    
    private async IAsyncEnumerable<string[]> GetRecordsAsyncFromStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_streamReader == null) yield break;
        
        string? line;
        while (!cancellationToken.IsCancellationRequested && (line = await _streamReader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrEmpty(line))
            {
                _lineNumber++;
                continue;
            }
            
            var fields = CsvParser.ParseLine(line.AsSpan(), _options);
            _recordCount++;
            _lineNumber++;
            yield return fields;
        }
    }

}
#endif