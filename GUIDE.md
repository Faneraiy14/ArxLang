
# ArxLang: Посібник користувача

ArxLang - це потужна, але проста мова програмування, створена для навчання та швидкої розробки.

## Основний синтаксис

### Змінні
Використовуйте `var` для оголошення змінних.
```arx
var x = 10
var name = "Arx"
var isCool = true
var nothing = null       // isNull(nothing) -> true
```

### Функції
Функції оголошуються за допомогою `func`. Точка входу - функція `main`.
```arx
func add(a, b) {
    return a + b
}

func main() {
    var result = add(5, 10)
    print("Результат: " + result)
}
```

`func` можна оголосити й усередині іншої функції — видима лише в межах
"батька" (лексично, як у більшості мов): однакова назва в різних
"батьках" не конфліктує, кожен викликає свою. Це не замикання — вкладена
функція не бачить локальних змінних "батька"; для захоплення оточення
потрібна анонімна лямбда (`var f = func() {...}`, розділ нижче).

```arx
func outer() {
    func inner(x) {
        return x * 2
    }
    return inner(21)
}
print(outer()) // 42
```

### Управляючі конструкції
```arx
if (x > 0) {
    print("Додатне")
} else {
    print("Від'ємне або нуль")
}

for i in 0..5 {
    print(i)
}

for item in [10, 20, 30] {
    print(item)     // ітерація по елементах масиву (без ручного індексу)
}

while (x > 0) {
    print(x)
    x = x - 1
}
```

### Структури та Методи
```arx
struct Point {
    x: i32
    y: i32
}

func Point.move(dx, dy) {
    self.x = self.x + dx
    self.y = self.y + dy
}

func main() {
    var p = Point { x: 10, y: 20 }
    p.move(5, 5)
    print(p.x) // 15
}
```

Поля структури обов'язково з типом (`x: i32`, не просто `x`), метод —
`func Структура.метод(...)` окремо від оголошення `struct`, доступ до
поточного екземпляра — `self`, не `this`.

### Обробка помилок (try/catch/throw)
```arx
func riskyDivide(a, b) {
    if b == 0 {
        throw "Ділення на нуль!"
    }
    return a / b
}

func main() {
    try {
        var r = riskyDivide(10, 0)
    } catch (e) {
        print("Спіймано: " + e)
    }
}
```
`try`/`catch` перехоплює як явний `throw`, так і внутрішні помилки VM (вихід за межі масиву, помилки нативних функцій тощо) — програма не падає, а виконання продовжується з catch-блоку.

### Мапи/словники
```arx
var ages = newMap()
mapSet(ages, "Святослав", 14)
print(mapGet(ages, "Святослав"))       // 14
print(mapHas(ages, "Богдан"))          // false
mapRemove(ages, "Святослав")
var keys = mapKeys(ages)
```
Мапа — окремий тип від `struct` (`typeOf()` повертає `"map"`), ключем може бути число, рядок або bool.

### Функції як значення та замикання
```arx
func square(x) { return x * x }

func apply(fn, x) { return fn(x) }

func main() {
    print(apply(square, 5))              // 25 - іменована функція як значення

    var double_ = func(x) { return x * 2 }
    print(double_(21))                   // 42 - анонімна функція (лямбда)

    var makeAdder = func(n) {
        return func(x) { return x + n }  // замикання - захоплює n
    }
    var add5 = makeAdder(5)
    print(add5(10))                      // 15
}
```
Замикання захоплюють значення зовнішніх змінних **копією в момент створення** лямбди (не живим посиланням) — зміна зовнішньої змінної після створення лямбди на неї не впливає.

### Модулі (import)
```arx
// math_helpers.arx
func square(x) { return x * x }
```
```arx
// main.arx
import "math_helpers.arx"

func main() {
    print(square(5))   // 25
}
```
Шлях в `import` — відносно файлу, що імпортує. Циклічні та повторні імпорти безпечні (кожен файл обробляється один раз).

