
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
    x, y
}

func Point.move(dx, dy) {
    this.x = this.x + dx
    this.y = this.y + dy
}

func main() {
    var p = Point { x: 10, y: 20 }
    p.move(5, 5)
    print(p.x) // 15
}
```

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
- `sqrt(x)`, `sin(x)`, `cos(x)`, `tan(x)`, `pow(x,y)`, `abs(x)`, `round/floor/ceil(x)`, `max/min(a,b)`, `clamp(x,min,max)` - математика
- `readFile(path)`, `writeFile(path, content)`, `appendFile(path, content)`, `fileExists(path)`, `readLines(path)` - робота з файлами
- `toString(v)`, `toInt(v)`, `toDouble(v)`, `typeOf(v)`, `isNumber/isString/isArray/isBool(v)` - перетворення та перевірка типів
- `charCode(s)` - код першого символу рядка (наприклад, `charCode("A")` -> 65); `fromCharCode(code)` - символ за кодом
- `len(v)`, `substring(s,start,len)`, `replace/toUpper/toLower/contains/startsWith/endsWith(s,...)`, `split(s,sep)`, `join(arr,sep)` - рядки
- `append(arr,v)`, `pop(arr)`, `insert(arr,i,v)`, `removeAt(arr,i)`, `clear(arr)` - масиви
- `newMap()`, `mapSet(m,k,v)`, `mapGet(m,k)`, `mapHas(m,k)`, `mapRemove(m,k)`, `mapKeys(m)`, `mapValues(m)` - мапи/словники
- `sort(arr,cmp)`, `mapArr(arr,fn)`, `filter(arr,fn)`, `reduce(arr,fn,init)` - функції вищого порядку над масивами
- `toJson(v)`, `fromJson(str)` - серіалізація в JSON і назад
- `sleep(ms)` - пауза виконання
- `createCanvas(title,w,h)`, `clearCanvas`, `drawRect`, `drawCircle`, `drawLine`, `drawText`, `presentCanvas`, `canvasShouldClose`, `closeCanvas` - 2D графіка
- `project3D(canvas,x,y,z,camDistance)` - проекція 3D-точки в 2D для рендеру 3D-сцен
- `isKeyDown(key)`, `isMouseDown(canvas)`, `getMouseX/Y(canvas)` - ввід для вікна
- `randomInt(min,max)`, `randomDouble(min,max)`, `now()`, `today()`, `timestamp()` - утиліти
- `osPlatform()`, `osArchitecture()`, `osMemory()`, `osCpuCount()`, `osEnv(name)`, `osCwd()` - інформація про систему
- `httpGet(url)`, `httpServer(port)` - мережа (експериментально)
- `guiWindow(title, w, h)`, `guiButton(text, x, y, w, h)`, `guiShow(win)` - GUI (експериментально)

## Як запустити
Використовуйте ArxLang.exe та передайте шлях до файлу:
`ArxLang.exe program.arx`

Інші команди:
- `ArxLang.exe format program.arx` - вивести відформатований код
- `ArxLang.exe lint program.arx` - перевірити код на типові помилки (невикористані змінні, задовгі рядки, порожні блоки)
