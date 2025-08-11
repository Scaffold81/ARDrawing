using UnityEngine;
using ARDrawing.Presentation.Presenters;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using R3;
using System;
using Zenject;

namespace ARDrawing.Testing
{
    /// <summary>
    /// Тестовый компонент для проверки функциональности DrawingPresenter.
    /// Test component for DrawingPresenter functionality verification.
    /// </summary>
    public class DrawingPresenterTester : MonoBehaviour
    {
        [Header("Testing Configuration")]
        [SerializeField] private bool enableAutomaticTesting = false;
        [SerializeField] private float testInterval = 3.0f;
        [SerializeField] private bool showReactiveStreams = true;
        
        [Header("Manual Test Controls")]
        [SerializeField] private KeyCode forceStopKey = KeyCode.S;
        [SerializeField] private KeyCode showStatsKey = KeyCode.P;
        [SerializeField] private KeyCode updateSettingsKey = KeyCode.U;
        
        [Header("Filter Test Settings")]
        [SerializeField] private float testThrottleMs = 32f;
        [SerializeField] private float testMinDistance = 0.008f;
        [SerializeField] private float testConfidenceThreshold = 0.6f;
        
        // Injected Dependencies
        [Inject] private DrawingPresenter drawingPresenter;
        [Inject] private IHandTrackingService handTrackingService;
        
        // Subscriptions to monitor reactive streams
        private IDisposable isDrawingSubscription;
        private IDisposable positionSubscription;
        private IDisposable confidenceSubscription;
        
        // Test State
        private bool isMonitoringStreams = false;
        private int drawingStateChanges = 0;
        private Vector3 lastPosition = Vector3.zero;
        private float lastConfidence = 0f;
        private DateTime lastTestTime = DateTime.Now;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeTesting();
        }
        
        private void Update()
        {
            HandleManualInput();
            HandleAutomaticTesting();
        }
        
