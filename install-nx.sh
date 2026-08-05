#!/usr/bin/env bash
# ============================================================
# install-nx.sh — глобальна команда "nx" (Linux/Mac)
#
# Запуск (з розпакованого архіву релізу, поруч із бінарником Nx):
#     bash install-nx.sh
#
# Без GUI/графіки — Windows Forms працює лише на Windows. Решта мови
# (компілятор, VM, майже вся стандартна бібліотека) працює однаково.
# ============================================================
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Спершу шукаємо готовий бінарник поруч зі скриптом (реліз), потім
# у build-теці (клон репозиторію розробника).
candidates=(
    "$script_dir/Nx"
    "$script_dir/src/NyxilumLang/bin/Release/net10.0/linux-x64/publish/Nx"
    "$script_dir/src/NyxilumLang/bin/Release/net10.0/osx-x64/publish/Nx"
    "$script_dir/src/NyxilumLang/bin/Release/net10.0/osx-arm64/publish/Nx"
    "$script_dir/src/NyxilumLang/bin/Debug/net10.0/Nx"
)

exe=""
for candidate in "${candidates[@]}"; do
    if [ -f "$candidate" ]; then
        exe="$candidate"
        break
    fi
done

if [ -z "$exe" ]; then
    echo "Nx не знайдено поруч зі скриптом і не зібрано локально." >&2
    echo "Або поклади бінарник Nx (з Releases) у цю ж папку, або зберіть:" >&2
    echo "    dotnet build src/NyxilumLang -f net10.0" >&2
    exit 1
fi

chmod +x "$exe"
echo "Знайдено: $exe"

bin_dir="$HOME/.local/bin"
mkdir -p "$bin_dir"

wrapper="$bin_dir/nx"
cat > "$wrapper" <<EOF
#!/usr/bin/env bash
exec "$exe" "\$@"
EOF
chmod +x "$wrapper"
echo "Створено $wrapper"

# ~/.local/bin є в PATH за замовчуванням у більшості дистрибутивів
# (systemd user-session, XDG), але не завжди — додаємо явно в shell rc,
# якщо його там ще немає.
add_to_rc() {
    local rc="$1"
    [ -f "$rc" ] || return 0
    if ! grep -q '.local/bin' "$rc" 2>/dev/null; then
        echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$rc"
        echo "Додано \$HOME/.local/bin у PATH через $rc"
    fi
}
add_to_rc "$HOME/.bashrc"
add_to_rc "$HOME/.zshrc"

echo ""
echo "Готово."
echo "Відкрий НОВИЙ термінал (або виконай 'source ~/.bashrc' чи 'source ~/.zshrc') і спробуй:"
echo ""
echo "    nx --version"
echo ""
echo "GUI (guiWindow тощо) і графіка (createCanvas тощо) недоступні поза"
echo "Windows — Windows Forms там не працює. Решта мови працює як завжди."
