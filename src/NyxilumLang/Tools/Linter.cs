using NyxilumLang.Core;

namespace NyxilumLang.Tools;

public class Linter
{
    private readonly List<string> _warnings = new();
    private readonly List<string> _errors = new();
    private readonly HashSet<string> _declaredVars = new();
    private readonly HashSet<string> _usedVars = new();

    public bool Lint(string sourceCode)
    {
        try
        {
            var lexer = new Lexer(sourceCode);
            var tokens = lexer.Tokenize();

            // 1. Перевірка довжини рядків
            var lines = sourceCode.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 120)
                {
                    _warnings.Add($"Рядок {i + 1} занадто довгий ({lines[i].Length} > 120)");
                }
            }

            // 2. Перевірка на порожні блоки
            if (sourceCode.Contains("{}") || sourceCode.Contains("{ }"))
            {
                _warnings.Add("Знайдено порожній блок коду");
            }

            // 3. Перевірка змінних (оголошені vs використані)
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Type == TokenType.Keyword && token.Value == "var")
                {
                    if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.Identifier)
                    {
                        _declaredVars.Add(tokens[i + 1].Value);
                    }
                }
                if (token.Type == TokenType.Identifier)
                {
                    // Перевіряємо, чи це не ключове слово
                    if (!_declaredVars.Contains(token.Value) && 
                        !new[] { "print", "readLine", "true", "false", "null" }.Contains(token.Value))
                    {
                        _usedVars.Add(token.Value);
                    }
                }
            }

            // Знаходимо невикористані змінні
            var unused = _declaredVars.Except(_usedVars);
            foreach (var varName in unused)
            {
                _warnings.Add($"Змінна '{varName}' оголошена, але не використовується");
            }

            // 4. Перевірка на зайві крапки з комою (якщо є)
            if (sourceCode.Contains(";"))
            {
                _warnings.Add("У мові Nyxilum крапка з комою не потрібна");
            }

            PrintReport();
            return _errors.Count == 0;
        }
        catch (Exception ex)
        {
            _errors.Add($"Помилка при аналізі: {ex.Message}");
            PrintReport();
            return false;
        }
    }

    private void PrintReport()
    {
        Console.WriteLine("=== LINTER REPORT ===");
        
        if (_errors.Count > 0)
        {
            Console.WriteLine($"❌ ПОМИЛКИ ({_errors.Count}):");
            foreach (var err in _errors)
                Console.WriteLine($"  {err}");
        }

        if (_warnings.Count > 0)
        {
            Console.WriteLine($"⚠️ ПОПЕРЕДЖЕННЯ ({_warnings.Count}):");
            foreach (var warn in _warnings)
                Console.WriteLine($"  {warn}");
        }

        if (_errors.Count == 0 && _warnings.Count == 0)
        {
            Console.WriteLine("✅ Код чистий!");
        }
    }
}