        private void OnDestroy()
        {
            CleanupSubscriptions();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeTesting()
        {
            if (drawingPresenter == null)
            {
                Debug.LogError("[DrawingPresenterTester] DrawingPresenter not injected!");
                return;
            }
            
            if (handTrackingService == null)
            {
                Debug.LogError("[DrawingPresenterTester] HandTrackingService not injected!");
                return;
            }
            
            if (showReactiveStreams)
            {
                SetupStreamMonitoring();
            }
            
            Debug.Log("[DrawingPresenterTester] Initialized successfully");
            ShowTestInstructions();
        }
        
        private void ShowTestInstructions()
        {
            Debug.Log($"[DrawingPresenterTester] Test controls:\n" +
                     $"- {forceStopKey}: Force stop drawing\n" +
                     $"- {showStatsKey}: Show performance stats\n" +
                     $"- {updateSettingsKey}: Update filter settings\n" +
                     $"- Automatic testing: {enableAutomaticTesting}");
        }
        
        #endregion
        
        #region Stream Monitoring
        
        private void SetupStreamMonitoring()
        {
            Debug.Log("[DrawingPresenterTester] Setting up reactive stream monitoring...");
            
            // Мониторинг состояния рисования
            isDrawingSubscription = drawingPresenter.IsDrawingActive
                .Subscribe(isDrawing => OnDrawingStateChanged(isDrawing));
            
            // Мониторинг позиции рисования
            positionSubscription = drawingPresenter.CurrentDrawingPosition
                .Where(pos => pos != Vector3.zero)
                .Subscribe(position => OnDrawingPositionChanged(position));
            
            // Мониторинг уверенности отслеживания
            confidenceSubscription = drawingPresenter.DrawingConfidence
                .Subscribe(confidence => OnConfidenceChanged(confidence));
            
            isMonitoringStreams = true;
            Debug.Log("[DrawingPresenterTester] Stream monitoring active");
        }
        
        private void OnDrawingStateChanged(bool isDrawing)
        {
            drawingStateChanges++;
            Debug.Log($"[DrawingPresenterTester] Drawing state changed: {isDrawing} (Total changes: {drawingStateChanges})");
        }
        
        private void OnDrawingPositionChanged(Vector3 position)
        {
            if (Vector3.Distance(position, lastPosition) > 0.01f) // Логируем только значительные изменения
            {
                Debug.Log($"[DrawingPresenterTester] Drawing position: {position:F3}");
                lastPosition = position;
            }
        }
        
        private void OnConfidenceChanged(float confidence)
        {
            if (Mathf.Abs(confidence - lastConfidence) > 0.1f) // Логируем только значительные изменения
            {
                Debug.Log($"[DrawingPresenterTester] Tracking confidence: {confidence:F2}");
                lastConfidence = confidence;
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleManualInput()
        {
            // Принудительная остановка рисования
            if (Input.GetKeyDown(forceStopKey))
            {
                TestForceStopDrawing();
            }
            
            // Показать статистику производительности
            if (Input.GetKeyDown(showStatsKey))
            {
                ShowPerformanceStats();
            }
            
            // Обновить настройки фильтрации
            if (Input.GetKeyDown(updateSettingsKey))
            {
                TestUpdateFilterSettings();
            }
        }
        
        private void HandleAutomaticTesting()
        {
            if (!enableAutomaticTesting)
                return;
            
            if (DateTime.Now.Subtract(lastTestTime).TotalSeconds >= testInterval)
            {
                PerformAutomaticTest();
                lastTestTime = DateTime.Now;
            }
        }
        
        #endregion
        
        #region Test Methods
        
        private void TestForceStopDrawing()
        {
            Debug.Log("[DrawingPresenterTester] Testing force stop drawing...");
            drawingPresenter.ForceStopDrawing();
            Debug.Log("[DrawingPresenterTester] Force stop completed");
        }
        
        private void ShowPerformanceStats()
        {
            Debug.Log("[DrawingPresenterTester] Performance Statistics:");
            Debug.Log(drawingPresenter.GetPerformanceStats());
        }
        
        private void TestUpdateFilterSettings()
        {
            Debug.Log("[DrawingPresenterTester] Testing filter settings update...");
            
            drawingPresenter.UpdateFilteringSettings(
                testThrottleMs,
                testMinDistance,
                testConfidenceThreshold
            );
            
            Debug.Log($"[DrawingPresenterTester] Filter settings updated - " +
                     $"Throttle: {testThrottleMs}ms, MinDistance: {testMinDistance}, Confidence: {testConfidenceThreshold}");
        }
        
        private void PerformAutomaticTest()
        {
            Debug.Log("[DrawingPresenterTester] Performing automatic test...");
            
            // Тест 1: Проверка состояния
            TestReactiveStreams();
            
            // Тест 2: Статистика
            ShowPerformanceStats();
            
            // Тест 3: Случайное обновление настроек
            if (UnityEngine.Random.value < 0.3f)
            {
                TestRandomFilterSettings();
            }
        }
        
        private void TestReactiveStreams()
        {
            Debug.Log($"[DrawingPresenterTester] Reactive Streams Test:");
            Debug.Log($"- Stream monitoring active: {isMonitoringStreams}");
            Debug.Log($"- Drawing state changes: {drawingStateChanges}");
            Debug.Log($"- Last position: {lastPosition}");
            Debug.Log($"- Last confidence: {lastConfidence:F2}");
        }
        
        private void TestRandomFilterSettings()
        {
            var randomThrottle = UnityEngine.Random.Range(16f, 50f);
            var randomDistance = UnityEngine.Random.Range(0.005f, 0.02f);
            var randomConfidence = UnityEngine.Random.Range(0.5f, 0.9f);
            
            Debug.Log($"[DrawingPresenterTester] Applying random filter settings...");
            
            drawingPresenter.UpdateFilteringSettings(
                randomThrottle,
                randomDistance,
                randomConfidence
            );
        }
        
        #endregion
        
        #region Full Integration Test
        
        [ContextMenu("Run Full Integration Test")]
        public void RunFullIntegrationTest()
        {
            Debug.Log("[DrawingPresenterTester] ===== FULL INTEGRATION TEST START =====");
            
            // Тест 1: Проверка зависимостей
            TestDependencies();
            
            // Тест 2: Проверка реактивных потоков
            TestReactiveStreams();
            
            // Тест 3: Проверка функциональности
            TestFunctionality();
            
            // Тест 4: Проверка производительности
            TestPerformance();
            
            Debug.Log("[DrawingPresenterTester] ===== FULL INTEGRATION TEST END =====");
        }
        
        private void TestDependencies()
        {
            Debug.Log("[DrawingPresenterTester] Testing dependencies...");
            
            var presenterExists = drawingPresenter != null;
            var handTrackingExists = handTrackingService != null;
            
            Debug.Log($"- DrawingPresenter: {(presenterExists ? "✅" : "❌")}");
            Debug.Log($"- HandTrackingService: {(handTrackingExists ? "✅" : "❌")}");
            
            if (presenterExists && handTrackingExists)
            {
                Debug.Log("[DrawingPresenterTester] ✅ All dependencies resolved");
            }
            else
            {
                Debug.LogError("[DrawingPresenterTester] ❌ Dependency injection failed");
            }
        }
        
        private void TestFunctionality()
        {
            Debug.Log("[DrawingPresenterTester] Testing functionality...");
            
            // Тест force stop
            TestForceStopDrawing();
            
            // Тест обновления настроек
            TestUpdateFilterSettings();
            
            Debug.Log("[DrawingPresenterTester] ✅ Functionality test completed");
        }
        
        private void TestPerformance()
        {
            Debug.Log("[DrawingPresenterTester] Testing performance...");
            
            ShowPerformanceStats();
            
            Debug.Log("[DrawingPresenterTester] ✅ Performance test completed");
        }
        
        #endregion
        
        #region Public API for External Testing
        
        /// <summary>
        /// Получает информацию о текущем состоянии тестирования.
        /// Gets current testing state information.
        /// </summary>
        /// <returns>Информация о состоянии / State information</returns>
        public string GetTestingInfo()
        {
            return $"DrawingPresenter Tester Info:\n" +
                   $"- Stream monitoring: {isMonitoringStreams}\n" +
                   $"- Drawing state changes: {drawingStateChanges}\n" +
                   $"- Last position: {lastPosition}\n" +
                   $"- Last confidence: {lastConfidence:F2}\n" +
                   $"- Automatic testing: {enableAutomaticTesting}\n" +
                   $"- Test interval: {testInterval}s";
        }
        
        /// <summary>
        /// Включает/выключает мониторинг реактивных потоков.
        /// Enables/disables reactive stream monitoring.
        /// </summary>
        /// <param name="enable">Включить мониторинг / Enable monitoring</param>
        public void SetStreamMonitoring(bool enable)
        {
            if (enable && !isMonitoringStreams)
            {
                SetupStreamMonitoring();
            }
            else if (!enable && isMonitoringStreams)
            {
                CleanupSubscriptions();
                isMonitoringStreams = false;
                Debug.Log("[DrawingPresenterTester] Stream monitoring disabled");
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupSubscriptions()
        {
            isDrawingSubscription?.Dispose();
            positionSubscription?.Dispose();
            confidenceSubscription?.Dispose();
            
            if (isMonitoringStreams)
            {
                Debug.Log("[DrawingPresenterTester] Cleaned up reactive subscriptions");
            }
            
            isMonitoringStreams = false;
        }
        
        #endregion
        
        #region Debug GUI
        
        private void OnGUI()
        {
            if (!showReactiveStreams)
                return;
            
            var rect = new Rect(420, 200, 350, 180);
            GUI.Box(rect, "DrawingPresenter Tester");
            
            var y = 225;
            GUI.Label(new Rect(430, y, 330, 20), $"Stream Monitoring: {(isMonitoringStreams ? "ON" : "OFF")}");
            y += 25;
            GUI.Label(new Rect(430, y, 330, 20), $"State Changes: {drawingStateChanges}");
            y += 25;
            GUI.Label(new Rect(430, y, 330, 20), $"Last Position: {lastPosition:F2}");
            y += 25;
            GUI.Label(new Rect(430, y, 330, 20), $"Last Confidence: {lastConfidence:F2}");
            y += 25;
            GUI.Label(new Rect(430, y, 330, 20), $"Auto Testing: {(enableAutomaticTesting ? "ON" : "OFF")}");
            y += 25;
            GUI.Label(new Rect(430, y, 330, 20), $"Controls: S-Stop | P-Stats | U-Update");
        }
        
        #endregion
    }
}