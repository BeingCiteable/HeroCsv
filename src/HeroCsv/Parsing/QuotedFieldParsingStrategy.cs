using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using HeroCsv.Core;
using HeroCsv.Models;

namespace HeroCsv.Parsing;

/// <summary>
/// Strategy for parsing CSV lines with quoted fields and escape sequences
/// </summary>
public sealed class QuotedFieldParsingStrategy(StringBuilderPool? stringBuilderPool = null) : IParsingStrategy
{
    private readonly StringBuilderPool _stringBuilderPool = stringBuilderPool ?? new StringBuilderPool();


    public int Priority => 50; // Lower priority, more complex

    public bool IsAvailable => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanHandle(ReadOnlySpan<char> line, CsvOptions options)
    {
        // Handles any line with quotes or when trimming is needed
        return line.IndexOf(options.Quote) >= 0 || options.TrimWhitespace;
    }

    public string[] Parse(ReadOnlySpan<char> line, CsvOptions options)
    {
        if (line.IsEmpty)
            return Array.Empty<string>();

        var fields = new List<string>();
        var fieldBuilder = _stringBuilderPool.Rent();

        try
        {
            int i = 0;
            bool inQuotes = false;
            bool fieldStarted = false;
            bool fieldWasQuoted = false;

            while (i < line.Length)
            {
                char currentChar = line[i];

                if (!inQuotes)
                {
                    if (currentChar == options.Quote && (!fieldStarted || fieldBuilder.Length == 0))
                    {
                        inQuotes = true;
                        fieldStarted = true;
                        fieldWasQuoted = true;
                        i++;
                        continue;
                    }

                    if (currentChar == options.Delimiter)
                    {
                        AddField(fields, fieldBuilder, options, fieldWasQuoted);
                        fieldStarted = false;
                        fieldWasQuoted = false;
                        i++;
                        continue;
                    }

                    if (options.TrimWhitespace && char.IsWhiteSpace(currentChar) && !fieldStarted)
                    {
                        i++;
                        continue;
                    }

                    fieldBuilder.Append(currentChar);
                    fieldStarted = true;
                    i++;
                }
                else // In quotes
                {
                    if (currentChar == options.Quote)
                    {
                        // Check for escaped quote
                        if (i + 1 < line.Length && line[i + 1] == options.Quote)
                        {
                            fieldBuilder.Append(options.Quote);
                            i += 2; // Skip both quotes
                        }
                        else
                        {
                            inQuotes = false;
                            i++;

                            // Skip to next delimiter or end of line
                            while (i < line.Length && line[i] != options.Delimiter)
                            {
                                if (!char.IsWhiteSpace(line[i]))
                                {
                                    // Non-whitespace after closing quote - include it
                                    fieldBuilder.Append(line[i]);
                                }
                                i++;
                            }

                            if (i < line.Length && line[i] == options.Delimiter)
                            {
                                AddField(fields, fieldBuilder, options, fieldWasQuoted);
                                fieldStarted = false;
                                fieldWasQuoted = false;
                                i++;
                            }
                        }
                    }
                    else
                    {
                        fieldBuilder.Append(currentChar);
                        i++;
                    }
                }
            }

            // Add the last field
            AddField(fields, fieldBuilder, options, fieldWasQuoted);

            return fields.ToArray();
        }
        finally
        {
            _stringBuilderPool.Return(fieldBuilder);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddField(List<string> fields, StringBuilder fieldBuilder, CsvOptions options, bool fieldWasQuoted)
    {
        string field = fieldBuilder.ToString();

        // Only trim unquoted fields when TrimWhitespace is enabled
        // Quoted fields should preserve their internal whitespace
        if (options.TrimWhitespace && !fieldWasQuoted)
        {
            field = field.Trim();
        }

        fields.Add(options.StringPool?.GetString(field) ?? field);
        fieldBuilder.Clear();
    }
}