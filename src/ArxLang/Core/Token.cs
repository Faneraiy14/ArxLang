namespace ArxLang.Core;

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }
    public int Line { get; }
    public int Column { get; }

    public Token(TokenType type, string value, int line, int column)
    {
        Type = type;
        Value = value;
        Line = line;
        Column = column;
    }

    public override string ToString() => $"Token({Type}, '{Value}', {Line}:{Column})";
}

public enum TokenType
{
    Keyword,
    Identifier,
    Number,
    String,
    Boolean,
    Operator,
    Punctuation,
    EOF
}