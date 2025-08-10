using UnityEngine;
using ARDrawing.Core.Services;
using ARDrawing.Core.Models;
using ARDrawing.Core.Config;
using R3;
using System;

namespace ARDrawing.Testing
{
    /// <summary>
    /// Тестовый компонент для проверки функциональности DrawingService.
    /// Test component for DrawingService functionality verification.
    /// </summary>
    public class DrawingServiceTester : MonoBehaviour
    {
        [Header("Testing Configuration")]
        [SerializeField] private DrawingService _drawingService;
        [SerializeField] private DrawingSettingsConfig _testSettings;
        [SerializeField] private Material _testLineMaterial;
        
        [Header("Test Controls")]
        [SerializeField] private bool _enableAutomaticTesting = false;
        [SerializeField] private float _testInterval = 2.0f;
        [SerializeField] private int _maxTestLines = 5;
        
        [Header("Manual Test Controls")]
        [SerializeField] private KeyCode _startLineKey = KeyCode.Space;
        [SerializeField] private KeyCode _endLineKey = KeyCode.Return;
        [SerializeField] private KeyCode _clearAllKey = KeyCode.C;
        [SerializeField] private KeyCode _changeColorKey = KeyCode.X;
        [SerializeField] private KeyCode _changeWidthKey = KeyCode.Z;
        
        [Header("Test Drawing Area")]
        [SerializeField] private Vector3 _testAreaCenter = Vector3.zero;
        [SerializeField] private Vector3 _testAreaSize = new Vector3(2f, 2f, 2f);
        [SerializeField] private bool _showTestArea = true;
        
        [Header("Debug Info")]
        [SerializeField] private bool _showDebugInfo = true;
        [SerializeField] private bool _logPoolStats = false;
        
        // Тестовые данные
        private IDisposable _activeLinesSubscription;
        private IDisposable _isDrawingSubscription;
        private bool _isCurrentlyDrawing = false;
        private float _lastTestTime;
        private int _currentColorIndex = 0;
        private int _currentWidthIndex = 0;
        private Vector3 _currentTestPosition;
        
        // Статистика
        private int _totalLinesCreated = 0;
        private int _totalPointsAdded = 0;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeTester();
        }
        
        private void Update()
        {
            HandleManualInput();
            HandleAutomaticTesting();
            UpdateTestPosition();
        }
        
        private void OnDrawGizmos()
        {
            if (_showTestArea)
            {
                DrawTestArea();
            }
        }
        