Вибірковий import — тягне лише перелічені функції/структури/глобальні змінні, а не весь файл:
```arx
import "math_helpers.arx" { square }

func main() {
    print(square(5))   // 25 - cube() з того ж файлу лишається неімпортованим
}
```
Методи названої структури підтягуються автоматично разом з нею. Якщо переліченого імені нема у файлі — помилка одразу при запуску (ще до виконання коду).

### Стандартна бібліотека (`lib/`)
У теці `lib/` в корені ArxEcosystem лежать готові `.arx`-модулі — підключаються звичайним `import` за відносним шляхом (`../lib/...` з файлу в `tests/`, або `lib/...`, якщо скрипт лежить поруч із самою `lib/`):

- **`lib/strings.arx`** — `capitalize(s)`, `titleCase(s)`, `isBlank(s)`, `isEmpty(s)`, `padLeft(s, len, ch)`, `padRight(s, len, ch)`, `countOccurrences(s, sub)`.
- **`lib/collections.arx`** — `range(n)`, `rangeFrom(start, end)`, `sum(arr)`, `first(arr)`, `last(arr)`, `flatten(arr)` (один рівень), `zip(arr1, arr2)`, `chunk(arr, size)`, `count(arr, fn)`.
- **`lib/testing.arx`** — `assertTrue(cond, msg)`, `assertFalse(cond, msg)`, `assertEqual(actual, expected, msg)`, `assertThrows(fn, msg)`. Провал — звичайний `throw`, тож або лови його `try/catch` сам, або лишай непійманим, щоб процес впав з ненульовим кодом (зручно для CI).
  ```arx
  import "lib/testing.arx" { assertEqual }

  func main() {
      assertEqual(2 + 2, 4, "2+2 має дорівнювати 4")
      print("тест пройдено")
  }
  ```
- **`lib/http_client.arx`** — обгортка над `httpGet`/`httpPost`/`httpRequest` з автоматичною JSON-серіалізацією (у дусі Python-бібліотеки `requests`): `getJson(url)`, `postJson(url, data)`, `requestJson(url, method, data)` (`data` — мапа/масив або `null`), `requestStatus(url, method, data)` — лише код статусу.
  ```arx
  import "lib/http_client.arx" { postJson }

  func main() {
      var data = newMap()
      mapSet(data, "name", "Святослав")
      var response = postJson("https://httpbin.org/post", data)
      print(toJson(response))
  }
  ```
- **`lib/telegram.arx`** — обгортка над [Telegram Bot API](https://core.telegram.org/bots/api) (звичайний HTTPS+JSON, без WebSocket - тому повністю реалізований на самій ArxLang): `tgGetMe(token)`, `tgSendMessage(token, chatId, text)`, `tgGetUpdates(token, offset)`, `tgMessageText(update)`, `tgChatId(update)`, і блокуючий `tgPollLoop(token, handler)` для готового бота одним викликом. Токен читай через `osEnv("TELEGRAM_BOT_TOKEN")`, ніколи не хардкодь у скрипті. Повний робочий приклад ехо-бота: `programs/telegram_echo_bot.arx`.
  ```arx
  import "lib/telegram.arx" { tgPollLoop, tgMessageText, tgChatId, tgSendMessage }

  func main() {
      var token = osEnv("TELEGRAM_BOT_TOKEN")
      tgPollLoop(token, func(update) {
          var text = tgMessageText(update)
          if !isNull(text) {
              tgSendMessage(token, tgChatId(update), "Ехо: " + text)
          }
      })
  }
  ```
  Discord поки НЕ підтримується так само просто: реальний Discord-бот приймає повідомлення через WebSocket-з'єднання (Gateway), якого в ArxLang немає - це окрема, важча нативна фіча (постійне з'єднання, heartbeat), не просто HTTP-обгортка.

