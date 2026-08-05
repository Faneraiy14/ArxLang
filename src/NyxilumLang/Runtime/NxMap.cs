namespace NyxilumLang.Runtime;

// Мапа/словник як окремий тип, відмінний від struct (Dictionary<string,object>
// із полем __type). Ключ може бути будь-яким значенням NyxilumLang (число, рядок,
// bool) — object.Equals/GetHashCode коректно працюють для всіх цих типів.
public class NxMap
{
    public Dictionary<object, object> Entries { get; } = new();
}