        private void OnDestroy()
        {
            CleanupTester();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeTester()
        {
            // Поиск DrawingService если не назначен
            if (_drawingService == null)
            {
                _drawingService = FindFirstObjectByType<DrawingService>();
                if (_drawingService == null)
                {
                    Debug.LogError("[DrawingServiceTester] DrawingService not found!");
                    return;
                }
            }
            
            // Установка тестовых настроек
            if (_testSettings != null)
            {
                _drawingService.SetDrawingSettings(_testSettings.ToDrawingSettings());
            }
            
            // Подписка на Observable потоки
            SubscribeToDrawingService();
            
            // Инициализация тестовой позиции
            _currentTestPosition = _testAreaCenter;
            
            Debug.Log("[DrawingServiceTester] Initialized successfully");
            
            // Показать доступные команды
            ShowTestCommands();
        }
        
        private void SubscribeToDrawingService()
        {
            if (_drawingService == null) return;
            
            // Подписка на изменения активных линий
            _activeLinesSubscription = _drawingService.ActiveLines
                .Subscribe(lines =>
                {
                    if (_showDebugInfo)
                    {
                        Debug.Log($"[DrawingServiceTester] Active lines count: {lines.Count}");
                    }
                });
            
            // Подписка на изменения состояния рисования
            _isDrawingSubscription = _drawingService.IsDrawing
                .Subscribe(isDrawing =>
                {
                    _isCurrentlyDrawing = isDrawing;
                    if (_showDebugInfo)
                    {
                        Debug.Log($"[DrawingServiceTester] Drawing state changed: {isDrawing}");
                    }
                });
        }
        
        private void ShowTestCommands()
        {
            Debug.Log($"[DrawingServiceTester] Available test commands:\n" +
                     $"- {_startLineKey}: Start new line\n" +
                     $"- {_endLineKey}: End current line\n" +
                     $"- {_clearAllKey}: Clear all lines\n" +
                     $"- {_changeColorKey}: Change color\n" +
                     $"- {_changeWidthKey}: Change width\n" +
                     $"- Automatic testing: {_enableAutomaticTesting}");
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleManualInput()
        {
            // Начать новую линию
            if (Input.GetKeyDown(_startLineKey))
            {
                StartTestLine();
            }
            
            // Завершить текущую линию
            if (Input.GetKeyDown(_endLineKey))
            {
                EndTestLine();
            }
            
            // Очистить все линии
            if (Input.GetKeyDown(_clearAllKey))
            {
                ClearAllTestLines();
            }
            
            // Изменить цвет
            if (Input.GetKeyDown(_changeColorKey))
            {
                ChangeTestColor();
            }
            
            // Изменить толщину
            if (Input.GetKeyDown(_changeWidthKey))
            {
                ChangeTestWidth();
            }
            
            // Добавление точек во время рисования
            if (_isCurrentlyDrawing && Input.GetKey(_startLineKey))
            {
                AddTestPoint();
            }
        }
        
        private void HandleAutomaticTesting()
        {
            if (!_enableAutomaticTesting) return;
            
            if (Time.time - _lastTestTime >= _testInterval)
            {
                PerformAutomaticTest();
                _lastTestTime = Time.time;
            }
        }
        
        #endregion
        
        #region Test Methods
        
        private void StartTestLine()
        {
            if (_drawingService == null) return;
            
            if (!_drawingService.CanStartNewLine())
            {
                Debug.LogWarning("[DrawingServiceTester] Cannot start new line - limit reached or already drawing");
                return;
            }
            
            Vector3 startPosition = GetRandomTestPosition();
            _drawingService.StartLine(startPosition);
            _totalLinesCreated++;
            
            Debug.Log($"[DrawingServiceTester] Started test line at {startPosition}");
        }
        
        private void AddTestPoint()
        {
            if (_drawingService == null || !_isCurrentlyDrawing) return;
            
            Vector3 nextPosition = GetNextTestPosition();
            _drawingService.AddPointToLine(nextPosition);
            _totalPointsAdded++;
        }
        
        private void EndTestLine()
        {
            if (_drawingService == null) return;
            
            _drawingService.EndLine();
            Debug.Log("[DrawingServiceTester] Ended test line");
        }
        
        private void ClearAllTestLines()
        {
            if (_drawingService == null) return;
            
            _drawingService.ClearAllLines();
            _totalLinesCreated = 0;
            _totalPointsAdded = 0;
            Debug.Log("[DrawingServiceTester] Cleared all test lines");
        }
        
        private void ChangeTestColor()
        {
            if (_drawingService == null || _testSettings == null) return;
            
            var availableColors = _testSettings.GetAvailableColors();
            _currentColorIndex = (_currentColorIndex + 1) % availableColors.Length;
            
            var currentSettings = _testSettings.ToDrawingSettings();
            currentSettings.LineColor = availableColors[_currentColorIndex];
            _drawingService.SetDrawingSettings(currentSettings);
            
            Debug.Log($"[DrawingServiceTester] Changed color to: {availableColors[_currentColorIndex]}");
        }
        
        private void ChangeTestWidth()
        {
            if (_drawingService == null || _testSettings == null) return;
            
            var availableWidths = _testSettings.GetLineWidthPresets();
            _currentWidthIndex = (_currentWidthIndex + 1) % availableWidths.Length;
            
            var currentSettings = _testSettings.ToDrawingSettings();
            currentSettings.LineWidth = availableWidths[_currentWidthIndex];
            _drawingService.SetDrawingSettings(currentSettings);
            
            Debug.Log($"[DrawingServiceTester] Changed width to: {availableWidths[_currentWidthIndex]}");
        }
        
        private void PerformAutomaticTest()
        {
            if (_drawingService == null) return;
            
            var activeLinesCopy = _drawingService.GetActiveLinesCopy();
            
            if (activeLinesCopy.Count >= _maxTestLines)
            {
                // Очищаем и начинаем заново
                ClearAllTestLines();
                return;
            }
            
            if (!_isCurrentlyDrawing)
            {
                // Случайно меняем настройки
                if (UnityEngine.Random.value < 0.3f) ChangeTestColor();
                if (UnityEngine.Random.value < 0.2f) ChangeTestWidth();
                
                // Начинаем новую линию
                StartTestLine();
            }
            else
            {
                // Добавляем несколько точек
                for (int i = 0; i < UnityEngine.Random.Range(3, 8); i++)
                {
                    AddTestPoint();
                }
                
                // С вероятностью завершаем линию
                if (UnityEngine.Random.value < 0.7f)
                {
                    EndTestLine();
                }
            }
        }
        
        #endregion
        
        #region Position Calculation
        
        private Vector3 GetRandomTestPosition()
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-_testAreaSize.x * 0.5f, _testAreaSize.x * 0.5f),
                UnityEngine.Random.Range(-_testAreaSize.y * 0.5f, _testAreaSize.y * 0.5f),
                UnityEngine.Random.Range(-_testAreaSize.z * 0.5f, _testAreaSize.z * 0.5f)
            );
            
