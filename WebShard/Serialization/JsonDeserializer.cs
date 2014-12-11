using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebShard.Serialization
{
    public enum TokenizerState
    {
        
    }

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

    public enum TokenType
    {
        Whitespace,
        Comment,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Identifier,
        String,
        Number,
        False,
        True,
        Null,
        Comma,
    }

    public class JsonTokenizer
    {
        private readonly string _input;
        private char _currentValue;
        private int _position;
        private int _startLine;
        private int _startColumn;

        private int _currentLine;
        private int _currentColumn;

        private StringBuilder _currentToken;

        public JsonTokenizer(string input)
        {
            _currentToken = new StringBuilder();
            _input = input;
            _currentLine = 1;
            _startLine = 1;
            _startColumn = 1;
        }

        private bool TryRead()
        {
            if (_position + 1 > _input.Length)
                return false;

            if (_currentValue == '\n')
            {
                _currentLine++;
                _currentColumn = 0;
            }
            else
                _currentColumn++;

            _currentValue = _input[_position++];
                
            return true;
        }

        private void Consume()
        {
            TryRead();
            _currentToken.Append(_currentValue);
        }

        private void ConsumeWhile(Func<char, bool> exp)
        {
            char ch;
            while (TryPeek(0, out ch) && exp(ch))
                Consume();
        }

        private Token EmitToken(TokenType type)
        {
            var result = new Token(_startLine, _startColumn, _currentToken.ToString(), type);
            _currentToken.Clear();
            _startColumn = _currentColumn;
            _startLine = _currentLine;
            return result;
        }

        private bool TryReadString()
        {
            char ch;
            if (TryPeek(0, out ch) && ch == '"')
            {
                Consume();
                char ch1;
                while (TryPeek(0, out ch1) && ((Func<char, bool>) (c => c!= '"'))(ch1))
                    Consume();
                Consume();
                return true;
            }
            return false;
        }

        private bool TryPeek(int offset, out char value)
        {
            if (_position + offset >= _input.Length)
            {
                value = default(char);
                return false;
            }
            value = _input[_position + offset];
            return true;
        }

        private bool TryPeek(string constant)
        {
            if (_position + constant.Length > _input.Length)
                return false;
            for (int i = 0; i < constant.Length; i++)
            {
                if (_input[_position + i] != constant[i])
                    return false;
            }
            return true;
        }

        private bool TryConsumeComment()
        {
            if (TryPeek("/*"))
            {
                Consume();
                Consume();

                bool lastWasAsterisk = false;
                char ch;
                while (TryPeek(0, out ch))
                {
                    if (ch == '*')
                        lastWasAsterisk = true;
                    else
                    {
                        if (ch == '/' && lastWasAsterisk)
                        {
                            Consume();
                            return true;
                        }
                        lastWasAsterisk = false;
                    }
                    Consume();
                }
                return true;
            }
            return false;
        }

        private bool TryConsumeLineComment()
        {
            if (TryPeek("//"))
            {
                Consume();
                Consume();
                char ch;
                while (TryPeek(0, out ch) && ch != 'n')
                {
                    if (ch == '\r')
                        TryRead();
                    else
                        Consume();
                }
                if (ch == '\n')
                    TryRead();

                return true;
            }
            return false;
        }

        private bool TryConsumeRegex(string regex)
        {
            var reg = new Regex(regex, RegexOptions.Singleline);
            var match = reg.Match(_input, _position);
            if (match.Success)
            {
                for (int i = 0; i < match.Value.Length; i++)
                    Consume();
                return true;
            }
            return false;
        }


        private bool TryConsumeConstant(string constant)
        {
            if (TryPeek(constant))
            {
                for (int i = 0; i < constant.Length; i++)
                    Consume();
                return true;
            }
            return false;
        }

        public IEnumerable<Token> Tokenize()
        {
            while (_position < _input.Length)
            {
                if (TryConsumeRegex(@"^\s+"))
                {
                    yield return EmitToken(TokenType.Whitespace);
                    continue;
                }
                if (TryConsumeComment())
                {
                    yield return EmitToken(TokenType.Comment);
                    continue;
                }
                if (TryConsumeLineComment())
                {
                    yield return EmitToken(TokenType.Comment);
                    continue;
                }
                if (TryConsumeRegex(@"^((\d+(\.\d+)?)|(\.\d+))"))
                {
                    yield return EmitToken(TokenType.Number);
                    continue;
                }

                if (TryConsumeConstant("{"))
                {
                    yield return EmitToken(TokenType.LeftBrace);
                    continue;
                }
                if (TryConsumeConstant("}"))
                {
                    yield return EmitToken(TokenType.RightBrace);
                    continue;
                }
                if (TryConsumeConstant("["))
                {
                    yield return EmitToken(TokenType.LeftBracket);
                    continue;
                }
                if (TryConsumeConstant("]"))
                {
                    yield return EmitToken(TokenType.RightBracket);
                    continue;
                }
                if (TryConsumeConstant(","))
                {
                    yield return EmitToken(TokenType.Comma);
                    continue;
                }
                if (TryReadString())
                {
                    yield return EmitToken(TokenType.String);
                    continue;
                }
                if (TryConsumeConstant("null"))
                {
                    yield return EmitToken(TokenType.Null);
                    continue;
                }
                if (TryConsumeConstant("false"))
                {
                    yield return EmitToken(TokenType.False);
                    continue;
                }
                if (TryConsumeConstant("true"))
                {
                    yield return EmitToken(TokenType.True);
                    continue;
                }
                throw new NotImplementedException();
            }
        }
    }

    public class JsonDeserializer
    {
        internal static readonly JsonDeserializer Instance = new JsonDeserializer();

        public static object Deserialize(string json, Type resultType)
        {
            throw new NotImplementedException();
        }
    }
}
