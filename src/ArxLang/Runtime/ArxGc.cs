namespace ArxLang.Runtime;

// Це НЕ заміна .NET GC — CLR і так коректно збирає боксовані object-графи
// ArxLang (включно з циклами посилань у структурах). Цінність тут інша:
// дати .arx-скрипту видимість і контроль над власними виділеннями (масиви,
// структури, мапи), щоб некерований цикл виділень не поклав хост-процес.
// Один інстанс на процес — реєструється як native-функції у
// VirtualMachine, чиї лямбди статичні, тож і облік має бути статичним.
public sealed class ArxGc
{
    public static readonly ArxGc Instance = new();

    private long _allocated;
    private long _sinceLastCheck;
    private long _limit = long.MaxValue;
    private const int CheckInterval = 256;

    public void RecordAllocation()
    {
        _allocated++;
        _sinceLastCheck++;
        // Перевірка ліміту на кожному виділенні була б зайвим накладним
        // видатком у гарячих циклах — досить перевіряти пачками.
        if (_sinceLastCheck < CheckInterval) return;
        _sinceLastCheck = 0;
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
