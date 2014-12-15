using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WebShard.Serialization.Json
{
    public class JsonTokenizer : IEnumerable<Token>
    {
        private readonly string _input;
        private char _currentValue;
        private int _position;
        private int _startLine;
        private int _startColumn;

        private int _currentLine;
        private int _currentColumn;

        private readonly StringBuilder _currentToken;

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

        private static readonly char[] extraIdentifierChars = {'_', '-'};
        private bool TryConsumeIdentifier()
        {
            
            char ch;
            if (TryPeek(0, out ch) && char.IsLetter(ch) || extraIdentifierChars.Contains(ch))
            {
                Consume();
                ConsumeWhile(c => char.IsLetterOrDigit(c) || extraIdentifierChars.Contains(c));
                return true;
            }
            return false;
        }

        private bool TryConsumeString()
        {
            char ch;
            if (TryPeek(0, out ch) && ch == '"')
            {
                Consume();
                bool lastWasSlash = false;
                char ch1;
                while (TryPeek(0, out ch1))
                {
                    if (!lastWasSlash && ch1 == '\\')
                        lastWasSlash = true;
                    else if (!lastWasSlash && ch1 == '\"')
                        break;
                    else
                        lastWasSlash = false;
                    Consume();
                }
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

        private bool TryConsumeWhitespace()
        {
            char ch;
            if (TryPeek(0, out ch) && char.IsWhiteSpace(ch))
            {
                ConsumeWhile(Char.IsWhiteSpace);
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

        private bool TryConsumeNumber()
        {
            char ch;
            int offset = 0;
            if (TryPeek(0, out ch) && (ch == '-' || ch == '+'))
                offset++;
            if (TryPeek(offset, out ch) && char.IsDigit(ch))
            {
                while (TryPeek(offset, out ch) && char.IsDigit(ch))
                    offset++;
                if (TryPeek(offset, out ch) && ch == '.')
                {
                    offset++;
                    if (TryPeek(offset, out ch) && char.IsDigit(ch))
                    {
                        while (TryPeek(offset, out ch) && char.IsDigit(ch))
                            offset++;
                    }
                    else
                        return false;
                }
            }
            else if (TryPeek(offset, out ch) && ch == '.')
            {
                offset++;
                if (TryPeek(offset, out ch) && char.IsDigit(ch))
                {
                    while (TryPeek(offset, out ch) && char.IsDigit(ch))
                        offset++;
                }
                else
                    return false;
            }
            else
                return false;

            for (int i = 0; i < offset; i++)
                Consume();
            return true;
        }

        public IEnumerable<Token> Tokenize()
        {
            while (_position < _input.Length)
            {
                if (TryConsumeWhitespace())
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
                if (TryConsumeNumber())
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
                if (TryConsumeConstant(":"))
                {
                    yield return EmitToken(TokenType.Colon);
                    continue;
                }
                if (TryConsumeString())
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
                if (TryConsumeIdentifier())
                {
                    yield return EmitToken(TokenType.Identifier);
                    continue;
                }
                throw new FormatException(string.Format("Unexpected character '{0}' at line {1} character {2}.", _input[_position], _currentLine, _currentColumn));
            }
            _currentLine = 1;
            _startLine = 1;
            _startColumn = 0;
            _position = 0;
            _currentValue = '\0';
            _currentToken.Clear();
        }

        public IEnumerator<Token> GetEnumerator()
        {
            return Tokenize().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}