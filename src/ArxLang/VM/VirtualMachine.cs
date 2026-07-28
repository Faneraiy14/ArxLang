using ArxLang.Compiler;
using ArxLang.Runtime;
using System.Windows.Forms;
using System.Drawing;

namespace ArxLang.VM;

// Кидається інструкцією THROW, коли немає жодного активного try/catch —
// піднімається аж до ArxNode.cs і показується як звичайна Runtime Error.
public class ArxThrowException : Exception
{
    public ArxThrowException(string message) : base(message) { }
}

public class VirtualMachine
{
    private readonly byte[] _code;
    private readonly List<object> _constants;
    private readonly Stack<object> _stack = new();
    // ВИПРАВЛЕНО: раніше всі змінні (параметри й локальні) лежали в ОДНОМУ
    // спільному словнику на всю програму, а номер слота призначався один раз
    // на етапі компіляції. Рекурсивний виклик функції використовував ті самі
    // слоти, що й батьківський виклик, — локальна змінна могла тихо
    // затертись вкладеним викликом. Тепер кожен виклик отримує власний,
    // окремий фрейм змінних; _frameStack зберігає фрейми викликачів.
    private Dictionary<int, object> _currentFrame = new();
    private readonly Stack<Dictionary<int, object>> _frameStack = new();
    private readonly Stack<int> _callStack = new();

    // try/catch: кожен активний try запам'ятовує, куди стрибнути і до якої
    // глибини стеку/фреймів/викликів відкотитись, якщо всередині нього
    // станеться помилка (як явний throw, так і внутрішня помилка VM).
    private class ExceptionHandler
    {
        public int CatchAddr;
        public int VarSlot;
        public int StackDepth;
        public int CallStackDepth;
        public int FrameStackDepth;
    }
    private readonly Stack<ExceptionHandler> _handlers = new();

    private void UnwindToHandler(object? errorValue)
    {
        var handler = _handlers.Pop();
        while (_stack.Count > handler.StackDepth) _stack.Pop();
        while (_callStack.Count > handler.CallStackDepth) _callStack.Pop();
        while (_frameStack.Count > handler.FrameStackDepth) _currentFrame = _frameStack.Pop();
        _currentFrame[handler.VarSlot] = errorValue ?? "";
        _ip = handler.CatchAddr;
    }
    private readonly Dictionary<string, int> _functionAddresses;
    private int _ip;
    private static readonly Random _random = new Random();
    public static readonly Dictionary<string, Func<object[], object?>> _nativeFunctions = new();

