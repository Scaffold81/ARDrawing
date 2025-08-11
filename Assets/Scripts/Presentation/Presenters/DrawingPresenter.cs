using UnityEngine;
using UnityEngine.EventSystems;
using R3;
using System;
using System.Linq;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using Zenject;

namespace ARDrawing.Presentation.Presenters
{
    /// <summary>
    /// Презентер для управления процессом рисования через реактивные потоки.
    /// Presenter for managing drawing process through reactive streams.
    /// </summary>
    public class DrawingPresenter : MonoBehaviour
    {
        [Header("Drawing Configuration")]
        [SerializeField] private bool enableAdvancedFiltering = true;
        [SerializeField] private float drawingThrottleMs = 16f; // ~60 FPS
        [SerializeField] private float minMovementDistance = 0.005f;
        [SerializeField] private float touchConfidenceThreshold = 0.7f;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private bool showPerformanceStats = false;
        
        // Injected Dependencies
        [Inject] private IHandTrackingService handTrackingService;
        [Inject] private IDrawingService drawingService;
        
        // Reactive Properties
        private ReactiveProperty<bool> isDrawingActive;
        private ReactiveProperty<Vector3> currentDrawingPosition;
        private ReactiveProperty<float> drawingConfidence;
        private ReactiveProperty<TouchState> currentTouchState;
        
        // Disposal tracking
        private bool _isDisposed = false;
        
        // Observable Streams
        private IDisposable touchStateSubscription;
        private IDisposable fingerPositionSubscription;
        private IDisposable drawingLogicSubscription;
        private IDisposable confidenceSubscription;
        
        // State Management
        private Vector3 lastDrawingPosition = Vector3.zero;
        private bool isCurrentlyDrawing = false;
        private DateTime lastDrawTime = DateTime.Now;
        
        // Performance Tracking
        private int totalPointsDrawn = 0;
        private int filteredPointsCount = 0;
        private float averageDrawingFPS = 0f;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeReactiveStreams();
        }
        
        private void OnDestroy()
        {
            DisposeSubscriptions();
        }
        
        #endregion
        
        #region UI Interaction Check
        
