# ArxLang для VS Code

Підсвічування синтаксису `.arx`-файлів: ключові слова (`func`, `var`, `struct`,
`if`/`else`, `while`, `for`/`in`, `try`/`catch`/`throw`, `import`, `break`/`continue`),
типи (`i32`, `f64`, `string`, `bool`), рядки, числа, коментарі (`//` і `/* */`),
виклики функцій та імена структур перед `{`.

## Локальне тестування (без публікації)

1. Відкрий теку `vscode-arxlang` у VS Code.
2. Натисни `F5` — відкриється нове вікно ("Extension Development Host") з
   активованим розширенням. Відкрий у ньому будь-який `.arx`-файл з `tests/`.

## Пакування у `.vsix` (щоб встановити собі без Marketplace)

```bash
npm install -g @vscode/vsce
vsce package
code --install-extension arxlang-0.1.0.vsix
```

Публікація в офіційний VS Code Marketplace — окремий крок (потрібен
Publisher-акаунт на https://marketplace.visualstudio.com), тут не робиться
автоматично.
