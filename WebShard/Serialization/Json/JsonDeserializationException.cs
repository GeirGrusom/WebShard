using System;
using System.Runtime.Serialization;

namespace WebShard.Serialization.Json
{
    [Serializable]
    public class JsonDeserializationException : Exception
    {
        private readonly int _lineNumber;
        private readonly int _column;
        private readonly string _token;

        public int LineNumber { get { return _lineNumber; } }
        public int Column { get { return _column; } }
        public string Token { get { return _token; } }

        internal JsonDeserializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _lineNumber = info.GetInt32("_lineNumber");
            _column = info.GetInt32("_column");
            _token = info.GetString("_token");
        }

        public JsonDeserializationException(Token token, string message, Exception innerException)
            : base(message, innerException)
        {
            _lineNumber = token.Line;
            _column = token.Column;
            _token = token.Value;
        }

        public JsonDeserializationException(Token token, string message)
            : base(message)
        {
            _lineNumber = token.Line;
            _column = token.Column;
            _token = token.Value;
        }

        public JsonDeserializationException(Token token, Exception innerException)
            : this(token,
                string.Format("There was an error deserializing the document in line {0} column {1} '{2}'.", token.Line,
                    token.Column, token.Value), innerException)
        {
        }

        public JsonDeserializationException(Token token)
            : this(token,
                string.Format("There was an error deserializing the document in line {0} column {1} '{2}'.", token.Line,
                    token.Column, token.Value))
        {
        }
    }
}
