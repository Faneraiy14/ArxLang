using ArxLang.Core;
using ArxLang.Compiler;
using ArxLang.VM;
using System.Text;

namespace ArxLang;

class Program
{
    static void OldMain(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("╔═══════════════════════════════════════╗");
        Console.WriteLine("║        ARX LANGUAGE v8.0            ║");
        Console.WriteLine("║   Структури • Методи • Масиви      ║");
        Console.WriteLine("║   Файли • Математика • Рядки        ║");
        Console.WriteLine("╚═══════════════════════════════════════╝\n");

        string code = "";
        bool runFromArgs = false;

        if (args.Length > 0)
        {
            string filePath = args[0];
            if (File.Exists(filePath))
            {
                code = File.ReadAllText(filePath, Encoding.UTF8);
                runFromArgs = true;
                Console.WriteLine($"Running file: {filePath}");
            }
            else
            {
                Console.WriteLine($"Error: File not found: {filePath}");
                return;
            }
        }
        else
        {
            code = @"
func main() {
    // ===== ВВІД =====
    print(""Введіть ваше ім'я: "")
    var inputName = readLine()
    print(""Привіт, "" + inputName + ""!"")

    print(""Введіть число: "")
    var num = readInt()
    print(""Квадрат числа: "" + num * num)

    // ===== МАТЕМАТИКА =====
    var x = 16
    print(""sqrt(16) = "" + sqrt(x))
    print(""abs(-5) = "" + abs(-5))
    print(""pow(2, 8) = "" + pow(2, 8))
    print(""sin(pi/2) = "" + sin(3.14159/2))

    // ===== ФАЙЛИ =====
    print(""Запис у файл test.txt..."")
    writeFile(""test.txt"", ""Привіт з ARX!"")

    var exists = fileExists(""test.txt"")
    if (exists) {
        var content = readFile(""test.txt"")
        print(""Файл містить: "" + content)
    } else {
        print(""Файл не знайдено"")
    }

    // ===== ЗВИЧАЙНІ РЕЧІ =====
    var name = ""Святослав""
    var age = 14
    if (age >= 18) {
        print(""Дорослий"")
    } else {
        print(""Юний геній!"")
    }

    var counter = 0
    while (counter < 3) {
        print(counter)
        counter = counter + 1
    }

    for i in 0..5 {
        print(i)
    }

    var arr = [10, 20, 30, 40, 50]
    print(arr[0])
    print(arr[2])
    print(arr[4])

    print(""Привіт, "" + name + ""! Твій вік: "" + age)
    print(""Число Пі: "" + 3.14159)

    func add(a, b) {
        return a + b
    }
    var sum = add(age, 10)
    print(""Сума: "" + sum)
}
";
        }

        Console.WriteLine("=== ВИХІДНИЙ КОД ===");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine(code);
        Console.WriteLine(new string('-', 50));

        try
        {
            Console.WriteLine("\n🔹 ЛЕКСИЧНИЙ АНАЛІЗ...");
            var lexer = new Lexer(code);
            var tokens = lexer.Tokenize();
            Console.WriteLine($"✅ {tokens.Count} токенів");

            Console.WriteLine("\n🔹 СИНТАКСИЧНИЙ АНАЛІЗ...");
            var parser = new Parser(tokens);
            var program = parser.ParseProgram();
            Console.WriteLine("✅ AST побудовано");

            Console.WriteLine("\n🔹 КОМПІЛЯЦІЯ...");
            var compiler = new Compiler.Compiler();
            var bytecode = compiler.Compile(program);
            Console.WriteLine($"✅ {bytecode.Code.Count} інструкцій");

            Console.WriteLine("\n🔹 ВИКОНАННЯ...");
            var vm = new VirtualMachine(bytecode);
            vm.Run();
            Console.WriteLine("\n✅ Програма виконана успішно!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ПОМИЛКА: {ex.Message}");
            Console.WriteLine($"\n📌 STACK TRACE:\n{ex.StackTrace}");
        }

        if (!runFromArgs)
        {
            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }
    }
}
