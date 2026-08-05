using System.Linq;
using System.Text;
using NyxilumLang.AST;
using NyxilumLang.Core;
using NyxilumLang.Packages;

namespace NyxilumLang.Runtime;

// Розгортає всі "import" СТАТЕМЕНТИ рекурсивно ПЕРЕД компіляцією: читає
// імпортований .nx-файл (шлях відносно файлу, що імпортує), парсить його
// й вливає його функції/структури у спільне дерево. Захист від циклічних
// та повторних імпортів через набір уже відвіданих (повних) шляхів.
//
// Два види import:
//   import "lexer.nx"   — шлях до файлу, відносно поточного каталогу
//   import "somepkg"     — ім'я пакета БЕЗ .nx: шукається в nx_modules/,
//                          з підйомом до кореня диска (як node_modules)
// Обидва підтримують вибірковий варіант: import "lexer.nx" { Token, scan }
// — вливає лише перелічені функції/структури/глобальні змінні (методи
// названої структури підтягуються автоматично). Відсутнє ім'я зі списку -
// помилка одразу при резолві imports, ще до компіляції.
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
                        import.Path.EndsWith(".nx")
                            ? $"Файл модуля не знайдено: {fullPath}"
                            : $"Пакет '{import.Path}' не знайдено в nx_modules/ " +
                              $"(шукано від {currentDir} до кореня диска). " +
                              $"Встанови його: nx install <owner/repo>");
                }

                visited.Add(fullPath);

                string code = File.ReadAllText(fullPath, Encoding.UTF8);
                var lexer = new Lexer(code);
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens);
                var importedProgram = parser.ParseProgram();

                var importedDir = Path.GetDirectoryName(fullPath) ?? ".";
                var resolvedImported = Resolve(importedProgram, importedDir, visited);
                var toMerge = import.Names != null
                    ? FilterByNames(resolvedImported.Statements, import.Names, import.Path)
                    : resolvedImported.Statements;
                merged.Statements.AddRange(toMerge);
            }
            else
            {
                merged.Statements.Add(stmt);
            }
        }

        return merged;
    }

    // Вибірковий import "file.nx" { a, b }: лишає лише запитані функції/
    // структури/глобальні змінні (за іменем), плюс методи запитаної
    // структури (func Struct.method — повне ім'я "Struct.method", метод
    // без структури в списку не втягується сам по собі), плюс БУДЬ-ЯКУ
    // функцію з іменем на "_" — приватний хелпер модуля (конвенція, як
    // Python _private): дописана публічна функція, яка сама викликає такий
    // хелпер (lib/discord.nx: dPollLoop -> _dIdentify/_dHeartbeat), інакше
    // ламалась би при вибірковому імпорті лише публічного імені — ніхто ж
    // не буде (і не повинен) перелічувати "_dIdentify" у списку import.
    private static List<StatementNode> FilterByNames(List<StatementNode> statements, List<string> names, string importPath)
    {
        var wanted = new HashSet<string>(names, StringComparer.Ordinal);
        var found = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<StatementNode>();

        foreach (var stmt in statements)
        {
            string? declName = stmt switch
            {
                FunctionDeclaration f => f.Name,
                StructDeclaration s => s.Name,
                VariableDeclaration v => v.Name,
                _ => null,
            };

            if (declName != null && declName.StartsWith('_'))
            {
                result.Add(stmt);
                continue;
            }

            if (declName != null && wanted.Contains(declName))
            {
                found.Add(declName);
                result.Add(stmt);
                continue;
            }

            if (stmt is FunctionDeclaration method && method.Name.Contains('.'))
            {
                var structName = method.Name[..method.Name.IndexOf('.')];
                if (wanted.Contains(structName))
                {
                    result.Add(stmt);
                    continue;
                }
            }
        }

        var missing = wanted.Except(found).ToList();
        if (missing.Count > 0)
        {
            throw new Exception(
                $"Вибірковий import з \"{importPath}\" не знайшов: {string.Join(", ", missing)}");
        }

        return result;
    }

    // ".nx" у рядку import — це шлях до конкретного файлу, як і раніше.
    // Без розширення — ім'я пакета: шукаємо через PackageManager, а не як
    // буквальний файл (тому "somepkg" не намагається відкрити файл
    // "somepkg" без розширення).
    private static string ResolvePath(string importPath, string currentDir)
    {
        if (importPath.EndsWith(".nx", StringComparison.OrdinalIgnoreCase))
            return Path.GetFullPath(Path.Combine(currentDir, importPath));

        var found = PackageManager.FindPackageEntry(currentDir, importPath);
        return found != null ? Path.GetFullPath(found) : Path.GetFullPath(Path.Combine(currentDir, importPath));
    }
}
