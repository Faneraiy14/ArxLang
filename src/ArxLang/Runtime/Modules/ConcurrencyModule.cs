using System.Linq;
using ArxLang.VM;

namespace ArxLang.Runtime.Modules;

// spawn(fn, ...args) — запускає fn на НОВІЙ VM в окремому потоці: той
// самий байткод (readonly після компіляції, безпечний для спільного
// доступу), але цілком окремий стек/фрейми/глобальні. Жодного спільного
// мутабельного стану з батьківською VM чи іншими воркерами — усе, що
// перетинає межу spawn()/channelSend(), проходить через DeepCopy.
//
// Без цього дві "паралельні" гілки коду, що случайно ділять один і той
// самий масив/мапу через замикання, мовчки псували б дані одна одній —
// саме той клас гонки даних, який непомітний, поки хтось не зловить
// його раз на тисячу запусків.
public sealed class ArxWorker
{
    public required Thread Thread;
    public object? Result;
    public Exception? Error;
}

public sealed class ArxChannel
{
    public readonly System.Collections.Concurrent.BlockingCollection<object> Queue = new();
}

public static class ConcurrencyModule
{
    public static void Register(Dictionary<string, Func<object[], object?>> registry)
    {
        // .Add(), не registry["ім'я"] = ...: реєстрація модулів раніше йшла
        // через звичайний індексатор, який МОВЧКИ переписує наявний запис
        // при колізії імен — саме так перша версія цього модуля тихо
        // затерла вже наявну "join" (склеювання масиву/рядка через
        // роздільник, VirtualMachine.cs). .Add() кидає виняток одразу при
        // старті процесу (static-конструктор VM), а не десь у непов'язаному
        // тесті через незрозумілу помилку приведення типів.
        registry.Add("spawn", Spawn);
        registry.Add("workerJoin", Join);
        registry.Add("newChannel", args => {
            ArxGc.Instance.RecordAllocation();
            return new ArxChannel();
        });
        registry.Add("channelSend", ChannelSend);
        registry.Add("channelReceive", ChannelReceive);
    }

    private static object? Spawn(object[] args)
    {
        var funcRef = (ArxFunctionRef)DeepCopy(args[0])!;
        var callArgs = args.Skip(1).Select(a => DeepCopy(a)!).ToArray();

        var parentVm = VirtualMachine.Current!;
        var globalsCopy = new Dictionary<string, object>();
        foreach (var kv in parentVm.Globals) globalsCopy[kv.Key] = DeepCopy(kv.Value)!;

        var worker = new ArxWorker { Thread = null! };
        var thread = new Thread(() =>
        {
            try
            {
                var workerVm = new VirtualMachine(parentVm, globalsCopy);
                worker.Result = workerVm.RunFunction(funcRef, callArgs);
            }
            catch (Exception ex)
            {
                worker.Error = ex;
            }
        })
        {
            // Background: воркер, який ніхто не приєднав через join(), не
            // тримає процес живим вічно — так само, як забутий httpServer
            // тримав би, якби був НЕ фоновим, лише навпаки за замовчуванням.
            IsBackground = true,
        };
        worker.Thread = thread;
        thread.Start();

        ArxGc.Instance.RecordAllocation();
        return worker;
    }

    private static object? Join(object[] args)
    {
        var worker = (ArxWorker)args[0];
        worker.Thread.Join();
        if (worker.Error != null)
            throw worker.Error;
        return worker.Result;
    }

    private static object? ChannelSend(object[] args)
    {
        var ch = (ArxChannel)args[0];
        ch.Queue.Add(DeepCopy(args[1])!);
        return null;
    }

    // channelReceive(ch)            — блокує без обмеження часу
    // channelReceive(ch, timeoutMs) — null, якщо нічого не прийшло за час
    private static object? ChannelReceive(object[] args)
    {
        var ch = (ArxChannel)args[0];
        if (args.Length > 1)
        {
            int timeoutMs = Convert.ToInt32(args[1]);
            return ch.Queue.TryTake(out var value, timeoutMs) ? value : null;
        }
        return ch.Queue.Take();
    }

    // Рекурсивно клонує все, що ArxLang-код міг би мутувати з обох боків
    // межі потоку (масиви/мапи/структури/захоплені значення лямбд).
    // Примітиви (double/string/bool/null) в .NET уже незмінні — ділити
    // ними безпечно й без копії. seen — захист від зациклення на
    // структурах із циклічними посиланнями (підтримуються мовою, див.
    // коментар біля ArxGc).
    internal static object? DeepCopy(object? value, Dictionary<object, object>? seen = null)
    {
        if (value is null) return null;
        if (value is double || value is string || value is bool) return value;

        seen ??= new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        if (seen.TryGetValue(value, out var existing)) return existing;

        switch (value)
        {
            case List<object> list:
                {
                    var copy = new List<object>(list.Count);
                    seen[value] = copy;
                    ArxGc.Instance.RecordAllocation();
                    foreach (var item in list) copy.Add(DeepCopy(item, seen)!);
                    return copy;
                }
            case Dictionary<string, object> structFields:
                {
                    var copy = new Dictionary<string, object>();
                    seen[value] = copy;
                    ArxGc.Instance.RecordAllocation();
                    foreach (var kv in structFields) copy[kv.Key] = DeepCopy(kv.Value, seen)!;
                    return copy;
                }
            case ArxMap map:
                {
                    var copy = new ArxMap();
                    seen[value] = copy;
                    ArxGc.Instance.RecordAllocation();
                    foreach (var kv in map.Entries) copy.Entries[DeepCopy(kv.Key, seen)!] = DeepCopy(kv.Value, seen)!;
                    return copy;
                }
            case ArxFunctionRef fn:
                {
                    var copy = new ArxFunctionRef { Name = fn.Name, Address = fn.Address, NativeName = fn.NativeName };
                    seen[value] = copy;
                    if (fn.Captured != null)
                    {
                        copy.Captured = new Dictionary<int, object>();
                        foreach (var kv in fn.Captured) copy.Captured[kv.Key] = DeepCopy(kv.Value, seen)!;
                    }
                    return copy;
                }
            default:
                // Handle-подібні об'єкти (ArxWorker, ArxChannel, файлові
                // хендли БД тощо) навмисно НЕ копіюються — ділитись самим
                // каналом чи воркером між потоками якраз і є суттю їхнього
                // API, на відміну від звичайних даних.
                return value;
        }
    }
}
