namespace ArxLang.Core;

public class Lexer
{
    private readonly string _source;
    private int _pos;
    private int _line = 1;
    private int _column = 1;
    private readonly List<Token> _tokens = new();
    private readonly HashSet<string> _keywords = new()
    {
        "func", "var", "if", "else", "while", "for", "return",
        "break", "continue",
        "true", "false", "null", "print", "in",
        "struct", "self", "import",
        "try", "catch", "throw",
        "readLine", "readInt", "readDouble",
        "readFile", "writeFile", "appendFile", "fileExists",
        "sqrt", "abs", "pow", "sin", "cos", "tan",
        "round", "floor", "ceil", "max", "min"
    };

    public Lexer(string source) { _source = source; }

    public List<Token> Tokenize()
    {
        while (_pos < _source.Length)
        {
            char c = _source[_pos];

            if (char.IsWhiteSpace(c))
            {
                if (c == '\n') { _line++; _column = 1; }
                else { _column++; }
                _pos++;
                continue;
            }

            if (c == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '/')
            {
                while (_pos < _source.Length && _source[_pos] != '\n') _pos++;
                continue;
            }

            if (c == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '*')
            {
                _pos += 2;
                while (_pos + 1 < _source.Length && !(_source[_pos] == '*' && _source[_pos + 1] == '/')) _pos++;
                _pos += 2;
                continue;
            }

            if (char.IsDigit(c))
            {
                string num = "";
                while (_pos < _source.Length && char.IsDigit(_source[_pos]))
                {
                    num += _source[_pos];
                    _pos++;
                }
                if (_pos < _source.Length && _source[_pos] == '.')
                {
                    if (_pos + 1 < _source.Length && _source[_pos + 1] == '.')
                    {
                        _tokens.Add(new Token(TokenType.Number, num, _line, _column));
                        _column += num.Length;
                        continue;
                    }
                    num += ".";
                    _pos++;
                    while (_pos < _source.Length && char.IsDigit(_source[_pos]))
                    {
                        num += _source[_pos];
                        _pos++;
                    }
                }
                _tokens.Add(new Token(TokenType.Number, num, _line, _column));
                _column += num.Length;
                continue;
            }

            if (c == '.')
            {
                if (_pos + 1 < _source.Length && _source[_pos + 1] == '.')
                {
                    _tokens.Add(new Token(TokenType.Operator, "..", _line, _column));
                    _pos += 2;
                    _column += 2;
                    continue;
                }
                _tokens.Add(new Token(TokenType.Punctuation, ".", _line, _column));
                _pos++;
                _column++;
                continue;
            }

            if (c == '"')
            {
                _pos++;
                string str = "";
                while (_pos < _source.Length && _source[_pos] != '"')
                {
                    if (_source[_pos] == '\\' && _pos + 1 < _source.Length)
                    {
                        char escaped = _source[_pos + 1] switch
                        {
                            'n' => '\n',
                            't' => '\t',
                            'r' => '\r',
                            '0' => '\0',
                            '"' => '"',
                            '\\' => '\\',
                            var other => other
                        };
                        str += escaped;
                        _pos += 2;
                    }
                    else
                    {
                        str += _source[_pos];
                        _pos++;
                    }
                }
                _pos++;
                _tokens.Add(new Token(TokenType.String, str, _line, _column));
                _column += str.Length + 2;
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                string word = "";
                while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '_'))
                {
                    word += _source[_pos];
                    _pos++;
                }

                TokenType type = TokenType.Identifier;
                if (_keywords.Contains(word))
                {
                    type = word == "true" || word == "false" ? TokenType.Boolean : TokenType.Keyword;
                }
                _tokens.Add(new Token(type, word, _line, _column));
                _column += word.Length;
                continue;
            }

            string op = "";
            if (c == '-' && _pos + 1 < _source.Length && _source[_pos + 1] == '>')
            {
                op = "->";
                _pos += 2;
            }
            else if (c == '=' && _pos + 1 < _source.Length && _source[_pos + 1] == '=')
            {
                op = "==";
                _pos += 2;
            }
            else if (c == '!' && _pos + 1 < _source.Length && _source[_pos + 1] == '=')
            {
                op = "!=";
                _pos += 2;
            }
            else if (c == '>' && _pos + 1 < _source.Length && _source[_pos + 1] == '=')
            {
                op = ">=";
                _pos += 2;
            }
            else if (c == '<' && _pos + 1 < _source.Length && _source[_pos + 1] == '=')
            {
                op = "<=";
                _pos += 2;
            }
            else if (c == '&' && _pos + 1 < _source.Length && _source[_pos + 1] == '&')
            {
                op = "&&";
                _pos += 2;
            }
            else if (c == '|' && _pos + 1 < _source.Length && _source[_pos + 1] == '|')
            {
                op = "||";
                _pos += 2;
            }
            else if (c == '+' && _pos + 1 < _source.Length && _source[_pos + 1] == '+')
            {
                op = "++";
                _pos += 2;
            }
            else if (c == '-' && _pos + 1 < _source.Length && _source[_pos + 1] == '-')
            {
                op = "--";
                _pos += 2;
            }
            else if (c == '+' && _pos + 1 < _source.Length && _source[_pos + 1] == '=')
            {
                op = "+=";
                _pos += 2;
            }
            else if (c == '-' && _pos + 1 < _source.Length && _source[_pos + 1] == '=')
            {
                op = "-=";
                _pos += 2;
            }
            else if (c == '*' && _pos + 1 < _source.Length && _source[_pos + 1] == '=')
            {
                op = "*=";
                _pos += 2;
            }
            else if (c == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '=')
            {
                op = "/=";
                _pos += 2;
            }
            else if ("+-*/%=!<>|&".Contains(c))
            {
                op = c.ToString();
                _pos++;
            }
            else if ("(){}[],;:".Contains(c))
            {
                op = c.ToString();
                _pos++;
                _tokens.Add(new Token(TokenType.Punctuation, op, _line, _column));
                _column++;
                continue;
            }
            else
            {
                throw new Exception($"Невідомий символ '{c}' на рядку {_line}, стовпець {_column}");
            }

            if (!string.IsNullOrEmpty(op))
            {
                _tokens.Add(new Token(TokenType.Operator, op, _line, _column));
                _column += op.Length;
            }
        }

        _tokens.Add(new Token(TokenType.EOF, "", _line, _column));
        return _tokens;
    }
}