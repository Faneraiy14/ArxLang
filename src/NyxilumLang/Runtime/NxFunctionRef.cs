namespace NyxilumLang.Runtime;

// Функція як значення: або посилання на іменовану функцію (Name/Address
// відомі одразу чи патчаться після компіляції всіх функцій), або анонімна
// функція (лямбда) зі знімком захоплених значень зовнішньої області видимості
// (просте "замикання копіюванням значень", без справжніх upvalue-посилань).
public class NxFunctionRef
{
    public string? Name;
    public int Address;
    public Dictionary<int, object>? Captured;
    // Якщо задано - це посилання на НАТИВНУ (вбудовану) функцію, у якої
    // немає адреси в байткоді; викликається напряму через _nativeFunctions.
    public string? NativeName;

    public override string ToString() => $"<function {NativeName ?? Name ?? "lambda"}>";
}
