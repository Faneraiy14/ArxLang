# Nyxilum для VS Code

Підсвічування синтаксису `.nx`-файлів: ключові слова (`func`, `var`, `struct`,
`if`/`else`, `while`, `for`/`in`, `try`/`catch`/`throw`, `import`, `break`/`continue`),
типи (`i32`, `f64`, `string`, `bool`), рядки, числа, коментарі (`//` і `/* */`),
виклики функцій та імена структур перед `{`.

## Локальне тестування (без публікації)

1. Відкрий теку `vscode-nyxilum` у VS Code.
2. Натисни `F5` — відкриється нове вікно ("Extension Development Host") з
   активованим розширенням. Відкрий у ньому будь-який `.nx`-файл з `tests/`.

## Пакування у `.vsix` (щоб встановити собі без Marketplace)

```bash
npm install -g @vscode/vsce
vsce package
code --install-extension nyxilum-0.3.0.vsix
```

Публікація в офіційний VS Code Marketplace — окремий крок (потрібен
Publisher-акаунт на https://marketplace.visualstudio.com), тут не робиться
автоматично.
