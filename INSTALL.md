# Встановлення ArxLang (Windows)

Два способи. Перший — для будь-кого, .NET не потрібен. Другий — якщо хочеш
працювати з вихідним кодом самої мови.

## Спосіб 1 — готовий реліз (рекомендовано)

**Крок 1.** Відкрий сторінку [останнього релізу](https://github.com/Faneraiy14/ArxLang/releases/latest)
і скачай `ArxLang-win-x64.zip`.

**Крок 2.** Розпакуй архів у будь-яку папку — наприклад `C:\ArxLang`.
Правою кнопкою на файлі → «Видобути все».

Усередині три файли:

```
ArxLang.exe        сама мова
install-arx.ps1     скрипт, що додає команду arx
README.txt          короткі нотатки
```

**Крок 3.** Клацни правою на `install-arx.ps1` → **«Запустити за допомогою
PowerShell»**.

Якщо Windows покаже попередження про виконання скриптів — це нормальна
поведінка PowerShell для файлів, скачаних з інтернету, не помилка. Введи в
тому ж вікні:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

і запусти `install-arx.ps1` ще раз.

**Крок 4.** Закрий термінал і відкрий **новий** — Windows оновлює PATH лише
для нових вікон, у вже відкритому команда `arx` ще не з'явиться.

**Крок 5.** Перевір:

```bash
arx --version
```

Має відповісти `ArxNode v1.0.0 (based on ArxLang v8.0)`. Якщо відповіло —
готово, `arx myprogram.arx` працює з будь-якої папки.

### Якщо Windows показала «Windows захистила ваш ПК»

`ArxLang.exe` не підписаний платним сертифікатом, тому SmartScreen може
насторожитись при першому запуску. Натисни дрібне посилання **«Докладніше»**
зверху вікна, потім **«Виконати все одно»**.

## Спосіб 2 — з вихідного коду

Потрібен [.NET SDK 10](https://dotnet.microsoft.com/download) (Windows).

```bash
git clone https://github.com/Faneraiy14/ArxLang.git
cd ArxLang
dotnet build src/ArxLang
powershell -ExecutionPolicy Bypass -File install-arx.ps1
```

Далі те саме — нове вікно термінала, `arx --version`.

Перезбереш проєкт (`dotnet build`) — команда `arx` підхопить нову версію
сама, без повторного запуску install-arx.ps1.

## Перша програма

Створи файл `hello.arx`:

```arx
func main() {
    print("Привіт, ArxLang!")
}
```

Запусти:

```bash
arx hello.arx
```

## Бібліотеки

```bash
arx install owner/repo
```

Тягне публічний GitHub-репозиторій із `main.arx` у корені. Подробиці —
у розділі [«Менеджер пакетів»](README.md#менеджер-пакетів) головного README.

## Якщо щось не працює

| Проблема | Причина |
|---|---|
| `arx` не розпізнається як команда | Термінал відкрито до встановлення — закрий і відкрий новий |
| `dotnet build` каже, що SDK не знайдено | .NET SDK не встановлений або треба перезапустити термінал після встановлення |
| SmartScreen блокує запуск | Нормально для непідписаних `.exe` — «Докладніше» → «Виконати все одно» |
| Скрипт відмовляється запускатись у PowerShell | `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`, потім спробуй ще раз |
