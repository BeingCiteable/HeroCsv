namespace FastCsv;

/// <summary>
/// Zero-allocation enumerable for parsing CSV from ReadOnlySpan
/// </summary>
internal readonly ref struct CsvSpanEnumerable
{
    private readonly ReadOnlySpan<char> _content;
    private readonly CsvOptions _options;

    public CsvSpanEnumerable(ReadOnlySpan<char> content, CsvOptions options)
    {
        _content = content;
        _options = options;
    }

    public Enumerator GetEnumerator() => new(_content, _options);

    /// <summary>
    /// Ref struct enumerator for zero-allocation enumeration
    /// </summary>
    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<char> _content;
        private readonly CsvOptions _options;
        private int _position;
        private string[]? _current;
        private bool _skipHeader;

        internal Enumerator(ReadOnlySpan<char> content, CsvOptions options)
        {
            _content = content;
            _options = options;
            _position = 0;
            _current = null;
            _skipHeader = options.HasHeader;

            // Skip BOM if present
            if (_content.Length >= 3 && _content[0] == '\uFEFF')
            {
                _position = 1;
            }
        }

        public readonly string[] Current => _current ?? throw new InvalidOperationException();

        public bool MoveNext()
        {
            while (_position < _content.Length)
            {
                // Find and parse line
                var lineEnd = CsvParser.FindLineEnd(_content, _position);
                
                if (lineEnd > _position)
                {
                    var line = _content.Slice(_position, lineEnd - _position);
                    var fields = CsvParser.ParseLine(line, _options);

                    _position = CsvParser.SkipLineEnding(_content, lineEnd);

                    // Skip header if needed
                    if (_skipHeader && fields.Length > 0)
                    {
                        _skipHeader = false;
                        continue;
                    }

                    if (fields.Length > 0)
                    {
                        _current = fields;
                        return true;
                    }
                    // Empty line, continue to next
                }
                else
                {
                    _position = CsvParser.SkipLineEnding(_content, lineEnd);
                }
            }

            _current = null;
            return false;
        }
    }
}