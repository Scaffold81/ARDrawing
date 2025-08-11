using UnityEngine;
using R3;
using System.Collections.Generic;
using System.Linq;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using ARDrawing.Core.Utils;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Сервис для управления рисованием линий в AR пространстве с объектным пулингом.
    /// Service for managing line drawing in AR space with object pooling.
    /// </summary>
    public class DrawingService : MonoBehaviour, IDrawingService
    {
        [Header("Drawing Configuration")]
        [SerializeField] private DrawingSettings _drawingSettings;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Transform _linesParent;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = true;
        [SerializeField] private bool _showPoolStats = false;
        
        // Observable потоки
        private readonly Subject<List<DrawingLine>> _activeLinesSubject = new Subject<List<DrawingLine>>();
        private readonly Subject<bool> _isDrawingSubject = new Subject<bool>();
        
        // Основные данные
        private readonly List<DrawingLine> _activeLines = new List<DrawingLine>();
        private readonly List<PooledLineRenderer> _activeRenderers = new List<PooledLineRenderer>();
        private DrawingLine _currentLine;
        private PooledLineRenderer _currentRenderer;
        private bool _isDrawing;
        
        // Пул объектов
        private ObjectPool<PooledLineRenderer> _lineRendererPool;
        
        // Настройки
        private DrawingSettings _currentSettings;
        
        #region IDrawingService Implementation
        
        public Observable<List<DrawingLine>> ActiveLines => _activeLinesSubject;
        public Observable<bool> IsDrawing => _isDrawingSubject;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            Debug.Log("[DrawingService] Awake called - starting initialization...");
            InitializeService();
        }
        
        private void OnDestroy()
        {
            DisposeService();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeService()
        {
            // Проверка на повторную инициализацию
            if (_lineRendererPool != null)
            {
                if (_enableDebugLog)
                    Debug.Log("[DrawingService] Already initialized, skipping...");
                return;
            }
            
            if (_enableDebugLog)
                Debug.Log("[DrawingService] Starting initialization...");
            
            // Настройки по умолчанию
            _currentSettings = _drawingSettings ?? DrawingSettings.Default;
            
            // Создание родительского объекта для линий
            if (_linesParent == null)
            {
                var linesContainer = new GameObject("DrawingLines");
                linesContainer.transform.SetParent(transform);
                _linesParent = linesContainer.transform;
                
                if (_enableDebugLog)
                    Debug.Log("[DrawingService] Created lines container");
            }
            
            // Инициализация пула
            InitializeLineRendererPool();
            
            // Инициализация состояния
            _isDrawing = false;
            _isDrawingSubject.OnNext(_isDrawing);
            _activeLinesSubject.OnNext(new List<DrawingLine>(_activeLines));
            
            if (_enableDebugLog)
            {
                Debug.Log($"[DrawingService] Initialized with pool size: {_currentSettings.LinePoolInitialSize}");
            }
        }
        
        private void InitializeLineRendererPool()
        {
            _lineRendererPool = new ObjectPool<PooledLineRenderer>(
                createFunc: CreateLineRenderer,
                onTake: OnLineRendererTaken,
                onReturn: OnLineRendererReturned,
                initialSize: _currentSettings.LinePoolInitialSize,
                maxSize: _currentSettings.LinePoolMaxSize
            );
        }
        
        private PooledLineRenderer CreateLineRenderer()
        {
            if (_enableDebugLog)
                Debug.Log("[DrawingService] Creating new LineRenderer...");
                
            var lineObject = new GameObject("PooledLine");
            lineObject.transform.SetParent(_linesParent);
            
            var lineRenderer = lineObject.AddComponent<LineRenderer>();
            
            if (_enableDebugLog)
                Debug.Log($"[DrawingService] LineRenderer created, configuring with material: {(_lineMaterial != null ? _lineMaterial.name : "NULL")}");
                
            ConfigureLineRenderer(lineRenderer);
            
            var pooledRenderer = new PooledLineRenderer(lineRenderer);
            pooledRenderer.Deactivate(); // Изначально неактивен
            
            if (_enableDebugLog)
                Debug.Log("[DrawingService] PooledLineRenderer created and deactivated");
            
            return pooledRenderer;
        }
        
        private void ConfigureLineRenderer(LineRenderer lineRenderer)
        {
            // Создаем материал по умолчанию если не назначен
            if (_lineMaterial == null)
            {
                if (_enableDebugLog)
                    Debug.LogWarning("[DrawingService] Line material is null, creating default material...");
                    
                _lineMaterial = new Material(Shader.Find("Sprites/Default"));
                _lineMaterial.name = "DefaultLineMaterial";
                _lineMaterial.color = Color.white;
                
                if (_enableDebugLog)
                    Debug.Log("[DrawingService] Default material created");
            }
            
            lineRenderer.material = _lineMaterial;
            lineRenderer.startWidth = _currentSettings.LineWidth;
            lineRenderer.endWidth = _currentSettings.LineWidth;
            lineRenderer.startColor = _currentSettings.LineColor;
            lineRenderer.endColor = _currentSettings.LineColor;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            
            // Настройки для VR/AR
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.allowOcclusionWhenDynamic = false;
            
            if (_enableDebugLog)
                Debug.Log($"[DrawingService] LineRenderer configured - Width: {_currentSettings.LineWidth}, Color: {_currentSettings.LineColor}");
        }
        
        #endregion
        
        #region Drawing Methods
        
        public void StartLine(Vector3 position)
        {
            if (_enableDebugLog)
                Debug.Log($"[DrawingService] StartLine called at {position}, pool is {(_lineRendererPool == null ? "NULL" : "initialized")}");
            
            // Принудительная инициализация если не инициализирован
            if (_lineRendererPool == null)
            {
                if (_enableDebugLog)
                    Debug.LogWarning("[DrawingService] Pool not initialized, calling InitializeService()...");
                InitializeService();
            }
            
            // Принудительно завершаем предыдущую линию если ещё рисуем
            if (_isDrawing)
            {
                if (_enableDebugLog)
                    Debug.LogWarning("[DrawingService] Force ending previous line before starting new one");
                EndLine();
            }
            
            // Проверка лимита линий
            if (_activeLines.Count >= _currentSettings.MaxLinesCount)
            {
                RemoveOldestLine();
            }
            
            // Создание новой линии
            _currentLine = new DrawingLine(_currentSettings.LineColor, _currentSettings.LineWidth);
            _currentLine.AddPoint(position);
            
            if (_enableDebugLog)
                Debug.Log($"[DrawingService] Created new DrawingLine, getting renderer from pool...");
            
            // Получение рендера из пула
            _currentRenderer = _lineRendererPool.Take();
            
            if (_enableDebugLog)
                Debug.Log($"[DrawingService] Got renderer from pool: {(_currentRenderer != null ? "SUCCESS" : "NULL")}");
            
            _currentRenderer.Activate(_currentLine);
            
            // Добавление в активные списки
            _activeLines.Add(_currentLine);
            _activeRenderers.Add(_currentRenderer);
            
            // Обновление состояния
            _isDrawing = true;
            _isDrawingSubject.OnNext(_isDrawing);
            _activeLinesSubject.OnNext(new List<DrawingLine>(_activeLines));
            
            // Обновление визуализации
            _currentRenderer.UpdateLine();
            
            if (_enableDebugLog)
            {
                Debug.Log($"[DrawingService] Started NEW line at {position}. Active lines: {_activeLines.Count}");
            }
        }
        
        public void AddPointToLine(Vector3 position)
        {
            if (!_isDrawing || _currentLine == null)
            {
                if (_enableDebugLog)
                    Debug.LogWarning("[DrawingService] Attempted to add point while not drawing");
                return;
            }
            
            // Проверка минимального расстояния
            if (_currentLine.GetDistanceFromLastPoint(position) < _currentSettings.MinDistanceBetweenPoints)
            {
                return;
            }
            
            // Проверка лимита точек
            if (_currentLine.PointCount >= _currentSettings.MaxPointsPerLine)
            {
                if (_enableDebugLog)
                    Debug.Log($"[DrawingService] Line reached max points limit: {_currentSettings.MaxPointsPerLine}");
                EndLine();
                return;
            }
            
            // Добавление точки
            _currentLine.AddPoint(position);
            _currentRenderer.UpdateLine();
            
            if (_enableDebugLog && _currentLine.PointCount % 50 == 0)
            {
                Debug.Log($"[DrawingService] Line points: {_currentLine.PointCount}");
            }
        }
        
        public void EndLine()
        {
            if (!_isDrawing || _currentLine == null)
            {
                if (_enableDebugLog)
                    Debug.LogWarning("[DrawingService] Attempted to end line while not drawing");
                return;
            }
            
            if (_enableDebugLog)
                Debug.Log($"[DrawingService] Ending line with {_currentLine.PointCount} points");
            
            // Если линия слишком короткая, удаляем её
            if (_currentLine.PointCount < 2)
            {
                if (_enableDebugLog)
                    Debug.Log("[DrawingService] Removing line with insufficient points");
                RemoveCurrentLine();
            }
            
            // ОБЯЗАТЕЛЬНО очищаем текущие ссылки
            _currentLine = null;
            _currentRenderer = null;
            
            // Обновление состояния
            _isDrawing = false;
            _isDrawingSubject.OnNext(_isDrawing);
            
            if (_enableDebugLog)
            {
                Debug.Log($"[DrawingService] Line ended successfully. Active lines: {_activeLines.Count}");
            }
        }
        
        public void ClearAllLines()
        {
            // Завершаем текущее рисование
            if (_isDrawing)
            {
                EndLine();
            }
            
            // Возвращаем все рендеры в пул
            foreach (var renderer in _activeRenderers)
            {
                _lineRendererPool.Return(renderer);
            }
            
            // Очищаем списки
            _activeLines.Clear();
            _activeRenderers.Clear();
            
            // Обновляем состояние
            _activeLinesSubject.OnNext(new List<DrawingLine>(_activeLines));
            
            if (_enableDebugLog)
            {
                Debug.Log("[DrawingService] All lines cleared");
            }
        }
        
        public bool UndoLastLine()
        {
            // Нельзя отменять во время рисования
            if (_isDrawing)
            {
                if (_enableDebugLog)
                    Debug.LogWarning("[DrawingService] Cannot undo while drawing. End current line first.");
                return false;
            }
            
            // Проверяем что есть линии для отмены
            if (_activeLines.Count == 0)
            {
                if (_enableDebugLog)
                    Debug.Log("[DrawingService] No lines to undo");
                return false;
            }
            
            // Удаляем последнюю линию
            int lastIndex = _activeLines.Count - 1;
            var removedLine = _activeLines[lastIndex];
            var removedRenderer = _activeRenderers[lastIndex];
            
            // Удаляем из списков
            _activeLines.RemoveAt(lastIndex);
            _activeRenderers.RemoveAt(lastIndex);
            
            // Возвращаем рендер в пул
            _lineRendererPool.Return(removedRenderer);
            
            // Обновляем состояние
            _activeLinesSubject.OnNext(new List<DrawingLine>(_activeLines));
            
            if (_enableDebugLog)
            {
                Debug.Log($"[DrawingService] Undone line with {removedLine.PointCount} points. Remaining lines: {_activeLines.Count}");
            }
            
            return true;
        }
        
        public void SetDrawingSettings(DrawingSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[DrawingService] Attempted to set null drawing settings");
                return;
            }
            
            _currentSettings = settings;
            _drawingSettings = settings;
            
            // Обновляем настройки пула если нужно
            if (_lineRendererPool != null)
            {
                // Пул не поддерживает изменение размера на лету,
                // но мы можем предварительно создать объекты
                _lineRendererPool.Prewarm(_currentSettings.LinePoolInitialSize);
            }
            
            if (_enableDebugLog)
            {
                Debug.Log($"[DrawingService] Settings updated. Color: {settings.LineColor}, Width: {settings.LineWidth}");
            }
        }
        
        #endregion
        
        #region Pool Management
        
        private void OnLineRendererTaken(PooledLineRenderer renderer)
        {
            // Настройка рендера при извлечении из пула
            var lineRenderer = renderer.LineRenderer;
            lineRenderer.material = _lineMaterial;
            lineRenderer.startWidth = _currentSettings.LineWidth;
            lineRenderer.endWidth = _currentSettings.LineWidth;
            lineRenderer.startColor = _currentSettings.LineColor;
            lineRenderer.endColor = _currentSettings.LineColor;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            
            // Настройки для VR/AR
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.allowOcclusionWhenDynamic = false;
        }
        
        private void OnLineRendererReturned(PooledLineRenderer renderer)
        {
            // Деактивация при возврате в пул
            renderer.Deactivate();
        }
        
        private void RemoveOldestLine()
        {
            if (_activeLines.Count == 0) return;
            
            var oldestLine = _activeLines[0];
            var oldestRenderer = _activeRenderers[0];
            
            // Удаляем из списков
            _activeLines.RemoveAt(0);
            _activeRenderers.RemoveAt(0);
            
            // Возвращаем рендер в пул
            _lineRendererPool.Return(oldestRenderer);
            
            if (_enableDebugLog)
            {
                Debug.Log($"[DrawingService] Removed oldest line with {oldestLine.PointCount} points");
            }
        }
        
        private void RemoveCurrentLine()
        {
            if (_currentLine == null || _currentRenderer == null) return;
            
            // Находим и удаляем текущую линию
            int index = _activeLines.IndexOf(_currentLine);
            if (index >= 0)
            {
                _activeLines.RemoveAt(index);
                _activeRenderers.RemoveAt(index);
                
                // Возвращаем рендер в пул
                _lineRendererPool.Return(_currentRenderer);
                
                // Обновляем Observable
                _activeLinesSubject.OnNext(new List<DrawingLine>(_activeLines));
            }
        }
        
        #endregion
        
        #region Debug and Statistics
        
        private void Update()
        {
            if (_showPoolStats && _lineRendererPool != null)
            {
                var stats = PoolStats.FromPool(_lineRendererPool, _currentSettings.LinePoolMaxSize);
                Debug.Log($"Pool Stats - Total: {stats.TotalCreated}, Active: {stats.ActiveObjects}, " +
                         $"Available: {stats.AvailableInPool}, Utilization: {stats.UtilizationPercent:F1}%");
            }
        }
        
        /// <summary>
        /// Получает статистику пула объектов.
        /// Gets object pool statistics.
        /// </summary>
        /// <returns>Статистика пула / Pool statistics</returns>
        public PoolStats GetPoolStatistics()
        {
            return _lineRendererPool != null 
                ? PoolStats.FromPool(_lineRendererPool, _currentSettings.LinePoolMaxSize)
                : default;
        }
        
        /// <summary>
        /// Получает общую информацию о состоянии сервиса.
        /// Gets general information about service state.
        /// </summary>
        /// <returns>Информация о состоянии / State information</returns>
        public string GetServiceInfo()
        {
            var poolStats = GetPoolStatistics();
            return $"Drawing Service Info:\n" +
                   $"- Active Lines: {_activeLines.Count}\n" +
                   $"- Is Drawing: {_isDrawing}\n" +
                   $"- Pool Total: {poolStats.TotalCreated}\n" +
                   $"- Pool Active: {poolStats.ActiveObjects}\n" +
                   $"- Pool Available: {poolStats.AvailableInPool}\n" +
                   $"- Current Settings: Color={_currentSettings.LineColor}, Width={_currentSettings.LineWidth}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void DisposeService()
        {
            try
            {
                // Очистка всех линий
                ClearAllLines();
                
                // Очистка пула с проверкой
                if (_lineRendererPool != null)
                {
                    _lineRendererPool.Clear();
                    _lineRendererPool = null;
                }
                
                // Освобождение Observable с проверкой
                _activeLinesSubject?.Dispose();
                _isDrawingSubject?.Dispose();
                
                // Очистка текущих ссылок
                _currentLine = null;
                _currentRenderer = null;
                
                if (_enableDebugLog)
                {
                    Debug.Log("[DrawingService] Service disposed safely");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DrawingService] Error during disposal: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Public API Extensions
        
        /// <summary>
        /// Получает копию всех активных линий.
        /// Gets a copy of all active lines.
        /// </summary>
        /// <returns>Список копий линий / List of line copies</returns>
        public List<DrawingLine> GetActiveLinesCopy()
        {
            return _activeLines.Select(line => line.Clone()).ToList();
        }
        
        /// <summary>
        /// Загружает линии из внешнего источника.
        /// Loads lines from external source.
        /// </summary>
        /// <param name="lines">Линии для загрузки / Lines to load</param>
        public void LoadLines(List<DrawingLine> lines)
        {
            if (lines == null) return;
            
            // Очищаем текущие линии
            ClearAllLines();
            
            // Загружаем новые линии
            foreach (var line in lines)
            {
                if (line.PointCount < 2) continue;
                
                var newLine = line.Clone();
                var renderer = _lineRendererPool.Take();
                renderer.Activate(newLine);
                renderer.UpdateLine();
                
                _activeLines.Add(newLine);
                _activeRenderers.Add(renderer);
            }
            
            // Обновляем Observable
            _activeLinesSubject.OnNext(new List<DrawingLine>(_activeLines));
            
            if (_enableDebugLog)
            {
                Debug.Log($"[DrawingService] Loaded {lines.Count} lines");
            }
        }
        
        /// <summary>
        /// Удаляет конкретную линию по индексу.
        /// Removes specific line by index.
        /// </summary>
        /// <param name="index">Индекс линии / Line index</param>
        public void RemoveLine(int index)
        {
            if (index < 0 || index >= _activeLines.Count) return;
            
            var renderer = _activeRenderers[index];
            _lineRendererPool.Return(renderer);
            
            _activeLines.RemoveAt(index);
            _activeRenderers.RemoveAt(index);
            
            _activeLinesSubject.OnNext(new List<DrawingLine>(_activeLines));
            
            if (_enableDebugLog)
            {
                Debug.Log($"[DrawingService] Removed line at index {index}");
            }
        }
        
        /// <summary>
        /// Получает текущую линию (если рисуем).
        /// Gets current line (if drawing).
        /// </summary>
        /// <returns>Текущая линия или null / Current line or null</returns>
        public DrawingLine GetCurrentLine()
        {
            return _currentLine?.Clone();
        }
        
        /// <summary>
        /// Проверяет, можно ли начать новую линию.
        /// Checks if a new line can be started.
        /// </summary>
        /// <returns>True если можно начать линию / True if line can be started</returns>
        public bool CanStartNewLine()
        {
            return !_isDrawing && _activeLines.Count < _currentSettings.MaxLinesCount;
        }
        
        /// <summary>
        /// Получает все текущие линии рисования.
        /// Gets all current drawing lines.
        /// </summary>
        /// <returns>Список всех линий / List of all lines</returns>
        public List<DrawingLine> GetAllLines()
        {
            return new List<DrawingLine>(_activeLines);
        }
        
        /// <summary>
        /// Получает количество текущих линий.
        /// Gets count of current lines.
        /// </summary>
        /// <returns>Количество линий / Line count</returns>
        public int GetLineCount()
        {
            return _activeLines.Count;
        }
        
        #endregion
    }
}