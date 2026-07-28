# ArxLang

Мова програмування власної розробки: компілятор у байткод, стекова віртуальна
машина та стандартна бібліотека з 71 вбудованої функції — від математики й
рядків до HTTP, графіки та вводу з клавіатури.

**Мова самохостована**: у `selfhosted/` лежить інтерпретатор ArxLang,
написаний **мовою ArxLang**, який виконує програми з рекурсією, структурами,
методами, масивами та замиканнями.

```arx
func factorial(n) {
    if (n <= 1) { return 1 }
    return n * factorial(n - 1)
}

func makeCounter() {
    var count = 0
    return func() {
        count = count + 1
        return count
    }
}

func main() {
    print(factorial(6))        // 720

    var counter = makeCounter()
    print(counter())           // 1
    print(counter())           // 2
}
```

## Швидкий старт

```bash
dotnet build src/ArxLang
```

Запуск програми:

```bash
src/ArxLang/bin/Debug/net10.0-windows/ArxLang.exe myprogram.arx
```

Щоб не писати повний шлях щоразу, див. [Команда arx](#команда-arx).

## Що вміє мова

| Можливість | Приклад |
|---|---|
| Змінні, `null` | `var x = 10`, `var n = null` |
| Функції, рекурсія | `func add(a, b) { return a + b }` |
| Замикання | `var f = func() { return count }` |
| Функції як значення | `var op = sqrt`, `sort(arr, cmp)` |
| Структури й методи | `struct Point { x, y }`, `func Point.len() {...}` |
| Мапи | `newMap()`, `mapSet`, `mapGet`, `mapKeys` |
| Масиви | `[1, 2, 3]`, `arr[0]`, `append(arr, 4)` |
| Цикли | `for i in 0..10`, `for x in arr`, `while` |
| `break` / `continue` | працюють у всіх циклах, зокрема вкладених |
| Кирилиця в іменах | `func привітати(імя) { ... }` |
| Помилки | `try { ... } catch (e) { ... }`, `throw` |
| Модулі | `import "helpers.arx"` |
| Вищий порядок | `mapArr`, `filter`, `reduce`, `sort` |

Стандартна бібліотека охоплює математику, рядки, масиви, мапи, JSON, файли,
час, HTTP-запити, 2D-графіку на канвасі та зчитування клавіш.

Повний опис синтаксису — у [GUIDE.md](GUIDE.md).

## Як це влаштовано

```
Вихідний код (.arx)
      │
   Lexer.cs        токенізація
      │
   Parser.cs       рекурсивний спуск -> AST
      │
   Compiler.cs     AST -> байткод (58 опкодів)
      │
VirtualMachine.cs  стекова VM виконує байткод
```

Це **не** дерево-обхідний інтерпретатор: програма спочатку компілюється
в байткод, а вже його виконує VM зі стеком операндів, стеком фреймів
локальних змінних і власним стеком обробників `try/catch`.

```
src/ArxLang/
  Core/       Lexer.cs, Parser.cs, Token.cs
  AST/        вузли дерева
  Compiler/   Compiler.cs, Bytecode.cs
  VM/         VirtualMachine.cs — виконання + 71 нативна функція
  Runtime/    ArxMap, ArxJson, ArxFunctionRef, модулі Http/Os/Graphics
  Tools/      Formatter.cs, Linter.cs

selfhosted/   інтерпретатор ArxLang, написаний на ArxLang
bootstrap/    ранній мінімальний самохост
tests/        19 тестів + run_all.sh
programs/     приклади (arxnode_dashboard — системний дашборд)
```

## Самохостинг

`selfhosted/` — доказ того, що мова достатньо повна, щоб описати саму себе:

| Файл | Що робить |
|---|---|
| `lexer.arx` | токенізація |
| `parser.arx` | рекурсивний спуск, AST у вигляді мап |
| `interpreter.arx` | обхід AST, середовища-ланцюжки, замикання |
| `main.arx` | точка входу, запускає гостьову програму |

```bash
cd selfhosted
../src/ArxLang/bin/Debug/net10.0-windows/ArxLang.exe main.arx
```

Очікуваний вивід: `720 / 5 / 100 / 30 / 1 2 3 / 256 / ПРИВІТ` — рекурсія,
структура з методом, масиви, індексація, лічильник на замиканні з мутацією
захопленого стану, нативний виклик і робота з рядками.

Два цікаві рішення всередині:

- **Середовища — ланцюжок мап** `{__vars, __parent}`. Оскільки мапа
  посилальна, замикання ділять один мутабельний стан — саме тому лічильник
  справді рахує, а не повертає одиницю щоразу.
- **`return` реалізований через `throw`/`try`**: значення загортається
  в маркер `__isReturn` і піднімається до найближчого виклику функції.
  Маркер критичний — без нього `catch` ловив би й справжні помилки,
  тихо перетворюючи їх на результат функції.

## Тести

```bash
bash tests/run_all.sh
```

19 тестів: рекурсія, замикання, фрейми, мапи, методи, стандартна бібліотека,
`try/catch`, модулі, самохостинг. Графічні тести пропускаються автоматично —
вони відкривають вікна.

Тести, де помилка є **очікуваним** результатом (наприклад необроблений
`throw`), перелічені в `EXPECT_ERROR` усередині скрипта: для них провалом
вважається якраз відсутність помилки.

## Команда arx

Щоб запускати `.arx` з будь-якої папки, додай теку зі збіркою в `PATH`
і створи короткий псевдонім. У PowerShell:

```powershell
$exe = "C:\Projects\ArxEcosystem\src\ArxLang\bin\Debug\net10.0-windows\ArxLang.exe"
New-Item -ItemType Directory -Force "$HOME\bin" | Out-Null
Set-Content "$HOME\bin\arx.cmd" "@echo off`r`n`"$exe`" %*" -Encoding ascii
[Environment]::SetEnvironmentVariable("PATH", "$env:PATH;$HOME\bin", "User")
```

Після перезапуску терміналу:

```bash
arx myprogram.arx
```

## Відомі обмеження

- Не можна викликати результат виклику напряму: замість `f()()` потрібна
  проміжна змінна.
- Немає імпорту за іменем — `import` підключає весь файл у глобальну область.
- Немає менеджера пакетів: `arx install` поки не існує, залежності
  підключаються через `import` з відносним шляхом.
- Цільова платформа — Windows: VM використовує Windows Forms для GUI
  та графіки.

## Ліцензія

Приватний проєкт. Автор — Faneraiy14.
