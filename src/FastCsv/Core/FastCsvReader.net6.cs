#if NET6_0_OR_GREATER
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using FastCsv.DataSources;
using FastCsv.Parsing;

namespace FastCsv.Core;

/// <summary>
/// Async operations for FastCsvReader
/// </summary>
public sealed partial class FastCsvReader
{
    /// <inheritdoc />
    public async IAsyncEnumerable<string[]> ReadRecordsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Only reset if the data source supports it
        if (_dataSource.SupportsReset)
        {
            Reset();
        }

        // Use async data source if available
        if (_dataSource is IAsyncCsvDataSource asyncSource)
        {
            bool headerSkipped = !_options.HasHeader; // If no header, consider it already skipped

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await asyncSource.TryReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (!result.success) break;

                // Skip empty lines
                if (string.IsNullOrEmpty(result.line)) continue;

                // Skip header if configured and not yet skipped
                if (_options.HasHeader && !headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                var fields = CsvParser.ParseLine(result.line.AsSpan(), _options);

                // Perform validation if enabled
                if (_validationHandler.IsEnabled || _errorHandler.IsEnabled)
                {
                    _validationHandler.ValidateRecord(fields, result.lineNumber, _validationHandler.ExpectedFieldCount);
                }

                _recordCount++;
                yield return fields;
            }
        }
        else
        {
            // Fallback to sync method for non-async sources
            while (!cancellationToken.IsCancellationRequested && TryReadRecord(out var record))
            {
                yield return record.ToArray();
            }
        }
    }
}

#endif