    static VirtualMachine()
    {
        // Math
        _nativeFunctions["abs"] = args => Math.Abs(PopNumVal(args[0]));
        _nativeFunctions["sqrt"] = args => Math.Sqrt(PopNumVal(args[0]));
        _nativeFunctions["sin"] = args => Math.Sin(PopNumVal(args[0]));
        _nativeFunctions["cos"] = args => Math.Cos(PopNumVal(args[0]));
        _nativeFunctions["tan"] = args => Math.Tan(PopNumVal(args[0]));
        _nativeFunctions["round"] = args => Math.Round(PopNumVal(args[0]));
        _nativeFunctions["floor"] = args => Math.Floor(PopNumVal(args[0]));
        _nativeFunctions["ceil"] = args => Math.Ceiling(PopNumVal(args[0]));
        _nativeFunctions["pow"] = args => Math.Pow(PopNumVal(args[0]), PopNumVal(args[1]));
        _nativeFunctions["max"] = args => Math.Max(PopNumVal(args[0]), PopNumVal(args[1]));
        _nativeFunctions["min"] = args => Math.Min(PopNumVal(args[0]), PopNumVal(args[1]));
        _nativeFunctions["clamp"] = args => Math.Max(PopNumVal(args[1]), Math.Min(PopNumVal(args[2]), PopNumVal(args[0])));
        
        // Random
        _nativeFunctions["randomInt"] = args => _random.Next(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]) + 1);
        _nativeFunctions["randomDouble"] = args => PopNumVal(args[0]) + (_random.NextDouble() * (PopNumVal(args[1]) - PopNumVal(args[0])));

        // Conversions & Types
        _nativeFunctions["toString"] = args => args[0]?.ToString() ?? "null";
        _nativeFunctions["toInt"] = args => Convert.ToInt32(args[0]);
        _nativeFunctions["toDouble"] = args => Convert.ToDouble(args[0]);
        _nativeFunctions["typeOf"] = args => args[0] switch {
            List<object> => "array",
            ArxMap => "map",
            ArxFunctionRef => "function",
            Dictionary<string, object> => "struct",
            string => "string",
            bool => "bool",
            double => "number",
            int => "number",
            null => "null",
            _ => args[0].GetType().Name
        };
        _nativeFunctions["isNumber"] = args => args[0] is int || args[0] is double || args[0] is float || args[0] is long;
        _nativeFunctions["isString"] = args => args[0] is string;
        _nativeFunctions["isArray"] = args => args[0] is List<object>;
        _nativeFunctions["isBool"] = args => args[0] is bool;
        _nativeFunctions["isNull"] = args => args[0] == null;

        // Символи
        _nativeFunctions["charCode"] = args => {
            string s = args[0]?.ToString() ?? "";
            if (s.Length == 0) throw new Exception("charCode: очікується непорожній рядок");
            return (double)s[0];
        };
        _nativeFunctions["fromCharCode"] = args => ((char)Convert.ToInt32(args[0])).ToString();

        // Мапи/словники (окремий тип від struct)
        _nativeFunctions["newMap"] = args => new ArxMap();
        _nativeFunctions["mapSet"] = args => {
            var map = (ArxMap)args[0];
            map.Entries[args[1]] = args[2];
            return null;
        };
        _nativeFunctions["mapGet"] = args => {
            var map = (ArxMap)args[0];
            return map.Entries.TryGetValue(args[1], out var v) ? v : null;
        };
        _nativeFunctions["mapHas"] = args => {
            var map = (ArxMap)args[0];
            return map.Entries.ContainsKey(args[1]);
        };
        _nativeFunctions["mapRemove"] = args => {
            var map = (ArxMap)args[0];
            return map.Entries.Remove(args[1]);
        };
        _nativeFunctions["mapKeys"] = args => {
            var map = (ArxMap)args[0];
            return map.Entries.Keys.ToList();
        };
        _nativeFunctions["mapValues"] = args => {
            var map = (ArxMap)args[0];
            return map.Entries.Values.ToList();
        };

        // Функції вищого порядку над масивами (потребують функцію-значення з Фази 4)
        _nativeFunctions["sort"] = args => {
            var list = new List<object>((List<object>)args[0]);
            var funcRef = (ArxFunctionRef)args[1];
            var vm = Current!;
            list.Sort((a, b) => Convert.ToInt32(Convert.ToDouble(vm.InvokeFunctionValue(funcRef, new object[] { a, b }))));
            return list;
        };
        _nativeFunctions["mapArr"] = args => {
            var list = (List<object>)args[0];
            var funcRef = (ArxFunctionRef)args[1];
            var vm = Current!;
            var result = new List<object>();
            foreach (var item in list) result.Add(vm.InvokeFunctionValue(funcRef, new object[] { item })!);
            return result;
        };
        _nativeFunctions["filter"] = args => {
            var list = (List<object>)args[0];
            var funcRef = (ArxFunctionRef)args[1];
            var vm = Current!;
            var result = new List<object>();
            foreach (var item in list)
                if (Convert.ToBoolean(vm.InvokeFunctionValue(funcRef, new object[] { item })))
                    result.Add(item);
            return result;
        };
        _nativeFunctions["reduce"] = args => {
            var list = (List<object>)args[0];
            var funcRef = (ArxFunctionRef)args[1];
            object acc = args[2];
            var vm = Current!;
            foreach (var item in list)
                acc = vm.InvokeFunctionValue(funcRef, new object[] { acc, item })!;
            return acc;
        };

        // Виклик функції-значення з масивом аргументів (аналог "apply") -
        // потрібно, коли кількість аргументів невідома на етапі компіляції
        // (напр. узагальнена передача виклику до нативної функції).
        _nativeFunctions["callWithArgs"] = args => {
            var funcRef = (ArxFunctionRef)args[0];
            var argList = (List<object>)args[1];
            return Current!.InvokeFunctionValue(funcRef, argList.ToArray());
        };

        // JSON
        _nativeFunctions["toJson"] = args => ArxJson.Serialize(args[0]);
        _nativeFunctions["fromJson"] = args => ArxJson.Deserialize(args[0]?.ToString() ?? "");

        // Strings & length
        _nativeFunctions["len"] = args => {
            if (args[0] is List<object> arr) return (double)arr.Count;
            if (args[0] is string s) return (double)s.Length;
            if (args[0] is Dictionary<string, object> d) return (double)d.Count;
            if (args[0] is ArxMap m) return (double)m.Entries.Count;
            return 0.0;
        };
        _nativeFunctions["substring"] = args => {
            string s = args[0]?.ToString() ?? "";
            int start = Convert.ToInt32(args[1]);
            int len = args.Length > 2 ? Convert.ToInt32(args[2]) : s.Length - start;
            if (start < 0) start = 0;
            if (len < 0) len = s.Length - start;
            if (start + len > s.Length) len = s.Length - start;
            return s.Substring(start, len);
        };
        _nativeFunctions["replace"] = args => (args[0]?.ToString() ?? "").Replace(args[1]?.ToString() ?? "", args[2]?.ToString() ?? "");
        _nativeFunctions["toUpper"] = args => (args[0]?.ToString() ?? "").ToUpper();
        _nativeFunctions["toLower"] = args => (args[0]?.ToString() ?? "").ToLower();
        _nativeFunctions["contains"] = args => (args[0]?.ToString() ?? "").Contains(args[1]?.ToString() ?? "");
        _nativeFunctions["startsWith"] = args => (args[0]?.ToString() ?? "").StartsWith(args[1]?.ToString() ?? "");
        _nativeFunctions["endsWith"] = args => (args[0]?.ToString() ?? "").EndsWith(args[1]?.ToString() ?? "");
        _nativeFunctions["split"] = args => {
            string s = args[0]?.ToString() ?? "";
            string sep = args[1]?.ToString() ?? "";
            var parts = s.Split(new[] { sep }, StringSplitOptions.None);
            return parts.Select(p => (object)p).ToList();
        };
        _nativeFunctions["join"] = args => {
            var list = (List<object>)args[0];
            string sep = args[1]?.ToString() ?? "";
            return string.Join(sep, list);
        };

        // Arrays
        _nativeFunctions["append"] = args => {
            var list = (List<object>)args[0];
            list.Add(args[1]);
            return null;
        };
        _nativeFunctions["pop"] = args => {
            var list = (List<object>)args[0];
            if (list.Count == 0) return null;
            var val = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return val;
        };
        _nativeFunctions["removeAt"] = args => {
            var list = (List<object>)args[0];
            int idx = Convert.ToInt32(args[1]);
            list.RemoveAt(idx);
            return null;
        };
        _nativeFunctions["insert"] = args => {
            var list = (List<object>)args[0];
            int idx = Convert.ToInt32(args[1]);
            list.Insert(idx, args[2]);
            return null;
        };
        _nativeFunctions["clear"] = args => {
            var list = (List<object>)args[0];
            list.Clear();
            return null;
        };

        // File I/O
        _nativeFunctions["readFile"] = args => File.ReadAllText(args[0]?.ToString() ?? "");
        _nativeFunctions["writeFile"] = args => {
            File.WriteAllText(args[0]?.ToString() ?? "", args[1]?.ToString() ?? "");
            return true;
        };
        _nativeFunctions["appendFile"] = args => {
            File.AppendAllText(args[0]?.ToString() ?? "", args[1]?.ToString() ?? "");
            return true;
        };
        _nativeFunctions["fileExists"] = args => File.Exists(args[0]?.ToString() ?? "");
        _nativeFunctions["readLines"] = args => File.ReadAllLines(args[0]?.ToString() ?? "").Select(l => (object)l).ToList();
        _nativeFunctions["printNoNewLine"] = args => {
            Console.Write(args[0]);
            return null;
        };

        // System / Time
        _nativeFunctions["now"] = args => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _nativeFunctions["today"] = args => DateTime.Now.ToString("yyyy-MM-dd");
        _nativeFunctions["timestamp"] = args => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _nativeFunctions["sleep"] = args => {
            System.Threading.Thread.Sleep(Convert.ToInt32(args[0]));
            return null;
        };

        // GUI
        _nativeFunctions["guiWindow"] = args => {
            var form = new Form {
                Text = args[0].ToString(),
                Width = Convert.ToInt32(args[1]),
                Height = Convert.ToInt32(args[2]),
                StartPosition = FormStartPosition.CenterScreen
            };
            return form;
        };
        _nativeFunctions["guiButton"] = args => {
            var btn = new Button {
                Text = args[0].ToString(),
                Left = Convert.ToInt32(args[1]),
                Top = Convert.ToInt32(args[2]),
                Width = Convert.ToInt32(args[3]),
                Height = Convert.ToInt32(args[4])
            };
            return btn;
        };
        _nativeFunctions["guiLabel"] = args => {
            var label = new Label {
                Text = args[0].ToString(),
                Left = Convert.ToInt32(args[1]),
                Top = Convert.ToInt32(args[2]),
                Width = Convert.ToInt32(args[3]),
                Height = Convert.ToInt32(args[4]),
                Font = new Font("Arial", 12)
            };
            return label;
        };
        _nativeFunctions["guiTextBox"] = args => {
            var tb = new TextBox {
                Left = Convert.ToInt32(args[0]),
                Top = Convert.ToInt32(args[1]),
                Width = Convert.ToInt32(args[2]),
                Height = Convert.ToInt32(args[3]),
                Font = new Font("Arial", 14),
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Right
            };
            return tb;
        };
        _nativeFunctions["guiAdd"] = args => {
            var parent = (Control)args[0];
            var child = (Control)args[1];
            parent.Controls.Add(child);
            return null;
        };
        _nativeFunctions["guiOnAction"] = args => {
            var control = (Control)args[0];
            var funcName = args[1].ToString();
            
            if (control is Button btn) {
                btn.Click += (s, e) => {
                    // We will handle event by calling function by name
                    // This is a bit of a hack but it works for simple cases if we have access to the VM instance
                };
            }
            return null;
        };
        _nativeFunctions["guiShow"] = args => {
            var form = (Form)args[0];
            Application.Run(form);
            return null;
        };

        // Runtime Registration (Auto-load if available)
        ArxLang.Runtime.Modules.HttpModule.Register(_nativeFunctions);
        ArxLang.Runtime.Modules.OsModule.Register(_nativeFunctions);
        ArxLang.Runtime.Modules.GraphicsModule.Register(_nativeFunctions);
    }

    private static double PopNumVal(object val) => Convert.ToDouble(val);

    public VirtualMachine(Bytecode bytecode)
    {
        _code = bytecode.ToArray();
        _constants = bytecode.Constants;
        _functionAddresses = bytecode.FunctionAddresses;
    }

    // Дозволяє нативним функціям (sort/mapArr/filter/reduce тощо) знайти
    // поточну VM і викликати ArxLang-функцію-значення як колбек.
    public static VirtualMachine? Current;

    public void Run()
    {
        Current = this;
        _currentFrame[0] = this; // Store VM in globals for callbacks if needed
        _currentFrame[1] = _nativeFunctions; // Allow access to native functions if needed

        while (_ip < _code.Length)
        {
            if (!Step()) return;
        }
    }

    // Виконує рекурсивний виклик ArxLang-функції-значення (з нативної
    // функції, напр. sort/mapArr/filter/reduce) і повертає її результат.
    // Використовує ту саму Step()-логіку, що й основний цикл Run(), просто
    // зупиняється, щойно стек викликів повертається до глибини, з якої
    // почався цей вкладений виклик (тобто після RETURN колбека).
    public object? InvokeFunctionValue(ArxFunctionRef funcRef, object[] args)
    {
        if (funcRef.NativeName != null)
        {
            return _nativeFunctions[funcRef.NativeName](args);
        }

        int savedIp = _ip;
        int targetCallDepth = _callStack.Count;

        _callStack.Push(_ip);
        _frameStack.Push(_currentFrame);
        _currentFrame = new Dictionary<int, object>();
        if (funcRef.Captured != null)
            foreach (var kv in funcRef.Captured) _currentFrame[kv.Key] = kv.Value;
        foreach (var a in args) _stack.Push(a);
        _ip = funcRef.Address;

        while (_callStack.Count > targetCallDepth)
        {
            if (!Step()) break;
        }

        var result = _stack.Count > 0 ? _stack.Pop() : null;
        _ip = savedIp;
        return result;
    }

    private bool Step()
    {
            try
            {
            var op = (OpCode)_code[_ip++];
            switch (op)
            {
                case OpCode.LOAD_CONST: _stack.Push(_constants[ReadInt16()]); break;
                case OpCode.LOAD_VAR:
                    int idx = ReadInt16();
                    _stack.Push(_currentFrame.TryGetValue(idx, out var v) ? v : 0);
                    break;
                case OpCode.STORE_VAR: _currentFrame[ReadInt16()] = _stack.Pop(); break;
                case OpCode.CALL:
                    int addr = ReadInt16();
                    _callStack.Push(_ip);
                    _frameStack.Push(_currentFrame);
                    _currentFrame = new Dictionary<int, object>();
                    _ip = addr;
                    break;
                case OpCode.RETURN:
                    if (_callStack.Count > 0)
                    {
                        // Компілятор гарантує, що кожен RETURN кладе власне
                        // значення (див. Compiler.cs), тому стек тут порожнім
                        // бути не має. Якщо все ж порожній — це помилка VM, і
                        // повертати 0 було б тихим приховуванням: раніше саме
                        // так "Stack empty" перетворювалось на значення функції.
                        var returnVal = _stack.Count > 0 ? _stack.Pop() : null;
                        // Якщо return стався всередині try (без TRY_END), приберемо
                        // обробники, що належали фрейму, який зараз завершується —
                        // інакше вони лишились би "висіти" і зловили б чужу помилку.
                        while (_handlers.Count > 0 && _handlers.Peek().CallStackDepth >= _callStack.Count) _handlers.Pop();
                        _ip = _callStack.Pop();
                        _currentFrame = _frameStack.Pop();
                        _stack.Push(returnVal);
                    }
                    break;
                case OpCode.ADD:
                    {
                        var b = _stack.Pop();
                        var a = _stack.Pop();
                        // null у конкатенації друкується як "null" (так само, як
                        // це робить print), а не валить програму NullReference:
                        // "текст" + f(), де f нічого не повернула, — надто
                        // звичайна ситуація, щоб бути аварійною.
                        if (a is string || b is string) _stack.Push((a?.ToString() ?? "null") + (b?.ToString() ?? "null"));
                        else _stack.Push(Convert.ToDouble(a) + Convert.ToDouble(b));
                    }
                    break;
                case OpCode.SUB: { var b = PopNum(); var a = PopNum(); _stack.Push(a - b); } break;
                case OpCode.MUL: { var b = PopNum(); var a = PopNum(); _stack.Push(a * b); } break;
                case OpCode.DIV: { var b = PopNum(); var a = PopNum(); _stack.Push(a / b); } break;
                case OpCode.MOD: { var b = PopNum(); var a = PopNum(); _stack.Push(a % b); } break;
                case OpCode.EQ: { var b = _stack.Pop(); var a = _stack.Pop(); _stack.Push(ValuesEqual(a, b)); } break;
                case OpCode.NEQ: { var b = _stack.Pop(); var a = _stack.Pop(); _stack.Push(!ValuesEqual(a, b)); } break;
                case OpCode.LT: { var b = PopNum(); var a = PopNum(); _stack.Push(a < b); } break;
                case OpCode.LTE: { var b = PopNum(); var a = PopNum(); _stack.Push(a <= b); } break;
                case OpCode.GT: { var b = PopNum(); var a = PopNum(); _stack.Push(a > b); } break;
                case OpCode.GTE: { var b = PopNum(); var a = PopNum(); _stack.Push(a >= b); } break;
                case OpCode.AND: { var b = Convert.ToBoolean(_stack.Pop()); var a = Convert.ToBoolean(_stack.Pop()); _stack.Push(a && b); } break;
                case OpCode.OR: { var b = Convert.ToBoolean(_stack.Pop()); var a = Convert.ToBoolean(_stack.Pop()); _stack.Push(a || b); } break;
                case OpCode.NOT: _stack.Push(!Convert.ToBoolean(_stack.Pop())); break;
                case OpCode.JUMP: _ip = ReadInt16(); break;
                case OpCode.JUMP_IF_FALSE:
                    int addr2 = ReadInt16();
                    if (!Convert.ToBoolean(_stack.Pop())) _ip = addr2;
                    break;
                case OpCode.PRINT: Console.WriteLine(_stack.Pop()); break;
                
                // ВВІД
                case OpCode.READ_LINE: _stack.Push(Console.ReadLine() ?? ""); break;
                case OpCode.READ_INT:
                    while (true)
                    {
                        var input = Console.ReadLine();
                        // null = ввід закінчився (Ctrl+Z, конвеєр, перенаправлення
                        // файлу). Раніше цикл у такому разі друкував запит
                        // нескінченно, бо чекав на ввід, якого вже не буде.
                        if (input == null) throw new Exception("Ввід закінчився: readInt() не отримав числа");
                        // Штовхаємо double, а не int: усі інші числа в мові -
                        // double, а порівняння int з double через object.Equals
                        // давало false навіть для однакових значень.
                        if (int.TryParse(input, out var result)) { _stack.Push((double)result); break; }
                        Console.Write("Введіть число: ");
                    }
                    break;
                case OpCode.READ_DOUBLE:
                    while (true)
                    {
                        var input = Console.ReadLine();
                        if (input == null) throw new Exception("Ввід закінчився: readDouble() не отримав числа");
                        if (double.TryParse(input, out var result)) { _stack.Push(result); break; }
                        Console.Write("Введіть число: ");
                    }
                    break;
                
                // ФАЙЛИ
                case OpCode.READ_FILE:
                    var path = _stack.Pop()?.ToString() ?? "";
                    try { _stack.Push(File.ReadAllText(path)); }
                    catch { _stack.Push(""); }
                    break;
                case OpCode.WRITE_FILE:
                    var content = _stack.Pop()?.ToString() ?? "";
                    var filePath = _stack.Pop()?.ToString() ?? "";
                    try { File.WriteAllText(filePath, content); _stack.Push(true); }
                    catch { _stack.Push(false); }
                    break;
                case OpCode.APPEND_FILE:
                    content = _stack.Pop()?.ToString() ?? "";
                    filePath = _stack.Pop()?.ToString() ?? "";
                    try { File.AppendAllText(filePath, content); _stack.Push(true); }
                    catch { _stack.Push(false); }
                    break;
                case OpCode.FILE_EXISTS:
                    filePath = _stack.Pop()?.ToString() ?? "";
                    _stack.Push(File.Exists(filePath));
                    break;
                
                // МАТЕМАТИКА
                case OpCode.SQRT: _stack.Push(Math.Sqrt(PopNum())); break;
                case OpCode.ABS: _stack.Push(Math.Abs(PopNum())); break;
                case OpCode.POW: { var b = PopNum(); var a = PopNum(); _stack.Push(Math.Pow(a, b)); } break;
                case OpCode.SIN: _stack.Push(Math.Sin(PopNum())); break;
                case OpCode.COS: _stack.Push(Math.Cos(PopNum())); break;
                case OpCode.TAN: _stack.Push(Math.Tan(PopNum())); break;
                case OpCode.ROUND: _stack.Push(Math.Round(PopNum())); break;
                case OpCode.FLOOR: _stack.Push(Math.Floor(PopNum())); break;
                case OpCode.CEIL: _stack.Push(Math.Ceiling(PopNum())); break;
                case OpCode.MAX: { var b = PopNum(); var a = PopNum(); _stack.Push(Math.Max(a, b)); } break;
                case OpCode.MIN: { var b = PopNum(); var a = PopNum(); _stack.Push(Math.Min(a, b)); } break;
                case OpCode.TO_STRING: _stack.Push(_stack.Pop()?.ToString() ?? "null"); break;
                case OpCode.TO_INT: _stack.Push(Convert.ToInt32(_stack.Pop())); break;
                case OpCode.TO_DOUBLE: _stack.Push(Convert.ToDouble(_stack.Pop())); break;
                case OpCode.LEN:
                    {
                        var val = _stack.Pop();
                        if (val is List<object> arr) _stack.Push((double)arr.Count);
                        else if (val is string s) _stack.Push((double)s.Length);
                        else if (val is Dictionary<string, object> d) _stack.Push((double)d.Count);
                        else _stack.Push(0.0);
                    }
                    break;

                case OpCode.ARRAY_NEW:
                    {
                        int size = ReadInt16();
                        var arr = new List<object>(size);
                        for (int i = 0; i < size; i++) arr.Add(0);
                        for (int i = size - 1; i >= 0; i--) arr[i] = _stack.Pop();
                        _stack.Push(arr);
                    }
                    break;
                case OpCode.ARRAY_GET:
                    {
                        int index = Convert.ToInt32(_stack.Pop());
                        var arr = (List<object>)_stack.Pop();
                        _stack.Push(arr[index]);
                    }
                    break;
                case OpCode.ARRAY_SET:
                    {
                        var val = _stack.Pop();
                        int index = Convert.ToInt32(_stack.Pop());
                        var arr = (List<object>)_stack.Pop();
                        arr[index] = val;
                    }
                    break;
                case OpCode.STRUCT_NEW:
                    {
                        int fieldCount = ReadInt16();
                        var fields = new Dictionary<string, object>();
                        for (int i = 0; i < fieldCount; i++)
                        {
                            var val = _stack.Pop();
                            var name = (string)_stack.Pop();
                            fields[name] = val;
                        }
                        _stack.Push(fields);
                    }
                    break;
                case OpCode.STRUCT_GET:
                    {
                        var fieldName = (string)_stack.Pop();
                        var fields = (Dictionary<string, object>)_stack.Pop();
                        _stack.Push(fields[fieldName]);
                    }
                    break;
                case OpCode.STRUCT_SET:
                    {
                        var val = _stack.Pop();
                        var fieldName = (string)_stack.Pop();
                        var fields = (Dictionary<string, object>)_stack.Pop();
                        fields[fieldName] = val;
                    }
                    break;
                case OpCode.CALL_NATIVE:
                    {
                        int nameIdx = ReadInt16();
                        int argCount = ReadInt16();
                        var name = (string)_constants[nameIdx];
                        var args = new object[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                            args[i] = _stack.Count > 0 ? _stack.Pop() : null!;
                        var result = _nativeFunctions[name](args);
                        // ВИПРАВЛЕНО: раніше `result ?? 0` тихо перетворював БУДЬ-ЯКЕ
                        // легітимне null-значення, повернуте нативною функцією
                        // (напр. mapGet на відсутньому ключі), на 0. Це було
                        // непомітно, поки null не став справжнім значенням мови
                        // (Фаза 8+); тепер null з нативної функції має лишатись null.
                        _stack.Push(result!);
                    }
                    break;
                case OpCode.CALL_METHOD:
                    {
                        int nameIdx = ReadInt16();
                        int argCount = ReadInt16();
                        var methodName = (string)_constants[nameIdx];
                        
                        // Object is at stack depth argCount
                        var obj = _stack.ElementAt(argCount);
                        if (obj is Dictionary<string, object> dict && dict.TryGetValue("__type", out var typeVal))
                        {
                            var typeName = typeVal.ToString();
                            var fullName = $"{typeName}.{methodName}";
                            if (_functionAddresses.TryGetValue(fullName, out int methodAddr))
                            {
                                _callStack.Push(_ip);
                                _frameStack.Push(_currentFrame);
                                _currentFrame = new Dictionary<int, object>();
                                _ip = methodAddr;
                            }
                            else
                            {
                                throw new Exception($"Метод '{methodName}' не знайдено у структурі '{typeName}'");
                            }
                        }
                        else
                        {
                            throw new Exception($"Спроба виклику методу '{methodName}' на об'єкті, що не є структурою");
                        }
                    }
                    break;
                
                case OpCode.MAKE_CLOSURE:
                    {
                        int closureAddr = ReadInt16();
                        int slotsConstIdx = ReadInt16();
                        var slotsList = (List<object>)_constants[slotsConstIdx];
                        var captured = new Dictionary<int, object>();
                        foreach (var slotObj in slotsList)
                        {
                            int slot = Convert.ToInt32(slotObj);
                            if (_currentFrame.TryGetValue(slot, out var val)) captured[slot] = val;
                        }
                        _stack.Push(new ArxFunctionRef { Address = closureAddr, Captured = captured });
                    }
                    break;
                case OpCode.CALL_VALUE:
                    {
                        int argCount = ReadInt16();
                        var callArgs = new object[argCount];
                        for (int i = argCount - 1; i >= 0; i--) callArgs[i] = _stack.Count > 0 ? _stack.Pop() : null!;
                        var funcRef = (ArxFunctionRef)_stack.Pop();

                        if (funcRef.NativeName != null)
                        {
                            // Посилання на нативну функцію - немає байткод-адреси,
                            // викликаємо напряму, як CALL_NATIVE.
                            var nativeResult = _nativeFunctions[funcRef.NativeName](callArgs);
                            _stack.Push(nativeResult!);
                            break;
                        }

                        _callStack.Push(_ip);
                        _frameStack.Push(_currentFrame);
                        _currentFrame = new Dictionary<int, object>();
                        if (funcRef.Captured != null)
                            foreach (var kv in funcRef.Captured) _currentFrame[kv.Key] = kv.Value;
                        foreach (var a in callArgs) _stack.Push(a);
                        _ip = funcRef.Address;
                    }
                    break;

                case OpCode.TRY_BEGIN:
                    {
                        int catchAddr = ReadInt16();
                        int varSlot = ReadInt16();
                        _handlers.Push(new ExceptionHandler
                        {
                            CatchAddr = catchAddr,
                            VarSlot = varSlot,
                            StackDepth = _stack.Count,
                            CallStackDepth = _callStack.Count,
                            FrameStackDepth = _frameStack.Count
                        });
                    }
                    break;
                case OpCode.TRY_END:
                    if (_handlers.Count > 0) _handlers.Pop();
                    break;
                case OpCode.THROW:
                    {
                        var thrown = _stack.Count > 0 ? _stack.Pop() : "помилка";
                        if (_handlers.Count > 0) UnwindToHandler(thrown);
                        else throw new ArxThrowException(thrown?.ToString() ?? "помилка");
                    }
                    break;

                case OpCode.POP: if (_stack.Count > 0) _stack.Pop(); break;

                case OpCode.HALT: return false;
            }
            }
            catch (ArxThrowException)
            {
                // Явний throw без активного обробника — це вже вирішено в THROW
                // (там або UnwindToHandler, або кидається саме цей виняток нагору,
                // тому тут його просто пропускаємо далі, до ArxNode.cs).
                throw;
            }
            catch (Exception ex) when (_handlers.Count > 0)
            {
                // Внутрішня помилка VM (ділення на нуль, вихід за межі масиву,
                // помилка нативної функції тощо) під час активного try —
                // перехоплюємо її так само, як явний throw.
                UnwindToHandler(ex.Message);
            }
        return true;
    }

    private int ReadInt16()
    {
        int val = _code[_ip] | (_code[_ip + 1] << 8);
        _ip += 2;
        return val;
    }

    private double PopNum() => Convert.ToDouble(_stack.Pop());

    // Порівняння на рівність. Раніше тут був object.Equals, який порівнює ще й
    // ТИПИ "коробок": int 2 та double 2.0 вважались різними, тому x == 2 давало
    // false для числа з readInt(). При цьому < і > працювали правильно, бо
    // зводять обидва боки до double через PopNum - через цю різницю баг і був
    // непомітним: підказки "більше/менше" діяли, а перевірка на рівність ні.
    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null || b == null) return a == null && b == null;

        if (IsNumber(a) && IsNumber(b))
            return Convert.ToDouble(a) == Convert.ToDouble(b);

        return Equals(a, b);
    }

    private static bool IsNumber(object v) =>
        v is double || v is float || v is decimal ||
        v is int || v is long || v is short || v is sbyte ||
        v is uint || v is ulong || v is ushort || v is byte;
}
