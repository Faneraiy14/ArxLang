# ============================================================
# install-arx.ps1 — глобальна команда "arx"
#
# Після встановлення .arx-файли можна запускати з будь-якої папки:
#     arx myprogram.arx
#
# Запуск (з клону репозиторію, потрібен .NET SDK):
#     powershell -ExecutionPolicy Bypass -File install-arx.ps1
#
# На чужому ПК без .NET і без клону репозиторію — качай самодостатній
# ArxLang.exe зі сторінки Releases репозитрію й запускай install-arx.ps1
# поруч із ним: скрипт знайде .exe в тій же папці, дотнет не знадобиться.
# ============================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Спершу шукаємо вже готовий .exe поруч зі скриптом (так буде на чужому ПК,
# що скачав реліз) або в build-теці (так буде в клоні репозиторію розробника).
$candidates = @(
    (Join-Path $scriptDir "ArxLang.exe"),
    (Join-Path $scriptDir "src\ArxLang\bin\Release\net10.0-windows\win-x64\publish\ArxLang.exe"),
    (Join-Path $scriptDir "src\ArxLang\bin\Debug\net10.0-windows\ArxLang.exe")
)
$exe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $exe) {
    Write-Host "ArxLang.exe не знайдено поруч зі скриптом і не зібрано локально." -ForegroundColor Red
    Write-Host "Або поклади ArxLang.exe (з Releases) у цю ж папку, або зберіть проєкт:"
    Write-Host "    dotnet build src\ArxLang"
    exit 1
}
Write-Host "Знайдено: $exe"

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