        /// <summary>
        /// Проверяет находится ли мышь/палец над UI элементом.
        /// Checks if mouse/finger is over UI element.
        /// </summary>
        private bool IsPointerOverUI()
        {
            // Для Editor - проверяем мышь
            if (Application.isEditor)
            {
                return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
            
            // Для реального устройства - проверяем касания
            if (Input.touchCount > 0)
            {
                return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
            
            return false;
        }
        
        #endregion
        
        #region Reactive Streams Initialization
        
        private void InitializeReactiveStreams()
        {
            if (_isDisposed) return;
            
            if (enableDebugLog)
                Debug.Log("[DrawingPresenter] Initializing reactive streams...");
            
            // Initialize reactive properties
            InitializeReactiveProperties();
            
            // Проверка зависимостей
            if (!ValidateDependencies())
                return;
            
            // 1. Подписка на состояния касания от HandTrackingService
            SetupTouchStateStream();
            
            // 2. Подписка на позиции пальца с фильтрацией
            SetupFingerPositionStream();
            
            // 3. Подписка на уверенность отслеживания
            SetupConfidenceStream();
            
            // 4. Основная логика рисования
            SetupDrawingLogic();
            
            if (enableDebugLog)
                Debug.Log("[DrawingPresenter] Reactive streams initialized successfully");
        }
        
        private void InitializeReactiveProperties()
        {
            if (_isDisposed) return;
            
            isDrawingActive = new ReactiveProperty<bool>(false);
            currentDrawingPosition = new ReactiveProperty<Vector3>(Vector3.zero);
            drawingConfidence = new ReactiveProperty<float>(0f);
            currentTouchState = new ReactiveProperty<TouchState>(TouchState.None);
        }
        
        private bool ValidateDependencies()
        {
            if (handTrackingService == null)
            {
                Debug.LogError("[DrawingPresenter] HandTrackingService not injected!");
                return false;
            }
            
            if (drawingService == null)
            {
                Debug.LogError("[DrawingPresenter] DrawingService not injected!");
                return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region Touch State Stream
        
        private void SetupTouchStateStream()
        {
            touchStateSubscription = handTrackingService.IsIndexFingerTouching
                .DistinctUntilChanged() // Только при изменении состояния
                .Do(isTouching => 
                {
                    var newState = isTouching ? TouchState.Active : TouchState.None;
                    currentTouchState.Value = newState;
                }) // Обновляем ReactiveProperty
                .Subscribe(OnTouchStateChanged);
        }
        
        private void OnTouchStateChanged(bool isTouching)
        {
            try
            {
                // Проверка что мы не кликаем по UI элементам (для Editor)
                if (IsPointerOverUI())
                {
                    // ОБЯЗАТЕЛЬНО завершить текущую линию при клике по UI
                    if (isCurrentlyDrawing)
                    {
                        EndDrawing();
                    }
                    return;
                }
                
                var newState = isTouching ? TouchState.Active : TouchState.None;
                
                switch (newState)
                {
                    case TouchState.Active:
                        if (!isCurrentlyDrawing)
                            StartDrawing();
                        break;
                        
                    case TouchState.None:
                        if (isCurrentlyDrawing)
                            EndDrawing();
                        break;
                }
            }
            catch (Exception ex)
            {
                OnStreamError(ex);
            }
        }
        
        #endregion
        
        #region Finger Position Stream
        
        private void SetupFingerPositionStream()
        {
            var baseStream = handTrackingService.IndexFingerPosition
                .Where(_ => currentTouchState.Value == TouchState.Active);
            
            if (enableAdvancedFiltering)
            {
                fingerPositionSubscription = baseStream
                    .DistinctUntilChanged() // Избегаем дублирующихся позиций
                    .Where(pos => IsValidDrawingPosition(pos)) // Фильтрация валидных позиций
                    .Do(pos => currentDrawingPosition.Value = pos) // Обновляем ReactiveProperty
                    .Subscribe(OnFingerPositionChanged);
            }
            else
            {
                fingerPositionSubscription = baseStream
                    .Do(pos => currentDrawingPosition.Value = pos)
                    .Subscribe(OnFingerPositionChanged);
            }
        }
        
        private bool IsValidDrawingPosition(Vector3 position)
        {
            // Проверка минимального расстояния от последней позиции
            if (isCurrentlyDrawing && Vector3.Distance(position, lastDrawingPosition) < minMovementDistance)
            {
                filteredPointsCount++;
                return false;
            }
            
            // Проверка что позиция в разумных пределах (не NaN, не Infinity)
            if (!IsValidVector3(position))
            {
                filteredPointsCount++;
                return false;
            }
            
            return true;
        }
        
        private bool IsValidVector3(Vector3 vector)
        {
            return !float.IsNaN(vector.x) && !float.IsNaN(vector.y) && !float.IsNaN(vector.z) &&
                   !float.IsInfinity(vector.x) && !float.IsInfinity(vector.y) && !float.IsInfinity(vector.z);
        }
        
        private void OnFingerPositionChanged(Vector3 position)
        {
            try
            {
                // Проверка UI перед добавлением точки - ОБЯЗАТЕЛЬНО!
                if (IsPointerOverUI())
                {
                    // Принудительно завершить линию если навели на UI
                    if (isCurrentlyDrawing)
                    {
                        EndDrawing();
                    }
                    return;
                }
                
                // Простое throttling через временную проверку
                var currentTime = DateTime.Now;
                var deltaTime = (float)(currentTime - lastDrawTime).TotalMilliseconds;
                
                if (deltaTime < drawingThrottleMs)
                {
                    return; // Пропускаем слишком частые обновления
                }
                
                if (isCurrentlyDrawing)
                {
                    AddPointToCurrentLine(position);
                    lastDrawingPosition = position;
                    
                    // Обновляем статистику производительности
                    UpdatePerformanceStats();
                }
            }
            catch (Exception ex)
            {
                OnStreamError(ex);
            }
        }
        
        #endregion
        
        #region Confidence Stream
        
        private void SetupConfidenceStream()
        {
            confidenceSubscription = handTrackingService.HandTrackingConfidence
                .DistinctUntilChanged()
                .Do(confidence => drawingConfidence.Value = confidence)
                .Where(confidence => confidence < touchConfidenceThreshold)
                .Subscribe(OnLowConfidence);
        }
        
        private void OnLowConfidence(float confidence)
        {
            // При очень низкой уверенности можно остановить рисование
            if (confidence < 0.3f && isCurrentlyDrawing)
            {
                EndDrawing();
            }
        }
        
        #endregion
        
        #region Drawing Logic
        
        private void SetupDrawingLogic()
        {
            // Комбинированный стрим для основной логики рисования
            drawingLogicSubscription = currentTouchState.AsObservable()
                .CombineLatest(currentDrawingPosition.AsObservable(), drawingConfidence.AsObservable(), 
                              (touchState, position, confidence) => new { TouchState = touchState, Position = position, Confidence = confidence })
                .Where(data => data.TouchState != TouchState.None) // Только когда есть касание
                .Where(data => data.Confidence >= touchConfidenceThreshold) // Только при достаточной уверенности
                .Subscribe(data => ProcessDrawingLogic(data.TouchState, data.Position, data.Confidence));
        }
        
        private void ProcessDrawingLogic(TouchState touchState, Vector3 position, float confidence)
        {
            // Дополнительная логика обработки рисования может быть добавлена здесь
            // Например, изменение толщины линии в зависимости от уверенности
            // Или специальные эффекты при определенных условиях
        }
        
        #endregion
        
        #region Drawing Operations
        
        private void StartDrawing()
        {
            if (isCurrentlyDrawing)
            {
                if (enableDebugLog)
                    Debug.LogWarning("[DrawingPresenter] Attempted to start drawing while already drawing");
                return;
            }
            
            // Получаем ТЕКУЩУЮ позицию пальца напрямую с HandTrackingService
            Vector3 actualStartPosition = handTrackingService.GetCurrentIndexFingerPosition();
            
            // Проверка что позиция корректна
            if (!IsValidVector3(actualStartPosition))
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[DrawingPresenter] Invalid start position: {actualStartPosition}, using fallback");
                actualStartPosition = currentDrawingPosition.Value;
            }
            
            if (enableDebugLog)
                Debug.Log($"[DrawingPresenter] Starting NEW drawing at ACTUAL position: {actualStartPosition}");
            
            drawingService.StartLine(actualStartPosition);
            isCurrentlyDrawing = true;
            isDrawingActive.Value = true;
            
            // Обновляем последнюю позицию на актуальную
            lastDrawingPosition = actualStartPosition;
            currentDrawingPosition.Value = actualStartPosition;
            lastDrawTime = DateTime.Now;
            
            // Сброс статистики для новой линии
            totalPointsDrawn = 0;
            filteredPointsCount = 0;
        }
        
        private void AddPointToCurrentLine(Vector3 position)
        {
            if (!isCurrentlyDrawing)
                return;
            
            drawingService.AddPointToLine(position);
            totalPointsDrawn++;
        }
        
        private void EndDrawing()
        {
            if (!isCurrentlyDrawing)
                return;
            
            if (enableDebugLog)
            {
                var duration = (DateTime.Now - lastDrawTime).TotalSeconds;
                Debug.Log($"[DrawingPresenter] Ending drawing. Duration: {duration:F2}s, Points: {totalPointsDrawn}, Filtered: {filteredPointsCount}");
            }
            
            drawingService.EndLine();
            isCurrentlyDrawing = false;
            isDrawingActive.Value = false;
            
            // Очищаем последние позиции чтобы не использовать старые данные
            lastDrawingPosition = Vector3.zero;
        }
        
        #endregion
        
        #region Performance Monitoring
        
        private void UpdatePerformanceStats()
        {
            if (!showPerformanceStats)
                return;
            
            var currentTime = DateTime.Now;
            var deltaTime = (float)(currentTime - lastDrawTime).TotalSeconds;
            
            if (deltaTime > 0)
            {
                var currentFPS = 1f / deltaTime;
                averageDrawingFPS = averageDrawingFPS * 0.9f + currentFPS * 0.1f; // Экспоненциальное сглаживание
            }
            
            lastDrawTime = currentTime;
        }
        
        #endregion
        
        #region Error Handling
        
        private void OnStreamError(Exception error)
        {
            Debug.LogError($"[DrawingPresenter] Reactive stream error: {error.Message}");
            Debug.LogError($"[DrawingPresenter] Stack trace: {error.StackTrace}");
            
            // Останавливаем рисование при ошибке
            if (isCurrentlyDrawing)
            {
                EndDrawing();
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void DisposeSubscriptions()
        {
            if (_isDisposed) return;
            
            // Dispose subscriptions safely
            DisposeSubscription(ref touchStateSubscription, "TouchState");
            DisposeSubscription(ref fingerPositionSubscription, "FingerPosition");
            DisposeSubscription(ref drawingLogicSubscription, "DrawingLogic");
            DisposeSubscription(ref confidenceSubscription, "Confidence");
            
            // Dispose reactive properties safely
            DisposeReactiveProperty(ref isDrawingActive, "IsDrawingActive");
            DisposeReactiveProperty(ref currentDrawingPosition, "CurrentDrawingPosition");
            DisposeReactiveProperty(ref drawingConfidence, "DrawingConfidence");
            DisposeReactiveProperty(ref currentTouchState, "CurrentTouchState");
            
            _isDisposed = true;
        }
        
        private void DisposeSubscription(ref IDisposable subscription, string name)
        {
            if (subscription != null)
            {
                try
                {
                    subscription.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DrawingPresenter] Error disposing {name} subscription: {ex.Message}");
                }
                finally
                {
                    subscription = null;
                }
            }
        }
        
        private void DisposeReactiveProperty<T>(ref ReactiveProperty<T> property, string name)
        {
            if (property != null)
            {
                try
                {
                    if (!property.IsDisposed)
                    {
                        property.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DrawingPresenter] Error disposing {name} property: {ex.Message}");
                }
                finally
                {
                    property = null;
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Получает текущее состояние рисования как Observable.
        /// Gets current drawing state as Observable.
        /// </summary>
        public Observable<bool> IsDrawingActive => isDrawingActive?.AsObservable() ?? Observable.Empty<bool>();
        
        /// <summary>
        /// Получает текущую позицию рисования как Observable.
        /// Gets current drawing position as Observable.
        /// </summary>
        public Observable<Vector3> CurrentDrawingPosition => currentDrawingPosition?.AsObservable() ?? Observable.Empty<Vector3>();
        
        /// <summary>
        /// Получает уверенность отслеживания как Observable.
        /// Gets tracking confidence as Observable.
        /// </summary>
        public Observable<float> DrawingConfidence => drawingConfidence?.AsObservable() ?? Observable.Empty<float>();
        
        /// <summary>
        /// Принудительно останавливает текущее рисование.
        /// Forcefully stops current drawing.
        /// </summary>
        public void ForceStopDrawing()
        {
            if (isCurrentlyDrawing)
            {
                EndDrawing();
            }
        }
        
        /// <summary>
        /// Получает статистику производительности.
        /// Gets performance statistics.
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"Drawing Performance Stats:\\n" +
                   $"- Points Drawn: {totalPointsDrawn}\\n" +
                   $"- Points Filtered: {filteredPointsCount}\\n" +
                   $"- Average FPS: {averageDrawingFPS:F1}\\n" +
                   $"- Currently Drawing: {isCurrentlyDrawing}\\n" +
                   $"- Touch State: {currentTouchState.Value}\\n" +
                   $"- Confidence: {drawingConfidence.Value:F2}";
        }
        
        /// <summary>
        /// Обновляет настройки фильтрации в реальном времени.
        /// Updates filtering settings in real-time.
        /// </summary>
        public void UpdateFilteringSettings(float newThrottleMs, float newMinDistance, float newConfidenceThreshold)
        {
            if (_isDisposed) return;
            
            drawingThrottleMs = newThrottleMs;
            minMovementDistance = newMinDistance;
            touchConfidenceThreshold = newConfidenceThreshold;
            
            if (enableDebugLog)
            {
                Debug.Log($"[DrawingPresenter] Updated filtering settings - Throttle: {newThrottleMs}ms, " +
                         $"MinDistance: {newMinDistance}, Confidence: {newConfidenceThreshold}");
            }
            
            // Безопасно пересоздаем стримы с новыми настройками
            SafeRecreateStreams();
        }
        
        private void SafeRecreateStreams()
        {
            try
            {
                // Dispose old subscriptions only (not reactive properties)
                DisposeSubscription(ref touchStateSubscription, "TouchState");
                DisposeSubscription(ref fingerPositionSubscription, "FingerPosition");
                DisposeSubscription(ref drawingLogicSubscription, "DrawingLogic");
                DisposeSubscription(ref confidenceSubscription, "Confidence");
                
                // Проверяем что зависимости все еще доступны
                if (ValidateDependencies() && !_isDisposed)
                {
                    // Повторно создаем только subscriptions
                    SetupTouchStateStream();
                    SetupFingerPositionStream();
                    SetupConfidenceStream();
                    SetupDrawingLogic();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DrawingPresenter] Error recreating streams: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Debug GUI
        
        private void OnGUI()
        {
            if (!enableDebugLog && !showPerformanceStats)
                return;
            
            var rect = new Rect(10, 200, 400, 200);
            GUI.Box(rect, "Drawing Presenter Stats");
            
            var y = 225;
            GUI.Label(new Rect(20, y, 380, 20), $"Is Drawing: {isCurrentlyDrawing}");
            y += 25;
            GUI.Label(new Rect(20, y, 380, 20), $"Touch State: {currentTouchState.Value}");
            y += 25;
            GUI.Label(new Rect(20, y, 380, 20), $"Position: {currentDrawingPosition.Value}");
            y += 25;
            GUI.Label(new Rect(20, y, 380, 20), $"Confidence: {drawingConfidence.Value:F2}");
            y += 25;
            GUI.Label(new Rect(20, y, 380, 20), $"Points: {totalPointsDrawn} | Filtered: {filteredPointsCount}");
            y += 25;
            GUI.Label(new Rect(20, y, 380, 20), $"Drawing FPS: {averageDrawingFPS:F1}");
        }
        
        #endregion
    }
}