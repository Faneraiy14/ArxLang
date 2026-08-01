# Встановлення ArxLang

Два способи. Перший — для будь-кого, .NET не потрібен. Другий — якщо хочеш
працювати з вихідним кодом самої мови. Обидва працюють однаково на Windows,
Linux і Mac.

## Спосіб 1 — готовий реліз (рекомендовано)

**Крок 1.** Відкрий сторінку [останнього релізу](https://github.com/Faneraiy14/ArxLang/releases/latest)
(Windows) або [релізів ArxNode](https://github.com/Faneraiy14/ArxNode/releases/latest)
(Linux/Mac — там же лежить і той самий win-x64.zip) і скачай архів для своєї
платформи: `ArxLang-win-x64.zip`, `ArxNode-linux-x64.tar.gz` або
`ArxNode-osx-x64.tar.gz` / `ArxNode-osx-arm64.tar.gz`.

**Крок 2.** Розпакуй архів у будь-яку папку.

- **Windows:** правою кнопкою на архіві → «Видобути все».
- **Linux/Mac:** `tar xzf ArxNode-<платформа>.tar.gz`.

Усередині — бінарник (`ArxLang.exe` на Windows, `ArxLang` на Linux/Mac),
скрипт встановлення й README.

**Крок 3.** Запусти скрипт встановлення з тієї ж папки.

**Windows** — клацни правою на `install-arx.ps1` → **«Запустити за допомогою
PowerShell»**. Якщо PowerShell покаже попередження про виконання скриптів —
це нормальна поведінка для файлів, скачаних з інтернету, не помилка:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

і запусти `install-arx.ps1` ще раз.

**Linux/Mac** — у терміналі з тієї ж папки:

```bash
bash install-arx.sh
```

**Крок 4.** Закрий термінал і відкрий **новий** — PATH оновлюється лише для
нових вікон (на Linux/Mac достатньо `source ~/.bashrc` чи `source ~/.zshrc`).

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

Потрібен [.NET SDK 10](https://dotnet.microsoft.com/download) — сам SDK
крос-платформний, встановлюється так само на Windows, Linux і Mac.

```bash
git clone https://github.com/Faneraiy14/ArxLang.git
cd ArxLang
dotnet build src/ArxLang
```

Далі — той самий скрипт встановлення, що й у Способі 1, лежить прямо в
корені репозиторію й сам знаходить щойно зібраний бінарник:

```powershell
# Windows
powershell -ExecutionPolicy Bypass -File install-arx.ps1
```

```bash
# Linux/Mac
bash install-arx.sh
```

Далі те саме — нове вікно термінала, `arx --version`.

Перезбереш проєкт (`dotnet build`) — команда `arx` підхопить нову версію
сама, без повторного запуску скрипта встановлення.

GUI (`guiWindow` тощо) і графіка (`createCanvas` тощо) працюють лише на
Windows — усередині Windows Forms, якого поза Windows не існує. Решта мови
(компілятор, VM, майже вся стандартна бібліотека) працює однаково на всіх
трьох платформах.

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
| SmartScreen блокує запуск (Windows) | Нормально для непідписаних `.exe` — «Докладніше» → «Виконати все одно» |
| Скрипт відмовляється запускатись у PowerShell | `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`, потім спробуй ще раз |
| `bash install-arx.sh` каже "не знайдено" | Бінарник не поруч зі скриптом і не зібраний — див. Спосіб 1 крок 2 або Спосіб 2 |