### Функції вищого порядку та JSON
```arx
var nums = [5, 2, 8, 1]
var sorted = sort(nums, func(a, b) { return a - b })
var squares = mapArr(nums, func(x) { return x * x })
var evens = filter(nums, func(x) { return x % 2 == 0 })
var sum = reduce(nums, func(acc, x) { return acc + x }, 0)

var data = newMap()
mapSet(data, "name", "Святослав")
print(toJson(data))              // {"name":"Святослав"}
var back = fromJson("[1,2,3]")   // масив
```

### Графіка (2D і 3D)
```arx
var canvas = createCanvas("Гра", 400, 300)
var frame = 0
while frame < 60 {
    clearCanvas(canvas, 20, 20, 30)
    drawRect(canvas, 50, 100, 40, 40, 200, 60, 60)
    drawCircle(canvas, 300, 150, 25, 60, 200, 60)
    drawText(canvas, "Кадр " + toString(frame), 10, 10, 14, 255, 255, 255)
    presentCanvas(canvas)
    sleep(16)
    frame = frame + 1
}
closeCanvas(canvas)
```
3D робиться поверх того ж полотна: обертання точок через `sin`/`cos` (уже вбудовані), а `project3D(canvas, x, y, z, camDistance)` перетворює 3D-координату в 2D-точку екрана — і далі малюєш ребра через `drawLine`. Ввід: `isKeyDown("W")`, `isMouseDown(canvas)`, `getMouseX/Y(canvas)`, `canvasShouldClose(canvas)`.

