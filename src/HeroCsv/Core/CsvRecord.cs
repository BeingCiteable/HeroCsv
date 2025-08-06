using System.Runtime.CompilerServices;

namespace HeroCsv.Core;

/// <summary>
/// High-performance CSV row implementation using spans for zero allocations
/// </summary>
internal sealed partial class CsvRecord(string[] fields, int lineNumber) : ICsvRecord
{
        private readonly string[] _fields = fields ?? throw new ArgumentNullException(nameof(fields));

        public int LineNumber => lineNumber;

        public int FieldCount => _fields.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetField(int index)
        {
            if (index < 0 || index >= _fields.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Field index {index} is out of range. Record has {_fields.Length} fields on line {lineNumber}. " +
                    $"Available fields: {GetFieldPreview()}");
            }
            return _fields[index].AsSpan();
        }

        /// <summary>
        /// Attempts to get field by index
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetField(int index, out ReadOnlySpan<char> field)
        {
            if (index >= 0 && index < _fields.Length)
            {
                field = _fields[index].AsSpan();
                return true;
            }
            field = ReadOnlySpan<char>.Empty;
            return false;
        }

        /// <summary>
        /// Checks if field index is valid
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < _fields.Length;
        }

        /// <summary>
        /// Copies all fields to destination span
        /// </summary>
        public int GetAllFields(Span<string> destination)
        {
            var count = Math.Min(_fields.Length, destination.Length);
            for (int i = 0; i < count; i++)
            {
                destination[i] = _fields[i];
            }
            return count;
        }

        /// <summary>
        /// Converts to string array
        /// </summary>
        public string[] ToArray()
        {
            var result = new string[_fields.Length];
            Array.Copy(_fields, result, _fields.Length);
            return result;
        }
        
        /// <summary>
        /// Gets a preview of fields for error messages
        /// </summary>
        private string GetFieldPreview()
        {
            if (_fields.Length == 0) return "(empty record)";
            
            const int maxFieldsToShow = 3;
            const int maxFieldLength = 20;
            
            var fieldsToShow = Math.Min(_fields.Length, maxFieldsToShow);
            var preview = new string[fieldsToShow];
            
            for (int i = 0; i < fieldsToShow; i++)
            {
                var field = _fields[i];
                if (field.Length > maxFieldLength)
                {
                    preview[i] = $"[{i}]=\"{field.Substring(0, maxFieldLength)}...\"";
                }
                else
                {
                    preview[i] = $"[{i}]=\"{field}\"";
                }
            }
            
            var result = string.Join(", ", preview);
            if (_fields.Length > maxFieldsToShow)
            {
                result += $", ... ({_fields.Length - maxFieldsToShow} more)";
            }
            
            return result;
        }

}