# 🧪 Testing Guide - Phase 2.3: TouchStateManager

## 📋 Подготовка к тестированию

### 1. Создание тестовой сцены

1. **Создай новую сцену:**
   - File → New Scene
   - Сохрани как `TouchStateTest.unity`

2. **Добавь HandTrackingSimulator:**
   ```
   1. Создай пустой GameObject: "HandTrackingSimulator"
   2. Добавь компонент HandTrackingSimulator
   3. Настрой параметры:
      - Enable Simulation: ✅ true
      - Move Speed: 2.0
      - Pinch Key: Space
      - Reset Key: R
      - Show Debug GUI: ✅ true
   ```

3. **Настрой камеру:**
   - Позиция: (0, 1.5, -2)
   - Rotation: (0, 0, 0)

## 🎮 Тесты функциональности

### Тест 1: Базовое движение пальца
**Цель:** Проверить отслеживание позиции
```
✅ Действие: Двигай мышь
✅ Ожидаемый результат: Зеленая сфера следует за мышью
✅ Проверь: GUI показывает изменение Position
```

### Тест 2: Pinch жест - Space
**Цель:** Проверить определение касания через клавиатуру
```
✅ Действие: Нажми и держи Space
✅ Ожидаемый результат: 
   - Сфера становится желтой (Started)
   - Затем красной (Active) 
   - GUI: Touch State = Active, Pinching = Yes
✅ Отпусти Space
✅ Ожидаемый результат:
   - Сфера становится оранжевой (Ended)
   - Затем зеленой (None)
```

### Тест 3: Pinch жест - Мышь
**Цель:** Проверить определение касания через ЛКМ
```
✅ Действие: Нажми и держи ЛКМ
✅ Ожидаемый результат: Аналогично Space
```

### Тест 4: Настройки TouchDetection
**Цель:** Проверить изменение параметров в реальном времени
```
✅ Действие: Измени Threshold slider в GUI
✅ Ожидаемый результат: Консоль показывает "Settings updated"
✅ Действие: Нажми "Update Settings"
✅ Проверь: Изменение чувствительности касания
```

### Тест 5: Стрелки клавиатуры
**Цель:** Точное движение
```
✅ Действие: Используй стрелки ←↑↓→
✅ Ожидаемый результат: Точное движение сферы
✅ Действие: Page Up/Down
✅ Ожидаемый результат: Движение по оси Z
```

### Тест 6: Сброс позиции
**Цель:** Функция Reset
```
✅ Действие: Нажми R или кнопку "Reset Position"
✅ Ожидаемый результат: Сфера возвращается в (0, 1.5, 1)
```

## 🔍 Console Debug Messages

### Ожидаемые сообщения:
```
✅ "HandTrackingSimulator: Initialized with TouchStateManager"
✅ "TouchStateManager: Initialized with improved touch detection"
✅ "TouchStateManager: Touch started at (x,y,z)"
✅ "HandTrackingSimulator: Touch state changed to Started"
✅ "HandTrackingSimulator: Touch state changed to Active"
✅ "TouchStateManager: Touch ended, duration: X.XXXs"
✅ "HandTrackingSimulator: Touch state changed to Ended"
✅ "TouchStateManager: Touch sequence completed"
```

## 🐛 Проверка на ошибки

### Частые проблемы:
```
❌ Сфера не появляется → Проверь создание FingerCursor
❌ GUI не отображается → Show Debug GUI = true
❌ Нет реакции на мышь → Проверь камеру в сцене
❌ Console errors → Проверь все using директивы
```

## 📊 Тест производительности

### В режиме Play:
```
✅ FPS должен быть стабильным (60+ FPS)
✅ Smooth движение без рывков
✅ Быстрая реакция на input (< 16ms)
✅ Память: без утечек при длительном использовании
```

## 🎯 Тест интеграции

### Если есть Meta Quest в проекте:
```
1. Замени HandTrackingSimulator на OpenXRHandTrackingService
2. Протестируй с реальными руками
3. Проверь автоматический поиск OVR компонентов
```

## ✅ Критерии успеха

### Phase 2.3 считается успешным если:
```
✅ Все 6 тестов проходят без ошибок
✅ Плавные переходы между состояниями касания
✅ Стабильная работа TouchStateManager
✅ GUI корректно отображает все данные
✅ Нет ошибок в Console
✅ Настройки меняются в реальном времени
```

## 🚀 Следующие шаги

После успешного тестирования:
```
1. ✅ Phase 2.3 завершен
2. 🎯 Переход к Phase 2.4: R3 Observable интеграция
3. 🔧 Добавление продвинутых операторов фильтрации
4. 📈 Оптимизация производительности
```
