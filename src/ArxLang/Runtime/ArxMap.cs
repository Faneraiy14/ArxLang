namespace ArxLang.Runtime;

// Мапа/словник як окремий тип, відмінний від struct (Dictionary<string,object>
// із полем __type). Ключ може бути будь-яким значенням ArxLang (число, рядок,
// bool) — object.Equals/GetHashCode коректно працюють для всіх цих типів.
public class ArxMap
{
    public Dictionary<object, object> Entries { get; } = new();
}
