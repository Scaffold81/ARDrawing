using UnityEngine;
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
        private readonly ReactiveProperty<bool> isDrawingActive = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<Vector3> currentDrawingPosition = new ReactiveProperty<Vector3>(Vector3.zero);
        private readonly ReactiveProperty<float> drawingConfidence = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<TouchState> currentTouchState = new ReactiveProperty<TouchState>(TouchState.None);
        
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
        
        #region Reactive Streams Initialization
        
        private void InitializeReactiveStreams()
        {
            if (enableDebugLog)
                Debug.Log("[DrawingPresenter] Initializing reactive streams...");
            
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
            if (enableDebugLog)
                Debug.Log("[DrawingPresenter] Setting up touch state stream...");
            
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
                var newState = isTouching ? TouchState.Active : TouchState.None;
                
                if (enableDebugLog)
                    Debug.Log($"[DrawingPresenter] Touch state changed: {newState}");
                
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
            if (enableDebugLog)
                Debug.Log("[DrawingPresenter] Setting up finger position stream...");
            
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
            if (enableDebugLog)
                Debug.Log("[DrawingPresenter] Setting up confidence stream...");
            
            confidenceSubscription = handTrackingService.HandTrackingConfidence
                .DistinctUntilChanged()
                .Do(confidence => drawingConfidence.Value = confidence)
                .Where(confidence => confidence < touchConfidenceThreshold)
                .Subscribe(OnLowConfidence);
        }
        
        private void OnLowConfidence(float confidence)
        {
            if (isCurrentlyDrawing && enableDebugLog)
            {
                Debug.LogWarning($"[DrawingPresenter] Low tracking confidence: {confidence:F2}");
            }
            
            // При очень низкой уверенности можно остановить рисование
            if (confidence < 0.3f && isCurrentlyDrawing)
            {
                if (enableDebugLog)
                    Debug.LogWarning("[DrawingPresenter] Stopping drawing due to very low confidence");
                EndDrawing();
            }
        }
        
        #endregion
        
        #region Drawing Logic
        
        private void SetupDrawingLogic()
        {
            if (enableDebugLog)
                Debug.Log("[DrawingPresenter] Setting up drawing logic stream...");
            
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
            
            if (enableDebugLog && confidence < 0.8f)
            {
                Debug.Log($"[DrawingPresenter] Drawing with reduced confidence: {confidence:F2}");
            }
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
            
            Vector3 startPosition = currentDrawingPosition.Value;
            
            if (enableDebugLog)
                Debug.Log($"[DrawingPresenter] Starting drawing at {startPosition}");
            
            drawingService.StartLine(startPosition);
            isCurrentlyDrawing = true;
            isDrawingActive.Value = true;
            lastDrawingPosition = startPosition;
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
            
            if (enableDebugLog && totalPointsDrawn % 20 == 0)
            {
                Debug.Log($"[DrawingPresenter] Added {totalPointsDrawn} points to current line");
            }
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
            if (enableDebugLog)
                Debug.Log("[DrawingPresenter] Disposing reactive subscriptions...");
            
            touchStateSubscription?.Dispose();
            fingerPositionSubscription?.Dispose();
            drawingLogicSubscription?.Dispose();
            confidenceSubscription?.Dispose();
            
            isDrawingActive?.Dispose();
            currentDrawingPosition?.Dispose();
            drawingConfidence?.Dispose();
            currentTouchState?.Dispose();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Получает текущее состояние рисования как Observable.
        /// Gets current drawing state as Observable.
        /// </summary>
        public Observable<bool> IsDrawingActive => isDrawingActive.AsObservable();
        
        /// <summary>
        /// Получает текущую позицию рисования как Observable.
        /// Gets current drawing position as Observable.
        /// </summary>
        public Observable<Vector3> CurrentDrawingPosition => currentDrawingPosition.AsObservable();
        
        /// <summary>
        /// Получает уверенность отслеживания как Observable.
        /// Gets tracking confidence as Observable.
        /// </summary>
        public Observable<float> DrawingConfidence => drawingConfidence.AsObservable();
        
        /// <summary>
        /// Принудительно останавливает текущее рисование.
        /// Forcefully stops current drawing.
        /// </summary>
        public void ForceStopDrawing()
        {
            if (isCurrentlyDrawing)
            {
                if (enableDebugLog)
                    Debug.Log("[DrawingPresenter] Force stopping drawing");
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
            drawingThrottleMs = newThrottleMs;
            minMovementDistance = newMinDistance;
            touchConfidenceThreshold = newConfidenceThreshold;
            
            if (enableDebugLog)
            {
                Debug.Log($"[DrawingPresenter] Updated filtering settings - Throttle: {newThrottleMs}ms, " +
                         $"MinDistance: {newMinDistance}, Confidence: {newConfidenceThreshold}");
            }
            
            // Пересоздаем стримы с новыми настройками
            DisposeSubscriptions();
            
            // Проверяем что зависимости все еще доступны
            if (ValidateDependencies())
            {
                // Повторно создаем только position стрим, который использует новые настройки
                SetupTouchStateStream();
                SetupFingerPositionStream();
                SetupConfidenceStream();
                SetupDrawingLogic();
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