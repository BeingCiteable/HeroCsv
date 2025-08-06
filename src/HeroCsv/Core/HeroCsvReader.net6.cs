#if NET6_0_OR_GREATER
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using HeroCsv.DataSources;
using HeroCsv.Parsing;

namespace HeroCsv.Core;

/// <summary>
/// Async operations for HeroCsvReader
/// </summary>
public sealed partial class HeroCsvReader
{
    /// <inheritdoc />
    public async IAsyncEnumerable<string[]> ReadRecordsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
#if NET7_0_OR_GREATER
        ThrowIfDisposed();
#else
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HeroCsvReader));
        }
#endif

        // Only reset if the data source supports it
        if (_dataSource.SupportsReset)
        {
            Reset();
        }

        bool headerSkipped = !_options.HasHeader;

        while (true)
        {
            // Check for cancellation before reading each line
            cancellationToken.ThrowIfCancellationRequested();
            
            var (success, line, lineNumber) = await _dataSource.TryReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (!success) break;

            // Skip empty lines
            if (string.IsNullOrEmpty(line)) continue;

            // Skip header if configured and not yet skipped
            if (_options.HasHeader && !headerSkipped)
            {
                headerSkipped = true;
                // Parse header to get field count for validation
                if (_validationHandler.IsEnabled)
                {
                    var headerFields = CsvParser.ParseLine(line.AsSpan(), _options);
                    // Expected field count will be set on first data row
                }
                continue;
            }

            var fields = CsvParser.ParseLine(line.AsSpan(), _options);

            // Perform validation if enabled
            if (_validationHandler.IsEnabled || _errorHandler.IsEnabled)
            {
                _validationHandler.ValidateRecord(fields, lineNumber, _validationHandler.ExpectedFieldCount);
            }

            _recordCount++;
            
            // Yield the record (cancellation will be checked on next iteration)
            yield return fields;
        }
    }
}

#endif