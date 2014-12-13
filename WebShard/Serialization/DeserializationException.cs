using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebShard.Serialization
{
    [Serializable]
    public class DeserializationException : Exception
    {
        private readonly int _lineNumber;
        private readonly int _column;
        private readonly string _token;

        public int LineNumber { get { return _lineNumber; } }
        public int Column { get { return _column; } }
        public string Token { get { return _token; } }

        internal DeserializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _lineNumber = info.GetInt32("_lineNumber");
            _column = info.GetInt32("_column");
            _token = info.GetString("_token");
        }

        public DeserializationException(Token token, string message, Exception innerException)
            : base(message, innerException)
        {
            _lineNumber = token.Line;
            _column = token.Column;
            _token = token.Value;
        }

        public DeserializationException(Token token, string message)
            : base(message)
        {
            _lineNumber = token.Line;
            _column = token.Column;
            _token = token.Value;
        }

        public DeserializationException(Token token, Exception innerException)
            : this(token,
                string.Format("There was an error deserializing the document in line {0} column {1} '{2}'.", token.Line,
                    token.Column, token.Value), innerException)
        {
        }

        public DeserializationException(Token token)
            : this(token,
                string.Format("There was an error deserializing the document in line {0} column {1} '{2}'.", token.Line,
                    token.Column, token.Value))
        {
        }
    }
}
