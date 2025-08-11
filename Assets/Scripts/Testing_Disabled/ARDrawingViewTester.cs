using UnityEngine;
using ARDrawing.Presentation.Views;
using Zenject;

namespace ARDrawing.Testing
{
    /// <summary>
    /// Тестер для ARDrawingView - проверка enhanced визуализации.
    /// Tester for ARDrawingView - testing enhanced visualization.
    /// </summary>
    public class ARDrawingViewTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool enableAutomaticTesting = false;
        [SerializeField] private float testInterval = 3.0f;
        [SerializeField] private bool showPerformanceStats = true;
        
        [Header("Visual Test Controls")]
        [SerializeField] private KeyCode toggleEffectsKey = KeyCode.E;
        [SerializeField] private KeyCode showStatsKey = KeyCode.R;
        [SerializeField] private KeyCode toggleLODKey = KeyCode.L;
        
        // Dependencies
        [Inject] private ARDrawingView arDrawingView;
        
        // Test State
        private float lastTestTime;
        private bool effectsEnabled = true;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeTester();
        }
        
        private void Update()
        {
            HandleInput();
            HandleAutomaticTesting();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeTester()
        {
            if (arDrawingView == null)
            {
                Debug.LogError("[ARDrawingViewTester] ARDrawingView not injected!");
                return;
            }
            
            Debug.Log("[ARDrawingViewTester] Initialized successfully");
            ShowTestInstructions();
        }
        
        private void ShowTestInstructions()
        {
            Debug.Log($"[ARDrawingViewTester] Test controls:\n" +
                     $"- {toggleEffectsKey}: Toggle enhanced effects\n" +
                     $"- {showStatsKey}: Show performance stats\n" +
                     $"- {toggleLODKey}: Toggle LOD system\n" +
                     $"- Automatic testing: {enableAutomaticTesting}");
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            // Toggle enhanced effects
            if (Input.GetKeyDown(toggleEffectsKey))
            {
                TestToggleEffects();
            }
            
            // Show performance stats
            if (Input.GetKeyDown(showStatsKey))
            {
                ShowPerformanceStats();
            }
            
            // Note: LOD toggle would require adding that functionality to ARDrawingViewSimple
            if (Input.GetKeyDown(toggleLODKey))
            {
                Debug.Log("[ARDrawingViewTester] LOD toggle test (not implemented in simple version)");
            }
        }
        
        private void HandleAutomaticTesting()
        {
            if (!enableAutomaticTesting)
                return;
            
            if (Time.time - lastTestTime >= testInterval)
            {
                PerformAutomaticTest();
                lastTestTime = Time.time;
            }
        }
        
        #endregion
        
        #region Test Methods
        
        private void TestToggleEffects()
        {
            effectsEnabled = !effectsEnabled;
            arDrawingView.SetEnhancedEffects(effectsEnabled);
            
            Debug.Log($"[ARDrawingViewTester] Enhanced effects {(effectsEnabled ? "enabled" : "disabled")}");
        }
        
        private void ShowPerformanceStats()
        {
            if (arDrawingView != null)
            {
                Debug.Log($"[ARDrawingViewTester] Performance Stats:");
                Debug.Log(arDrawingView.GetPerformanceStats());
            }
        }
        
        private void PerformAutomaticTest()
        {
            Debug.Log("[ARDrawingViewTester] Performing automatic test...");
            
            // Test 1: Show stats
            if (showPerformanceStats)
            {
                ShowPerformanceStats();
            }
            
            // Test 2: Random effect toggle
            if (Random.value < 0.3f)
            {
                TestToggleEffects();
            }
        }
        
        #endregion
        
        #region Full Integration Test
        
        [ContextMenu("Run Full ARDrawingView Test")]
        public void RunFullTest()
        {
            Debug.Log("[ARDrawingViewTester] ===== FULL AR DRAWING VIEW TEST START =====");
            
            // Test 1: Check dependencies
            TestDependencies();
            
            // Test 2: Test functionality
            TestFunctionality();
            
            // Test 3: Performance check
            TestPerformance();
            
            Debug.Log("[ARDrawingViewTester] ===== FULL AR DRAWING VIEW TEST END =====");
        }
        
        private void TestDependencies()
        {
            Debug.Log("[ARDrawingViewTester] Testing dependencies...");
            
            var viewExists = arDrawingView != null;
            Debug.Log($"- ARDrawingView: {(viewExists ? "✅" : "❌")}");
            
            if (viewExists)
            {
                Debug.Log("[ARDrawingViewTester] ✅ Dependencies resolved");
            }
            else
            {
                Debug.LogError("[ARDrawingViewTester] ❌ Dependency injection failed");
            }
        }
        
        private void TestFunctionality()
        {
            Debug.Log("[ARDrawingViewTester] Testing functionality...");
            
            if (arDrawingView == null) return;
            
            // Test effects toggle
            arDrawingView.SetEnhancedEffects(false);
            arDrawingView.SetEnhancedEffects(true);
            
            Debug.Log("[ARDrawingViewTester] ✅ Functionality test completed");
        }
        
        private void TestPerformance()
        {
            Debug.Log("[ARDrawingViewTester] Testing performance...");
            
            ShowPerformanceStats();
            
            Debug.Log("[ARDrawingViewTester] ✅ Performance test completed");
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Получает информацию о состоянии тестирования.
        /// Gets testing state information.
        /// </summary>
        public string GetTestingInfo()
        {
            return $"ARDrawingView Tester Info:\n" +
                   $"- Enhanced Effects: {effectsEnabled}\n" +
                   $"- Automatic Testing: {enableAutomaticTesting}\n" +
                   $"- Test Interval: {testInterval}s\n" +
                   $"- Performance Stats: {showPerformanceStats}";
        }
        
        #endregion
        
        #region Debug GUI
        
        private void OnGUI()
        {
            if (!showPerformanceStats)
                return;
            
            var rect = new Rect(800, 200, 350, 150);
            GUI.Box(rect, "ARDrawingView Tester");
            
            var y = 225;
            GUI.Label(new Rect(810, y, 330, 20), $"Enhanced Effects: {(effectsEnabled ? "ON" : "OFF")}");
            y += 25;
            GUI.Label(new Rect(810, y, 330, 20), $"Auto Testing: {(enableAutomaticTesting ? "ON" : "OFF")}");
            y += 25;
            GUI.Label(new Rect(810, y, 330, 20), $"Controls: E-Effects | R-Stats | L-LOD");
            y += 25;
            
            if (arDrawingView != null)
            {
                var stats = arDrawingView.GetPerformanceStats();
                var lines = stats.Split('\n');
                
                foreach (var line in lines)
                {
                    if (y > rect.y + rect.height - 30) break;
                    GUI.Label(new Rect(810, y, 330, 15), line);
                    y += 20;
                }
            }
        }
        
        #endregion
    }
}