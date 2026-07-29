using System.Text;
using ArxLang.Core;
using ArxLang.Compiler;
using ArxLang.VM;
using ArxLang.Tools;
using ArxLang.Packages;

namespace ArxLang.Runtime;

public class ArxNode
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        if (args.Length == 0)
        {
            RunRepl();
            return;
        }

        string command = args[0];
        if (command == "--version" || command == "-v")
        {
            Console.WriteLine("ArxNode v1.0.0 (based on ArxLang v8.0)");
            return;
        }

        if (command == "format" && args.Length > 1)
        {
            RunFormat(args[1]);
            return;
        }

        if (command == "lint" && args.Length > 1)
        {
            RunLint(args[1]);
            return;
        }

        if (command == "install")
        {
            RunInstall(args.Length > 1 ? args[1] : null);
            return;
        }

        if (File.Exists(command))
        {
            RunFile(command);
        }
        else
        {
            Console.WriteLine($"Error: Cannot find file '{command}'");
            Environment.Exit(1);
        }
    }

    // "arx install"            — ставить усе з arx.json у поточній папці
    // "arx install owner/repo" — тягне конкретний пакет і дописує в arx.json
    private static void RunInstall(string? source)
    {
        try
        {
            var projectDir = Directory.GetCurrentDirectory();
            if (source == null) PackageManager.InstallAll(projectDir);
            else PackageManager.InstallSingle(source, projectDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка встановлення: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void RunFormat(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Error: Cannot find file '{path}'");
            return;
        }
        string code = File.ReadAllText(path, Encoding.UTF8);
        var formatter = new Formatter();
        Console.WriteLine(formatter.Format(code));
    }

    private static void RunLint(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Error: Cannot find file '{path}'");
            return;
        }
        string code = File.ReadAllText(path, Encoding.UTF8);
        var linter = new Linter();
        linter.Lint(code);
    }

    private static void RunFile(string path)
    {
        try
        {
            string code = File.ReadAllText(path, Encoding.UTF8);
            Execute(code, path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Runtime Error: {ex.Message}");
            // Без цього процес завершувався кодом 0 навіть після
            // необробленої помилки — жоден CI/shell-скрипт, що перевіряє
            // $?/ERRORLEVEL після запуску .arx-файлу, не міг побачити
            // провал: скрипт з throw без catch виглядав як успішний запуск.
            Environment.Exit(1);
        }
    }

    private static void RunRepl()
    {
        Console.WriteLine("ArxNode REPL v1.0.0");
        Console.WriteLine("Type 'exit()' to quit.");
        
        // We'll keep a single VM and Compiler state if possible, but for now, simple line-by-line
        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line == null || line == "exit()") break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                // REPL needs a bit of a trick: wrap in main if not present
                string code = line.Contains("func ") ? line : $"func main() {{ {line} }}";
                Execute(code);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static void Execute(string code, string? sourcePath = null)
    {
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var program = parser.ParseProgram();

        if (sourcePath != null)
            program = ModuleResolver.ResolveImports(program, sourcePath);

        var compiler = new Compiler.Compiler();
        var bytecode = compiler.Compile(program);

        // Опційний ліміт на кількість ArxLang-виділень (масиви/структури/
        // мапи) за весь запуск — захист від некерованого циклу виділень
        // у .arx-скрипті, що інакше поклав би хост-процес.
        var gcLimitEnv = Environment.GetEnvironmentVariable("ARX_GC_MAX_OBJECTS");
        if (!string.IsNullOrEmpty(gcLimitEnv) && long.TryParse(gcLimitEnv, out var gcLimit))
            ArxGc.Instance.SetLimit(gcLimit);

        var vm = new VirtualMachine(bytecode);
        vm.Run();
    }
}
