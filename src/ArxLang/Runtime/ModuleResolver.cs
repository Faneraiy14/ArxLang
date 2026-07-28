using System.Text;
using ArxLang.AST;
using ArxLang.Core;
using ArxLang.Packages;

namespace ArxLang.Runtime;

// Розгортає всі "import" СТАТЕМЕНТИ рекурсивно ПЕРЕД компіляцією: читає
// імпортований .arx-файл (шлях відносно файлу, що імпортує), парсить його
// й вливає його функції/структури у спільне дерево. Захист від циклічних
// та повторних імпортів через набір уже відвіданих (повних) шляхів.
//
// Два види import:
//   import "lexer.arx"   — шлях до файлу, відносно поточного каталогу
//   import "somepkg"     — ім'я пакета БЕЗ .arx: шукається в arx_modules/,
//                          з підйомом до кореня диска (як node_modules)
public static class ModuleResolver
{
    public static ProgramNode ResolveImports(ProgramNode program, string entryFilePath)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        visited.Add(Path.GetFullPath(entryFilePath));
        var baseDir = Path.GetDirectoryName(Path.GetFullPath(entryFilePath)) ?? ".";
        return Resolve(program, baseDir, visited);
    }

    private static ProgramNode Resolve(ProgramNode program, string currentDir, HashSet<string> visited)
    {
        var merged = new ProgramNode();

        foreach (var stmt in program.Statements)
        {
            if (stmt is ImportStatement import)
            {
                string fullPath = ResolvePath(import.Path, currentDir);

                if (visited.Contains(fullPath))
                    continue; // вже імпортовано - уникаємо циклів і дублювання

                if (!File.Exists(fullPath))
                {
                    throw new Exception(
                        import.Path.EndsWith(".arx")
                            ? $"Файл модуля не знайдено: {fullPath}"
                            : $"Пакет '{import.Path}' не знайдено в arx_modules/ " +
                              $"(шукано від {currentDir} до кореня диска). " +
                              $"Встанови його: arx install <owner/repo>");
                }

                visited.Add(fullPath);

                string code = File.ReadAllText(fullPath, Encoding.UTF8);
                var lexer = new Lexer(code);
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens);
                var importedProgram = parser.ParseProgram();

                var importedDir = Path.GetDirectoryName(fullPath) ?? ".";
                var resolvedImported = Resolve(importedProgram, importedDir, visited);
                merged.Statements.AddRange(resolvedImported.Statements);
            }
            else
            {
                merged.Statements.Add(stmt);
            }
        }

        return merged;
    }

    // ".arx" у рядку import — це шлях до конкретного файлу, як і раніше.
    // Без розширення — ім'я пакета: шукаємо через PackageManager, а не як
    // буквальний файл (тому "somepkg" не намагається відкрити файл
    // "somepkg" без розширення).
    private static string ResolvePath(string importPath, string currentDir)
    {
        if (importPath.EndsWith(".arx", StringComparison.OrdinalIgnoreCase))
            return Path.GetFullPath(Path.Combine(currentDir, importPath));

        var found = PackageManager.FindPackageEntry(currentDir, importPath);
        return found != null ? Path.GetFullPath(found) : Path.GetFullPath(Path.Combine(currentDir, importPath));
    }
}
