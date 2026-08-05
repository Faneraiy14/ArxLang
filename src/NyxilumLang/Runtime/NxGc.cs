namespace NyxilumLang.Runtime;

// Це НЕ заміна .NET GC — CLR і так коректно збирає боксовані object-графи
// NyxilumLang (включно з циклами посилань у структурах). Цінність тут інша:
// дати .nx-скрипту видимість і контроль над власними виділеннями (масиви,
// структури, мапи), щоб некерований цикл виділень не поклав хост-процес.
// Один інстанс на процес — реєструється як native-функції у
// VirtualMachine, чиї лямбди статичні, тож і облік має бути статичним.
//
// З появою spawn() (ConcurrencyModule.cs) RecordAllocation() викликається
// одночасно з кількох потоків (головний + воркери) на ЦЕЙ САМИЙ спільний
// інстанс — ліміт виділень навмисно один бюджет на всю програму, а не
// окремий на воркер. Тому лічильники через Interlocked, а не звичайний
// "++": некоректний під конкурентним доступом (втрачені інкременти).
public sealed class NxGc
{
    public static readonly NxGc Instance = new();

    private long _allocated;
    private long _sinceLastCheck;
    private long _limit = long.MaxValue;
    private const int CheckInterval = 256;

    public void RecordAllocation()
    {
        Interlocked.Increment(ref _allocated);
        // Перевірка ліміту на кожному виділенні була б зайвим накладним
        // видатком у гарячих циклах — досить перевіряти пачками. Гонка між
        // потоками тут можлива (кілька можуть одночасно "виграти" поріг
        // CheckInterval і перевірити трохи частіше за задумане) — нешкідливо,
        // бо CheckLimits() лише читає підсумковий _allocated, не змінює його.
        if (Interlocked.Increment(ref _sinceLastCheck) < CheckInterval) return;
        Interlocked.Exchange(ref _sinceLastCheck, 0);
        CheckLimits();
    }

    public void CheckLimits()
    {
        if (_allocated > _limit)
            throw new Exception($"GC ліміт перевищено: виділено {_allocated} об'єктів, ліміт {_limit}");
    }

    public void SetLimit(long max) => _limit = max <= 0 ? long.MaxValue : max;

    public void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public Dictionary<string, object> Stats()
    {
        return new Dictionary<string, object>
        {
            ["allocated"] = (double)_allocated,
            ["limit"] = _limit == long.MaxValue ? -1.0 : (double)_limit,
            ["bytesEstimate"] = (double)GC.GetAllocatedBytesForCurrentThread()
        };
    }
}
