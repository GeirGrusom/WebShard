namespace WebShard.Serialization.Json
{
    public struct Token
    {
        private readonly int _line;
        private readonly int _column;
        private readonly string _value;
        private readonly TokenType _type;
        public Token(int line, int column, string value, TokenType type)
        {
            _line = line;
            _column = column;
            _value = value;
            _type = type;
        }

        public int Line { get { return _line; } }
        public int Column { get { return _column; } }
        public string Value { get { return _value; } }
        public TokenType Type { get { return _type; } }

        public override string ToString()
        {
            return _value;
        }
    }
}