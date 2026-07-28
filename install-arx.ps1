# ============================================================
# install-arx.ps1 — глобальна команда "arx"
#
# Після встановлення .arx-файли можна запускати з будь-якої папки:
#     arx myprogram.arx
#
# Запуск:  powershell -ExecutionPolicy Bypass -File install-arx.ps1
# ============================================================

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe  = Join-Path $repo "src\ArxLang\bin\Debug\net10.0-windows\ArxLang.exe"

if (-not (Test-Path $exe)) {
    Write-Host "ArxLang.exe не знайдено." -ForegroundColor Red
    Write-Host "Спочатку зберіть проєкт:  dotnet build src\ArxLang"
    exit 1
}

$binDir = Join-Path $HOME "bin"
if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir | Out-Null
    Write-Host "Створено папку $binDir"
}

# .cmd, а не .ps1 — так команда працює і в cmd, і в PowerShell.
# %* передає всі аргументи далі (ім'я файлу тощо).
$cmdPath = Join-Path $binDir "arx.cmd"
$content = "@echo off`r`n`"$exe`" %*"
Set-Content -Path $cmdPath -Value $content -Encoding ascii
Write-Host "Створено $cmdPath"

# PATH користувача, не системний — прав адміністратора не потрібно.
$userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($userPath -notlike "*$binDir*") {
    [Environment]::SetEnvironmentVariable("PATH", "$userPath;$binDir", "User")
    Write-Host "Додано $binDir до PATH"
} else {
    Write-Host "$binDir вже є в PATH"
}

Write-Host ""
Write-Host "Готово." -ForegroundColor Green
Write-Host "Відкрий НОВЕ вікно термінала (PATH оновлюється лише в нових) і спробуй:"
Write-Host ""
Write-Host "    arx --version" -ForegroundColor Cyan
Write-Host ""
Write-Host "Якщо перезбереш проєкт — команда підхопить нову збірку сама,"
Write-Host "бо посилається на ту саму папку."
