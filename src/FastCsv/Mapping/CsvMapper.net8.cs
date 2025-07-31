#if NET8_0_OR_GREATER
using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastCsv.Core;
using FastCsv.Models;

namespace FastCsv.Mapping;

/// <summary>
/// NET8+ optimizations for CsvMapper with frozen collections
/// </summary>
internal sealed partial class CsvMapper<T> where T : class, new()
{
        private FrozenDictionary<string, List<PropertyInfo>>? _frozenPropertyMap;
        private FrozenDictionary<int, List<PropertyInfo>>? _frozenIndexMap;
        private FrozenDictionary<int, Func<string, object?>>? _frozenConverters;

        /// <summary>
        /// Optimizes mapping performance by creating frozen collections
        /// </summary>
        public void OptimizeForRepeatedUse()
        {
            _frozenPropertyMap = _propertyMap.ToFrozenDictionary();
            _frozenIndexMap = _indexMap.ToFrozenDictionary();
            _frozenConverters = _converters.ToFrozenDictionary();
        }

        /// <summary>
        /// Maps a CSV record using frozen collections for maximum performance
        /// </summary>
        /// <param name="record">CSV record as string array</param>
        /// <returns>Mapped object instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T MapRecordFast(string[] record)
        {
            var instance = new T();
            var indexMap = _frozenIndexMap ?? (IReadOnlyDictionary<int, List<PropertyInfo>>)_indexMap;
            var converters = _frozenConverters ?? (IReadOnlyDictionary<int, Func<string, object?>>)_converters;

            // Use frozen collections for optimal lookup performance
            foreach (var kvp in indexMap)
            {
                var index = kvp.Key;
                var properties = kvp.Value;

                if (index < record.Length)
                {
                    var value = record[index];
                    
                    // Map the same value to all properties that map to this index
                    foreach (var property in properties)
                    {
                        if (!string.IsNullOrEmpty(value) || !_options.SkipEmptyFields)
                        {
                            var convertedValue = converters.TryGetValue(index, out var converter)
                                ? converter(value)
                                : ConvertValue(index, value, property);
                            property.SetValue(instance, convertedValue);
                        }
                    }
                }
            }

            return instance;
        }

    }

#endif