### Вбудовані функції
- `print(val)` - вивід у консоль
- `readLine()`, `readInt()`, `readDouble()` - читання з консолі
- `sqrt(x)`, `sin(x)`, `cos(x)`, `tan(x)`, `pow(x,y)`, `abs(x)`, `round/floor/ceil(x)`, `clamp(x,min,max)` - математика
- `max/min(a,b)` або `max/min(arr)` - максимум/мінімум двох чисел або всього масиву
- `toFixed(x,n)` - число як рядок з n знаками після коми, напр. `toFixed(3.14159, 2)` -> `"3.14"`
- `readFile(path)`, `writeFile(path, content)`, `appendFile(path, content)`, `fileExists(path)`, `readLines(path)` - робота з файлами
- `toString(v)`, `toInt(v)`, `toDouble(v)`, `typeOf(v)`, `isNumber/isString/isArray/isBool(v)` - перетворення та перевірка типів
- `charCode(s)` - код першого символу рядка (наприклад, `charCode("A")` -> 65); `fromCharCode(code)` - символ за кодом
- `len(v)`, `substring(s,start,len)`, `replace/toUpper/toLower/contains/startsWith/endsWith(s,...)`, `split(s,sep)`, `join(arr,sep)` - рядки
- `trim(s)` - прибирає пробіли з обох боків; `repeat(s,n)` - повторює рядок n разів
- `indexOf(v,item)` - позиція елемента/підрядка (-1, якщо нема); працює і для рядків, і для масивів
- `reverse(v)` - розвертає рядок або масив (оригінальний масив не змінює)
- `append(arr,v)`, `pop(arr)`, `insert(arr,i,v)`, `removeAt(arr,i)`, `clear(arr)` - масиви
- `slice(arr,start,end?)` - підмасив від start до end (або до кінця); `unique(arr)` - прибирає дублікати, лишає порядок
- `newMap()`, `mapSet(m,k,v)`, `mapGet(m,k)`, `mapHas(m,k)`, `mapRemove(m,k)`, `mapKeys(m)`, `mapValues(m)` - мапи/словники
- `sort(arr,cmp)`, `mapArr(arr,fn)`, `filter(arr,fn)`, `reduce(arr,fn,init)` - функції вищого порядку над масивами
- `toJson(v)`, `fromJson(str)` - серіалізація в JSON і назад
- `sleep(ms)` - пауза виконання
- `exit(код)` - негайно завершує процес із заданим кодом виходу (рядки коду після exit() не виконуються) - для CI/скриптів, де потрібен конкретний код (0 успіх, інший - провал), без штучного throw (той завжди дає код 1)
- `gc_stats()` - структура `{allocated, limit, bytesEstimate}` з обліком ArxLang-виділень (масиви/структури/мапи) поточного запуску; `gc_collect()` - форсує збирання сміття .NET і оновлює оцінку пам'яті; `gc_limit(n)` - встановлює ліміт кількості виділень, перевищення кидає помилку (можна зловити через `try/catch`, або задати ззовні через `ARX_GC_MAX_OBJECTS`)
- `dbOpen(path)` - відкриває (чи створює) персистентну KV-базу [ArxDb](https://github.com/Faneraiy14/ArxDb) в теці за шляхом `path`; `dbSet(db,k,v)`, `dbGet(db,k)` (рядок або `null`), `dbHas(db,k)`, `dbDelete(db,k)` - значення в v1 лише рядки; `dbKeys(db,prefix?)` - масив ключів (опційно за префіксом); `dbCount(db)`; `dbCheckpoint(db)` - примусова компакція; `dbClose(db)` - закриває базу (компактує WAL, якщо він непорожній)
- `createCanvas(title,w,h)`, `clearCanvas`, `drawRect`, `drawCircle`, `drawLine`, `drawText`, `presentCanvas`, `canvasShouldClose`, `closeCanvas` - 2D графіка
- `project3D(canvas,x,y,z,camDistance)` - проекція 3D-точки в 2D для рендеру 3D-сцен
- `guiWindow(title,w,h)`, `guiLabel(text,x,y,w,h)`, `guiButton(text,x,y,w,h)`, `guiTextBox(x,y,w,h)` - вікна на Windows Forms
- `guiAdd(parent,child)` - додати елемент у вікно; `guiSetText(control,text)`/`guiGetText(control)` - змінити/прочитати текст
- `guiOnAction(button,fn)` - викликати ArxLang-функцію `fn` при кліку (звичайну функцію-значення, як у `sort(arr,cmp)` — без дужок, не рядок з іменем)
- `guiShow(win)` - показати вікно (блокує, доки вікно не закриють)
- `isKeyDown(key)`, `isMouseDown(canvas)`, `getMouseX/Y(canvas)` - ввід для вікна
- `randomInt(min,max)`, `randomDouble(min,max)`, `now()`, `today()`, `timestamp()` - утиліти
- `osPlatform()`, `osArchitecture()`, `osMemory()`, `osCpuCount()`, `osEnv(name)`, `osCwd()` - інформація про систему
- `httpGet(url)`, `urlStatus(url)` - HTTP-запити
- `httpPost(url, body)` - POST-запит з тілом `body` (Content-Type `application/json`), повертає тіло відповіді рядком
- `httpRequest(url, method, body?)` - запит довільним методом (`"PUT"`, `"DELETE"`, `"PATCH"` тощо), повертає мапу `{status, body}`
- `httpServer(port, handler)` - HTTP-сервер; `handler(path, method)` викликається на кожен запит і повертає рядок тіла відповіді. Блокує назавжди (Ctrl+C для зупинки)
- `regexTest(s, pattern)` - чи збігається рядок з regex-шаблоном (bool); `regexMatch(s, pattern)` - перший збіг або `null`; `regexFindAll(s, pattern)` - масив усіх збігів; `regexReplace(s, pattern, replacement)` - заміна всіх збігів
- `guiWindow(title, w, h)`, `guiButton(text, x, y, w, h)`, `guiShow(win)` - GUI (експериментально)

## Як запустити
Використовуйте ArxLang.exe та передайте шлях до файлу:
`ArxLang.exe program.arx`

Інші команди:
- `ArxLang.exe format program.arx` - вивести відформатований код
- `ArxLang.exe lint program.arx` - перевірити код на типові помилки (невикористані змінні, задовгі рядки, порожні блоки)
