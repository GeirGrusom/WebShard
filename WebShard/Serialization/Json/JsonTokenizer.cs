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
        private enum TryConsumeResult
        {
            Success,
            Failed,
            SyntaxError
        }

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

        private void Read()
        {
            unchecked
            {
                if (_currentValue == '\n')
                {
                    _currentLine++;
                    _currentColumn = 0;
                }
                else
                    _currentColumn++;

                _currentValue = _input[_position];
                _position++;
            }
        }

        private void Consume()
        {
            Read();
            _currentToken.Append(_currentValue);
        }

        private Token EmitToken(TokenType type, string value)
        {
            var result = new Token(_startLine, _startColumn, value, type);
            _currentToken.Clear();
            _startColumn = _currentColumn;
            _startLine = _currentLine;
            return result;
        }
        private Token EmitToken(TokenType type)
        {
            return EmitToken(type, _currentToken.ToString());
        }

        private void ConsumeString()
        {
            bool lastWasSlash = false;
            char ch1;
            while (TryPeek(out ch1))
            {
                if (!lastWasSlash)
                {
                    if (ch1 == '\\')
                    {
                        lastWasSlash = true;
                        Consume();
                        continue;
                    }
                    if (ch1 == '"')
                    {
                        Consume();
                        break;
                    }
                }
                else
                    lastWasSlash = false;
                Consume();
            }
        }

        private bool TryPeek(out char value)
        {
            unchecked
            {
                if (_position >= _input.Length)
                {
                    value = default(char);
                    return false;
                }
                value = _input[_position];
                return true;
            }
        }

        private bool TryPeek(int offset, out char value)
        {
            unchecked
            {
                if (_position + offset >= _input.Length)
                {
                    value = default(char);
                    return false;
                }
                value = _input[_position + offset];
                return true;
            }
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

        private bool TryConsumeNumber(char current)
        {
            char ch = current;
            if (ch == '-' || ch == '+')
            {
                Consume();
                if (!TryPeek(out ch))
                    return false;
            }
            if (char.IsDigit(ch))
            {
                Consume();
                while (TryPeek(out ch) && char.IsDigit(ch))
                    Consume();
                if (TryPeek(out ch) && ch == '.')
                {
                    Consume();
                    if (TryPeek(out ch) && char.IsDigit(ch))
                    {
                        Consume();
                        while (TryPeek(out ch) && char.IsDigit(ch))
                            Consume();
                    }
                    else
                        return false;
                }
            }
            else if (ch == '.')
            {
                Consume();
                if (TryPeek(out ch) && char.IsDigit(ch))
                {
                    Consume();
                    while (TryPeek(out ch) && char.IsDigit(ch))
                        Consume();
                }
                else
                    return false;
            }
            else
                return false;

            if (TryPeek(out ch) && (ch == 'e' || ch == 'E'))
            {
                Consume();
                if (TryPeek(out ch) && (ch == '+' || ch == '-'))
                    Consume();
                while (TryPeek(out ch) && char.IsDigit(ch))
                    Consume();
            }

            return true;
        }

        public IEnumerable<Token> Tokenize()
        {
            while (_position < _input.Length)
            {
                char next;
                if (!TryPeek(0, out next))
                    break;
                if(char.IsWhiteSpace(next))
                {
                    Consume();
                    while(TryPeek(out next) && char.IsWhiteSpace(next))
                        Consume();
                    yield return EmitToken(TokenType.Whitespace);
                    continue;
                }
                if (next == '-' || next == '+' || next == '.' || char.IsNumber(next))
                {
                    if (TryConsumeNumber(next))
                    {
                        yield return EmitToken(TokenType.Number);
                        continue;
                    }
                    yield return EmitToken(TokenType.InvalidToken);
                    yield break;
                }

                if (next == '{')
                {
                    Consume();
                    yield return EmitToken(TokenType.LeftBrace);
                    continue;
                }
                if (next == '}')
                {
                    Consume();
                    yield return EmitToken(TokenType.RightBrace);
                    continue;
                }
                if (next == '[')
                {
                    Consume();
                    yield return EmitToken(TokenType.LeftBracket);
                    continue;
                }
                if (next == ']')
                {
                    Consume();
                    yield return EmitToken(TokenType.RightBracket);
                    continue;
                }
                if (next == ',')
                {
                    Consume();
                    yield return EmitToken(TokenType.Comma);
                    continue;
                }
                if (next == ':')
                {
                    Consume();
                    yield return EmitToken(TokenType.Colon);
                    continue;
                }
                if (next == '"')
                {
                    Consume();
                    ConsumeString();
                    if (_currentValue == '"')
                    {
                        yield return EmitToken(TokenType.String);
                        continue;
                    }
                    yield return EmitToken(TokenType.InvalidToken);
                    continue;
                }
                if (char.IsLetter(next) | next == '_')
                {
                    Consume();
                    while(TryPeek(0, out next) && (char.IsLetterOrDigit(next) || next == '_' || next == '-'))
                        Consume();

                    string currentToken = _currentToken.ToString();
                    switch (currentToken)
                    {
                        case "null":
                            yield return EmitToken(TokenType.Null, currentToken);
                            continue;
                        case "true":
                            yield return EmitToken(TokenType.True, currentToken);
                            continue;
                        case "false":
                            yield return EmitToken(TokenType.False, currentToken);
                            continue;
                        default:
                            yield return EmitToken(TokenType.Identifier, currentToken);
                            continue;
                    }
                }
                if (next == '/')
                {

                    Consume();
                    if (!TryPeek(out next))
                    {
                        yield return EmitToken(TokenType.InvalidToken);
                        break;
                    }
                    Consume();
                    if (next == '/')
                    {
                        while (TryPeek(out next) && next != '\n')
                            Consume();
                        if (next == '\n')
                            Consume();
                    }

                    if (next == '*')
                    {
                        while (true)
                        {
                            if (!TryPeek(out next))
                                break;
                            Consume();
                            if (next == '*')
                            {
                                if(!TryPeek(out next))
                                    break;
                                if (next == '/')
                                {
                                    Consume();
                                    break;
                                }
                            }
                        }
                    }

                    yield return EmitToken(TokenType.Comment);
                    continue;
                }
                throw new FormatException(string.Format("Unexpected character '{0}' at line {1} character {2}.",
                        _input[_position], _currentLine, _currentColumn));

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