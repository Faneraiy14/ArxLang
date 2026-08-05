using System.Text.RegularExpressions;

namespace NyxilumLang.Runtime.Modules;

// Регулярні вирази: раніше NyxilumLang мав лише contains/startsWith/endsWith/
// split (буквальні підрядки), без жодного способу перевірити чи витягнути
// текст за шаблоном (наприклад "чи це email", "витягнути всі числа з рядка").
// Помилковий шаблон (Regex кидає ArgumentException) пропускається як
// звичайний .NET Exception — CALL_NATIVE вже ловить такі й перетворює на
// катчабельну NyxilumLang-помилку через наявний TRY_BEGIN/TRY_END механізм.
public static class RegexModule
{
    public static void Register(Dictionary<string, Func<object[], object?>> registry)
    {
        registry["regexTest"] = RegexTest;
        registry["regexMatch"] = RegexMatch;
        registry["regexFindAll"] = RegexFindAll;
        registry["regexReplace"] = RegexReplace;
    }

    private static object? RegexTest(object[] args)
        => Regex.IsMatch(args[0].ToString()!, args[1].ToString()!);

    private static object? RegexMatch(object[] args)
    {
        var m = Regex.Match(args[0].ToString()!, args[1].ToString()!);
        return m.Success ? m.Value : null;
    }

    private static object? RegexFindAll(object[] args)
    {
        var matches = Regex.Matches(args[0].ToString()!, args[1].ToString()!);
        var result = new List<object>();
        foreach (Match m in matches)
            result.Add(m.Value);
        return result;
    }

    private static object? RegexReplace(object[] args)
        => Regex.Replace(args[0].ToString()!, args[1].ToString()!, args[2].ToString()!);
}
