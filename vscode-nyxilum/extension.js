// extension.js — базове автодоповнення для .nx-файлів: ключові слова,
// усі вбудовані функції (список продубльовано з Compiler.cs _builtins —
// оновлюй тут при додаванні нової вбудованої функції в рантайм) і символи
// (func/struct/var), знайдені текстовим пошуком у поточному документі.
//
// Це НЕ повноцінний LSP: немає резолву типів, скоупів чи імпортованих
// файлів — лише те, що видно як текст у відкритому документі. Для мови
// без статичних типів цього вистачає для основного сценарію "не
// перепечатувати ім'я функції/змінної вручну".
const vscode = require('vscode');

const KEYWORDS = [
    'func', 'var', 'if', 'else', 'while', 'for', 'return',
    'break', 'continue', 'true', 'false', 'null', 'in',
    'struct', 'self', 'import', 'try', 'catch', 'throw',
];

// Продубльовано з src/NyxilumLang/Compiler/Compiler.cs _builtins.
const BUILTINS = [
    'print', 'printNoNewLine',
    'readLine', 'readInt', 'readDouble',
    'readFile', 'writeFile', 'appendFile', 'fileExists', 'readLines',
    'sqrt', 'abs', 'pow', 'sin', 'cos', 'tan',
    'round', 'floor', 'ceil', 'max', 'min', 'clamp',
    'toString', 'toInt', 'toDouble', 'toFixed', 'len',
    'substring', 'replace', 'toUpper', 'toLower', 'contains', 'startsWith', 'endsWith', 'split', 'join',
    'trim', 'repeat', 'indexOf', 'reverse',
    'append', 'pop', 'removeAt', 'insert', 'clear', 'slice', 'unique',
    'randomInt', 'randomDouble',
    'now', 'today', 'timestamp', 'sleep',
    'typeOf', 'isNumber', 'isString', 'isArray', 'isBool', 'isNull',
    'charCode', 'fromCharCode',
    'newMap', 'mapSet', 'mapGet', 'mapHas', 'mapRemove', 'mapKeys', 'mapValues',
    'sort', 'mapArr', 'filter', 'reduce', 'toJson', 'fromJson', 'callWithArgs',
    'osPlatform', 'osArchitecture', 'osMemory', 'osCpuCount', 'osEnv', 'osCwd',
    'httpServer', 'httpGet', 'urlStatus', 'httpPost', 'httpRequest',
    'regexTest', 'regexMatch', 'regexFindAll', 'regexReplace',
    'wsConnect', 'wsSend', 'wsReceive', 'wsClose',
    'createCanvas', 'clearCanvas', 'drawRect', 'drawCircle', 'drawLine', 'drawText',
    'presentCanvas', 'canvasShouldClose', 'closeCanvas',
    'isKeyDown', 'isMouseDown', 'getMouseX', 'getMouseY', 'project3D',
    'guiWindow', 'guiButton', 'guiLabel', 'guiTextBox', 'guiAdd',
    'guiOnAction', 'guiShow', 'guiSetText', 'guiGetText',
    'gc_stats', 'gc_collect', 'gc_limit', 'exit',
    'dbOpen', 'dbClose', 'dbSet', 'dbGet', 'dbHas', 'dbDelete', 'dbKeys', 'dbCount', 'dbCheckpoint',
];

// Ідентифікатор в NyxilumLang може бути кирилицею (напр. "func подвоїти(x)"),
// тож \w тут не годиться — потрібні unicode property escapes (\p{L}).
const IDENT = '\\p{L}[\\p{L}\\p{N}_]*';
const FUNC_RE = new RegExp(`\\bfunc\\s+(${IDENT})\\s*\\(`, 'gu');
const STRUCT_RE = new RegExp(`\\bstruct\\s+(${IDENT})`, 'gu');
const VAR_RE = new RegExp(`\\bvar\\s+(${IDENT})`, 'gu');

function keywordItems() {
    return KEYWORDS.map((kw) => {
        const item = new vscode.CompletionItem(kw, vscode.CompletionItemKind.Keyword);
        item.detail = 'ключове слово NyxilumLang';
        return item;
    });
}

function builtinItems() {
    return BUILTINS.map((name) => {
        const item = new vscode.CompletionItem(name, vscode.CompletionItemKind.Function);
        item.detail = 'вбудована функція NyxilumLang';
        item.insertText = new vscode.SnippetString(`${name}($0)`);
        return item;
    });
}

// Символи, знайдені текстовим пошуком по всьому відкритому документу —
// не лише в межах поточної області видимості (простіше й для скрипта на
// кілька сотень рядків досить точно; хибний позитив тут не гірший за
// звичайну відсутність автодоповнення).
function documentSymbolItems(document) {
    const text = document.getText();
    const items = [];
    const seen = new Set();

    const addAll = (regex, kind, detail, asCall) => {
        for (const match of text.matchAll(regex)) {
            const name = match[1];
            const key = kind + ':' + name;
            if (seen.has(key)) continue;
            seen.add(key);
            const item = new vscode.CompletionItem(name, kind);
            item.detail = detail;
            if (asCall) item.insertText = new vscode.SnippetString(`${name}($0)`);
            items.push(item);
        }
    };

    addAll(FUNC_RE, vscode.CompletionItemKind.Function, 'функція (з цього файлу)', true);
    addAll(STRUCT_RE, vscode.CompletionItemKind.Struct, 'структура (з цього файлу)', false);
    addAll(VAR_RE, vscode.CompletionItemKind.Variable, 'змінна (з цього файлу)', false);

    return items;
}

function activate(context) {
    const provider = vscode.languages.registerCompletionItemProvider(
        'nyxilum',
        {
            provideCompletionItems(document) {
                return [
                    ...keywordItems(),
                    ...builtinItems(),
                    ...documentSymbolItems(document),
                ];
            },
        },
        // Триґер і на звичайне введення літери (VS Code сам фільтрує список
        // за вже набраним префіксом), і явно на '.' — на майбутнє, якщо
        // з'явиться доступ до полів структур через крапку в автодоповненні.
    );
    context.subscriptions.push(provider);
}

function deactivate() {}

module.exports = { activate, deactivate };
