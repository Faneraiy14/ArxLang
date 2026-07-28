using System.Text;
using ArxLang.AST;
using ArxLang.Core;

namespace ArxLang.Runtime;

// Розгортає всі "import" СТАТЕМЕНТИ рекурсивно ПЕРЕД компіляцією: читає
// імпортований .arx-файл (шлях відносно файлу, що імпортує), парсить його
// й вливає його функції/структури у спільне дерево. Захист від циклічних
// та повторних імпортів через набір уже відвіданих (повних) шляхів.
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
                string fullPath = Path.GetFullPath(Path.Combine(currentDir, import.Path));

                if (visited.Contains(fullPath))
                    continue; // вже імпортовано - уникаємо циклів і дублювання

                if (!File.Exists(fullPath))
                    throw new Exception($"Файл модуля не знайдено: {fullPath}");

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
}
