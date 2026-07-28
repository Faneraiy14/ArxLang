#!/bin/bash
# ============================================================
# run_all.sh — прогін усіх тестів ArxLang
#
# Запуск:  bash tests/run_all.sh
#
# Тест вважається успішним, якщо він вивів очікуваний результат і не
# впав з непередбаченою помилкою. Тести, які НАВМИСНЕ перевіряють
# помилку (напр. необроблений throw), перелічені в EXPECT_ERROR —
# для них помилка це і є успіх, а її ВІДСУТНІСТЬ — провал.
# ============================================================

TESTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXE="${ARX_EXE:-$TESTS_DIR/../src/ArxLang/bin/Debug/net10.0-windows/ArxLang.exe}"

# Відкривають вікна або чекають на ввід — у автоматичному прогоні пропускаємо.
SKIP="test_graphics2d.arx test_graphics3d.arx calculator.arx"

# Тести, де помилка — очікуваний результат.
EXPECT_ERROR="test_throw_uncaught.arx"

TIMEOUT_SEC=25

cd "$TESTS_DIR" || exit 1

if [ ! -f "$EXE" ]; then
    echo "Не знайдено ArxLang.exe: $EXE"
    echo "Спочатку зберіть: dotnet build src/ArxLang"
    exit 1
fi

pass=0
fail=0
skip=0
failed=()

in_list() {
    case " $2 " in *" $1 "*) return 0;; *) return 1;; esac
}

for f in *.arx; do
    [ -e "$f" ] || continue

    if in_list "$f" "$SKIP"; then
        echo "⏭️  $f — пропущено (графіка/інтерактив)"
        skip=$((skip+1))
        continue
    fi

    out=$(timeout "$TIMEOUT_SEC" "$EXE" "$f" 2>&1)
    code=$?

    if echo "$out" | grep -qiE "Runtime Error|Parse Error|Unhandled exception"; then
        has_error=1
    else
        has_error=0
    fi

    if [ $code -eq 124 ]; then
        echo "⏱️  $f — таймаут (${TIMEOUT_SEC}с)"
        fail=$((fail+1)); failed+=("$f — таймаут")
        continue
    fi

    if in_list "$f" "$EXPECT_ERROR"; then
        if [ $has_error -eq 1 ]; then
            echo "✅ $f — помилка очікувана й отримана"
            pass=$((pass+1))
        else
            echo "❌ $f — мала бути помилка, але її немає"
            fail=$((fail+1)); failed+=("$f — очікувалась помилка")
        fi
        continue
    fi

    if [ $has_error -eq 1 ]; then
        echo "❌ $f"
        echo "$out" | grep -iE "Runtime Error|Parse Error|Unhandled exception" | head -2 | sed 's/^/       /'
        fail=$((fail+1)); failed+=("$f")
    else
        echo "✅ $f"
        pass=$((pass+1))
    fi
done

# Модульні тести лежать в окремій підпапці й запускаються через main.arx
if [ -f "modules/main.arx" ]; then
    out=$(cd modules && timeout "$TIMEOUT_SEC" "$EXE" main.arx 2>&1)
    if echo "$out" | grep -qiE "Runtime Error|Parse Error|Unhandled exception"; then
        echo "❌ modules/main.arx"
        fail=$((fail+1)); failed+=("modules/main.arx")
    else
        echo "✅ modules/main.arx"
        pass=$((pass+1))
    fi
fi

echo
echo "======================================"
echo "Успішно: $pass | Провалено: $fail | Пропущено: $skip"

if [ ${#failed[@]} -gt 0 ]; then
    echo
    echo "Проблемні:"
    printf '  %s\n' "${failed[@]}"
    exit 1
fi

echo "Усі тести пройдено."
