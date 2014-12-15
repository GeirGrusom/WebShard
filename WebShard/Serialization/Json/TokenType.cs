namespace WebShard.Serialization.Json
{
    public enum TokenType
    {
        Whitespace,
        Comment,
        Colon,
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
}