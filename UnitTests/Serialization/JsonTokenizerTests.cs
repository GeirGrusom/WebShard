using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using WebShard.Serialization;

namespace UnitTests.Serialization
{
    [TestFixture]
    public class JsonTokenizerTests
    {
        [TestCase(TokenType.LeftBrace, "{")]
        [TestCase(TokenType.RightBrace, "}")]
        [TestCase(TokenType.LeftBracket, "[")]
        [TestCase(TokenType.RightBracket, "]")]
        [TestCase(TokenType.Comma, ",")]
        [TestCase(TokenType.Null, "null")]
        [TestCase(TokenType.False, "false")]
        [TestCase(TokenType.True, "true")]
        [TestCase(TokenType.Number, "10")]
        [TestCase(TokenType.Number, "10.01")]
        [TestCase(TokenType.Number, "0.01")]
        [TestCase(TokenType.Number, ".01")]
        [TestCase(TokenType.Whitespace, " ")]
        [TestCase(TokenType.Whitespace, "\t")]
        [TestCase(TokenType.Whitespace, "\r")]
        [TestCase(TokenType.Whitespace, "\n")]
        [TestCase(TokenType.Comment, "/* Foo bar */")]
        [TestCase(TokenType.Comment, "// Foo bar")]
        public void Tokenize_SingleTokens_Ok(TokenType expectedTokenType, string value)
        {
            // Aarrange
            var tokenizer = new JsonTokenizer(value);

            // Act
            var token = tokenizer.Tokenize().Single();

            // Assert
            Assert.That(token.Type, Is.EqualTo(expectedTokenType));
            Assert.That(token.Value, Is.EqualTo(value));
        }

        [Test]
        public void TwoTokens_LastIsRegex()
        {
            // Arrange
            var tokenizer = new JsonTokenizer("123// Foo bar");

            // Act
            var results = tokenizer.Tokenize().ToArray();

            // Assert
            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results.Select(r => r.Value), Is.EquivalentTo(new [] { "123", "// Foo bar" }));
        }}
}
