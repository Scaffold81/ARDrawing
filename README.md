# AR Drawing Application

Unity AR-приложение для рисования в виртуальном пространстве с использованием отслеживания рук для Oculus Quest 2/3.

## Архитектура

### MVP + Reactive Programming + DI

Проект использует современную архитектуру с четким разделением ответственности:

- **MVP Pattern**: Разделение презентационной логики
- **Reactive Programming (UniRx/R3)**: Реактивные потоки данных
- **Dependency Injection (Zenject)**: Слабая связанность компонентов
- **Object Pooling**: Оптимизация производительности

### Структура слоев

```
┌─────────────────────────────────────┐
│ UI Layer (Phase 4)                  │
│ ├── UIPresenter (MVP)               │
│ ├── MainPanel, ColorPickerPanel     │
├─────────────────────────────────────┤
│ Presentation Layer                  │
│ ├── DrawingPresenter                │
│ └── ARDrawingView, ARLineVisualizer │
├─────────────────────────────────────┤
│ Core Layer                          │
│ ├── Services (Drawing, HandTracking)│
│ ├── Models (DrawingLine, Settings)  │
│ └── Interfaces                      │
└─────────────────────────────────────┘
```

## Технологический стек

- **Unity 6+ LTS**
- **Oculus Integration SDK** - AR/VR поддержка
- **UniRx (R3)** - реактивное программирование
- **Zenject** - dependency injection
- **Oculus Hand Tracking API** - отслеживание рук

## Основные компоненты

### UI System 
- `UIPresenter` - главный MVP presenter
- `MainPanel` - панель управления (цвет, очистка, отмена)
- `ColorPickerPanel` - выбор цвета
- `InteractableButton` - AR кнопки с finger tracking

### Drawing System
- `DrawingService` - управление линиями
- `DrawingLine` - модель линии с точками
- `ARLineVisualizer` - визуализация линий

### Hand Tracking
- `IHandTrackingService` - интерфейс отслеживания
- `OculusHandTrackingService` - реализация для Oculus

### Save/Load System
- `ISaveLoadService` - интерфейс сохранения
- `JsonSaveLoadService` - JSON сериализация

## Ключевые особенности

### Reactive Programming (R3)
```csharp
// Пример реактивного потока
drawingService.ActiveLines
    .Subscribe(lines => UpdateUI(lines))
    .AddTo(this);
```

### Dependency Injection (Zenject)
```csharp
[Inject] private IDrawingService drawingService;
[Inject] private IHandTrackingService handTracking;
```

### Object Pooling
- Пул LineRenderer для оптимизации
- Переиспользование объектов UI

### Performance Monitoring
- `MemoryLeakDetector` - детекция утечек памяти
- `MemoryLeakFixer` - автоматическое исправление

## Установка и настройка

1. Unity 6+ LTS
2. Импорт Oculus Integration SDK
3. Настройка XR для Oculus Quest
4. Установка пакетов: UniRx, Zenject

Все системы тестирования отключены и перенесены в папки `*_Disabled` для production сборки.

## Производительность

- Object Pooling для LineRenderer
- Throttling обновлений пальца
- Эффективная память через CompositeDisposable
- Мониторинг утечек памяти
