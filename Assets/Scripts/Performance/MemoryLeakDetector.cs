using UnityEngine;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARDrawing.Performance
{
    /// <summary>
    /// Детектор утечек памяти для AR Drawing приложения.
    /// Memory leak detector for AR Drawing application.
    /// </summary>
    public class MemoryLeakDetector : MonoBehaviour
    {
        [Header("Monitoring Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float checkIntervalSeconds = 5f;
        [SerializeField] private bool showDebugOutput = true;
        [SerializeField] private bool autoDetectLeaks = true;
        
        [Header("Thresholds")]
        [SerializeField] private int maxGameObjectCount = 1000;
        [SerializeField] private int maxObservableCount = 50;
        [SerializeField] private float memoryGrowthThresholdMB = 10f;
        
        // Monitoring State
        private IDisposable _monitoringSubscription;
        private List<MemorySnapshot> _memoryHistory = new List<MemorySnapshot>();
        private Dictionary<Type, int> _objectTypeCounts = new Dictionary<Type, int>();
        private StringBuilder _reportBuilder = new StringBuilder();
        
        // Leak Detection
        private int _consecutiveLeakChecks = 0;
        private const int LEAK_CONFIRMATION_CHECKS = 3;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeMonitoring();
        }
        
        private void OnDestroy()
        {
            CleanupMonitoring();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeMonitoring()
        {
            if (!enableMonitoring) return;
            
            _monitoringSubscription = Observable.Interval(TimeSpan.FromSeconds(checkIntervalSeconds))
                .Subscribe(_ => PerformMemoryCheck());
        }
        
        private void CleanupMonitoring()
        {
            _monitoringSubscription?.Dispose();
        }
        
        #endregion
        
        #region Memory Monitoring
        
        private void PerformMemoryCheck()
        {
            try
            {
                var snapshot = CreateMemorySnapshot();
                _memoryHistory.Add(snapshot);
                
                // Keep only last 20 snapshots
                if (_memoryHistory.Count > 20)
                {
                    _memoryHistory.RemoveAt(0);
                }
                
                AnalyzeMemoryTrends(snapshot);
                CheckForObjectLeaks();
                CheckForObservableLeaks();
                
                if (autoDetectLeaks)
                {
                    DetectPotentialLeaks();
                }
                
                if (showDebugOutput)
                {
                    LogMemoryStats(snapshot);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MemoryLeakDetector] Error during memory check: {ex.Message}");
            }
        }
        
        private MemorySnapshot CreateMemorySnapshot()
        {
            // Force garbage collection for accurate measurements
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            return new MemorySnapshot
            {
                Timestamp = DateTime.Now,
                HeapSizeMB = GC.GetTotalMemory(false) / (1024f * 1024f),
                UnityHeapSizeMB = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / (1024f * 1024f),
                UnityUsedHeapMB = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024f * 1024f),
                GraphicsMemoryMB = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024f * 1024f),
                TotalGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length,
                TotalLineRenderers = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Length,
                TotalRigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length,
                TotalColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None).Length,
                TotalCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length,
                ActiveObservableStreams = CountActiveObservables()
            };
        }
        
        private int CountActiveObservables()
        {
            // Approximate count based on R3 internal state
            var subjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .SelectMany(mb => mb.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                .Where(field => field.FieldType.Name.Contains("Subject") || field.FieldType.Name.Contains("Observable"))
                .Count();
            
            return subjects;
        }
        
        private void LogMemoryStats(MemorySnapshot snapshot)
        {
            // Memory stats logged
        }
        
        #endregion
        
        #region Leak Detection
        
        private void AnalyzeMemoryTrends(MemorySnapshot current)
        {
            if (_memoryHistory.Count < 5) return;
            
            var recentSnapshots = _memoryHistory.TakeLast(5).ToList();
            float memoryGrowth = current.HeapSizeMB - recentSnapshots.First().HeapSizeMB;
            
            if (memoryGrowth > memoryGrowthThresholdMB)
            {
                Debug.LogWarning($"[MemoryLeakDetector] Memory growth: +{memoryGrowth:F2}MB");
                _consecutiveLeakChecks++;
            }
            else
            {
                _consecutiveLeakChecks = 0;
            }
        }
        
        private void CheckForObjectLeaks()
        {
            if (_memoryHistory.Count == 0) return;
            var current = _memoryHistory[_memoryHistory.Count - 1];
            
            if (current.TotalGameObjects > maxGameObjectCount)
            {
                Debug.LogError($"[MemoryLeakDetector] OBJECT LEAK: {current.TotalGameObjects} GameObjects");
                LogDetailedObjectBreakdown();
            }
            
            if (current.TotalLineRenderers > 100)
            {
                Debug.LogWarning($"[MemoryLeakDetector] High LineRenderer count: {current.TotalLineRenderers}");
            }
        }
        
        private void CheckForObservableLeaks()
        {
            if (_memoryHistory.Count == 0) return;
            var current = _memoryHistory[_memoryHistory.Count - 1];
            
            if (current.ActiveObservableStreams > maxObservableCount)
            {
                Debug.LogError($"[MemoryLeakDetector] OBSERVABLE LEAK: {current.ActiveObservableStreams} streams");
            }
        }
        
        private void DetectPotentialLeaks()
        {
            if (_consecutiveLeakChecks >= LEAK_CONFIRMATION_CHECKS)
            {
                Debug.LogError("[MemoryLeakDetector] MEMORY LEAK CONFIRMED!");
                GenerateDetailedLeakReport();
                _consecutiveLeakChecks = 0;
            }
        }
        
        #endregion
        
        #region Detailed Analysis
        
        private void LogDetailedObjectBreakdown()
        {
            _objectTypeCounts.Clear();
            
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                var type = obj.GetType();
                if (_objectTypeCounts.ContainsKey(type))
                    _objectTypeCounts[type]++;
                else
                    _objectTypeCounts[type] = 1;
            }
            
            Debug.Log("[MemoryLeakDetector] Object Breakdown:");
            foreach (var kvp in _objectTypeCounts.OrderByDescending(x => x.Value).Take(10))
            {
                Debug.Log($"  {kvp.Key.Name}: {kvp.Value}");
            }
        }
        
        private void GenerateDetailedLeakReport()
        {
            _reportBuilder.Clear();
            _reportBuilder.AppendLine("=== MEMORY LEAK REPORT ===");
            _reportBuilder.AppendLine($"Time: {DateTime.Now}");
            
            if (_memoryHistory.Count >= 2)
            {
                var first = _memoryHistory[0];
                var last = _memoryHistory[_memoryHistory.Count - 1];
                
                _reportBuilder.AppendLine($"Heap: {first.HeapSizeMB:F2}MB -> {last.HeapSizeMB:F2}MB");
                _reportBuilder.AppendLine($"Objects: {first.TotalGameObjects} -> {last.TotalGameObjects}");
            }
            
            Debug.LogError(_reportBuilder.ToString());
        }
        
        #endregion
        
        #region Public API
        
        public void ForceMemoryCheck()
        {
            PerformMemoryCheck();
        }
        
        public MemorySnapshot GetCurrentSnapshot()
        {
            return CreateMemorySnapshot();
        }
        
        public string GetDetailedReport()
        {
            var current = CreateMemorySnapshot();
            
            return $"Memory Report:\\n" +
                   $"Heap: {current.HeapSizeMB:F2} MB\\n" +
                   $"GameObjects: {current.TotalGameObjects}\\n" +
                   $"LineRenderers: {current.TotalLineRenderers}\\n" +
                   $"Observables: {current.ActiveObservableStreams}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Снимок состояния памяти.
    /// Memory state snapshot.
    /// </summary>
    [Serializable]
    public struct MemorySnapshot
    {
        public DateTime Timestamp;
        public float HeapSizeMB;
        public float UnityHeapSizeMB;
        public float UnityUsedHeapMB;
        public float GraphicsMemoryMB;
        public int TotalGameObjects;
        public int TotalLineRenderers;
        public int TotalRigidbodies;
        public int TotalColliders;
        public int TotalCanvases;
        public int ActiveObservableStreams;
    }
}
