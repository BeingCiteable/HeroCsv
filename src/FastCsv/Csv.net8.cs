#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using FastCsv.Models;

namespace FastCsv;

/// <summary>
/// Advanced parsing features for Csv
/// </summary>
public static partial class Csv
    {
        private static readonly SearchValues<char> CommonDelimiters = SearchValues.Create(",;\t|");
        private static readonly SearchValues<char> QuoteChars = SearchValues.Create("\"'");

        /// <summary>
        /// Auto-detect CSV format and read
        /// </summary>
        /// <param name="content">CSV content as string</param>
        /// <returns>Enumerable of string arrays representing records</returns>
        public static IEnumerable<string[]> ReadAutoDetect(string content)
        {
            var options = AutoDetectFormat(content.AsSpan());
            return ReadContent(content, options);
        }

        /// <summary>
        /// Auto-detect CSV format from file and read
        /// </summary>
        /// <param name="filePath">Path to CSV file</param>
        /// <returns>Enumerable of string arrays representing records</returns>
        public static IEnumerable<string[]> ReadFileAutoDetect(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return ReadAutoDetect(content);
        }

        /// <summary>
        /// Auto-detect CSV format and read with zero-allocation span processing
        /// </summary>
        /// <param name="content">CSV content as memory span</param>
        /// <returns>Enumerable of string arrays representing records</returns>
        public static IEnumerable<string[]> ReadAutoDetect(ReadOnlySpan<char> content)
        {
            var options = AutoDetectFormat(content);
            return ReadAllRecords(content, options);
        }

        private static CsvOptions AutoDetectFormat(ReadOnlySpan<char> content)
        {
            var sampleSize = Math.Min(content.Length, 2000);
            var sample = content.Slice(0, sampleSize);

            var commaCount = 0;
            var semicolonCount = 0;
            var tabCount = 0;
            var hasQuotes = false;

            var position = 0;
            while (position < sample.Length)
            {
                var nextDelimiter = sample.Slice(position).IndexOfAny(CommonDelimiters);
                if (nextDelimiter == -1) break;

                var actualPos = position + nextDelimiter;
                switch (sample[actualPos])
                {
                    case ',': commaCount++; break;
                    case ';': semicolonCount++; break;
                    case '\t': tabCount++; break;
                    case '|': break; // Could add pipe support
                }
                position = actualPos + 1;
            }

            // Check for quotes
            hasQuotes = sample.IndexOfAny(QuoteChars) != -1;

            var delimiter = ',';
            if (semicolonCount > commaCount && semicolonCount > tabCount)
                delimiter = ';';
            else if (tabCount > commaCount && tabCount > semicolonCount)
                delimiter = '\t';

            return new CsvOptions(delimiter, hasQuotes ? '"' : '"', true);
        }
}

#endif