            return _testAreaCenter + randomOffset;
        }
        
        private Vector3 GetNextTestPosition()
        {
            // Случайное движение от текущей позиции
            Vector3 movement = new Vector3(
                UnityEngine.Random.Range(-0.1f, 0.1f),
                UnityEngine.Random.Range(-0.1f, 0.1f),
                UnityEngine.Random.Range(-0.1f, 0.1f)
            );
            
            _currentTestPosition += movement;
            
            // Ограничение в пределах тестовой области
            _currentTestPosition = Vector3.Max(_currentTestPosition, _testAreaCenter - _testAreaSize * 0.5f);
            _currentTestPosition = Vector3.Min(_currentTestPosition, _testAreaCenter + _testAreaSize * 0.5f);
            
            return _currentTestPosition;
        }
        
        private void UpdateTestPosition()
        {
            if (!_isCurrentlyDrawing) return;
            
            // Плавное движение курсора для симуляции рисования
            float time = Time.time;
            Vector3 targetOffset = new Vector3(
                Mathf.Sin(time * 2f) * 0.3f,
                Mathf.Cos(time * 1.5f) * 0.2f,
                Mathf.Sin(time * 0.8f) * 0.1f
            );
            
            _currentTestPosition = Vector3.Lerp(_currentTestPosition, _testAreaCenter + targetOffset, Time.deltaTime);
        }
        
        #endregion
        
        #region Gizmos and Debug
        
        private void DrawTestArea()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_testAreaCenter, _testAreaSize);
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_currentTestPosition, 0.02f);
        }
        
        #endregion
        
        #region Public Test Methods
        
        /// <summary>
        /// Выполняет полный тест функциональности DrawingService.
        /// Performs complete DrawingService functionality test.
        /// </summary>
        [ContextMenu("Run Full Test")]
        public void RunFullTest()
        {
            if (_drawingService == null)
            {
                Debug.LogError("[DrawingServiceTester] DrawingService not found for full test!");
                return;
            }
            
            Debug.Log("[DrawingServiceTester] Starting full functionality test...");
            
            // Тест 1: Создание линий
            TestLineCreation();
            
            // Тест 2: Изменение настроек
            TestSettingsChange();
            
            // Тест 3: Пул объектов
            TestObjectPooling();
            
            // Тест 4: Очистка
            TestClearing();
            
            Debug.Log("[DrawingServiceTester] Full test completed!");
        }
        
        private void TestLineCreation()
        {
            Debug.Log("[DrawingServiceTester] Testing line creation...");
            
            for (int i = 0; i < 3; i++)
            {
                Vector3 startPos = GetRandomTestPosition();
                _drawingService.StartLine(startPos);
                
                for (int j = 0; j < 10; j++)
                {
                    _drawingService.AddPointToLine(GetNextTestPosition());
                }
                
                _drawingService.EndLine();
            }
            
            Debug.Log($"[DrawingServiceTester] Created 3 test lines with 10 points each");
        }
        
        private void TestSettingsChange()
        {
            Debug.Log("[DrawingServiceTester] Testing settings change...");
            
            if (_testSettings != null)
            {
                var colors = _testSettings.GetAvailableColors();
                var widths = _testSettings.GetLineWidthPresets();
                
                var newSettings = _testSettings.ToDrawingSettings();
                newSettings.LineColor = colors[UnityEngine.Random.Range(0, colors.Length)];
                newSettings.LineWidth = widths[UnityEngine.Random.Range(0, widths.Length)];
                
                _drawingService.SetDrawingSettings(newSettings);
                Debug.Log($"[DrawingServiceTester] Changed settings to Color: {newSettings.LineColor}, Width: {newSettings.LineWidth}");
            }
        }
        
        private void TestObjectPooling()
        {
            Debug.Log("[DrawingServiceTester] Testing object pooling...");
            
            var poolStats = _drawingService.GetPoolStatistics();
            Debug.Log($"[DrawingServiceTester] Pool stats - Total: {poolStats.TotalCreated}, " +
                     $"Active: {poolStats.ActiveObjects}, Available: {poolStats.AvailableInPool}");
        }
        
        private void TestClearing()
        {
            Debug.Log("[DrawingServiceTester] Testing clearing...");
            
            _drawingService.ClearAllLines();
            var linesAfterClear = _drawingService.GetActiveLinesCopy();
            
            Debug.Log($"[DrawingServiceTester] Lines after clear: {linesAfterClear.Count} (should be 0)");
        }
        
        /// <summary>
        /// Получает информацию о текущем состоянии тестирования.
        /// Gets current testing state information.
        /// </summary>
        /// <returns>Информация о состоянии / State information</returns>
        public string GetTestingInfo()
        {
            if (_drawingService == null) return "DrawingService not available";
            
            return $"Drawing Service Tester Info:\n" +
                   $"- Total Lines Created: {_totalLinesCreated}\n" +
                   $"- Total Points Added: {_totalPointsAdded}\n" +
                   $"- Currently Drawing: {_isCurrentlyDrawing}\n" +
                   $"- Automatic Testing: {_enableAutomaticTesting}\n" +
                   $"- Current Color Index: {_currentColorIndex}\n" +
                   $"- Current Width Index: {_currentWidthIndex}\n" +
                   $"{_drawingService.GetServiceInfo()}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupTester()
        {
            _activeLinesSubscription?.Dispose();
            _isDrawingSubscription?.Dispose();
            
            Debug.Log("[DrawingServiceTester] Cleaned up subscriptions");
        }
        
        #endregion
    }
}