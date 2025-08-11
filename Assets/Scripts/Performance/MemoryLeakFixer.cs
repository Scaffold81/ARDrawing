using UnityEngine;
using System;

namespace ARDrawing.Performance
{
    /// <summary>
    /// Мониторинг утечек памяти с автоматическими исправлениями.
    /// Memory leak monitoring with automatic fixes.
    /// </summary>
    public class MemoryLeakFixer : MonoBehaviour
    {
        [Header("Monitoring")]
        [SerializeField] private bool enableAutoFix = true;
        [SerializeField] private float checkInterval = 10f;
        [SerializeField] private bool showLogs = true;
        
        [Header("Thresholds")]
        [SerializeField] private long maxHeapMB = 150;
        [SerializeField] private int maxGameObjects = 800;
        
        private float _lastCheckTime;
        private long _lastHeapSize;
        
        private void Start()
        {
            _lastCheckTime = Time.time;
            _lastHeapSize = GC.GetTotalMemory(false);
            
            if (showLogs)
                Debug.Log("[MemoryLeakFixer] Memory monitoring started");
        }
        
        private void Update()
        {
            if (Time.time - _lastCheckTime >= checkInterval)
            {
                CheckMemoryHealth();
                _lastCheckTime = Time.time;
            }
        }
        
        private void CheckMemoryHealth()
        {
            try
            {
                long currentHeap = GC.GetTotalMemory(false);
                long heapMB = currentHeap / (1024 * 1024);
                int gameObjectCount = FindObjectsOfType<GameObject>().Length;
                
                bool needsFixing = false;
                
                // Check heap size
                if (heapMB > maxHeapMB)
                {
                    if (showLogs)
                        Debug.LogWarning($"[MemoryLeakFixer] High heap usage: {heapMB}MB");
                    needsFixing = true;
                }
                
                // Check GameObject count
                if (gameObjectCount > maxGameObjects)
                {
                    if (showLogs)
                        Debug.LogWarning($"[MemoryLeakFixer] High GameObject count: {gameObjectCount}");
                    needsFixing = true;
                }
                
                // Check memory growth
                long memoryGrowth = currentHeap - _lastHeapSize;
                if (memoryGrowth > 10 * 1024 * 1024) // 10MB growth
                {
                    if (showLogs)
                        Debug.LogWarning($"[MemoryLeakFixer] Rapid memory growth: {memoryGrowth / (1024 * 1024)}MB");
                    needsFixing = true;
                }
                
                if (needsFixing && enableAutoFix)
                {
                    ApplyMemoryFixes();
                }
                
                _lastHeapSize = currentHeap;
                
                if (showLogs)
                {
                    Debug.Log($"[MemoryLeakFixer] Health check - Heap: {heapMB}MB, Objects: {gameObjectCount}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MemoryLeakFixer] Error during health check: {ex.Message}");
            }
        }
        
        private void ApplyMemoryFixes()
        {
            if (showLogs)
                Debug.Log("[MemoryLeakFixer] Applying memory fixes...");
            
            try
            {
                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // Clean up null references
                Resources.UnloadUnusedAssets();
                
                if (showLogs)
                    Debug.Log("[MemoryLeakFixer] Memory fixes applied");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MemoryLeakFixer] Error applying fixes: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Принудительная очистка памяти.
        /// Force memory cleanup.
        /// </summary>
        [ContextMenu("Force Memory Cleanup")]
        public void ForceCleanup()
        {
            ApplyMemoryFixes();
        }
        
        /// <summary>
        /// Получить отчет о памяти.
        /// Get memory report.
        /// </summary>
        public string GetMemoryReport()
        {
            long heap = GC.GetTotalMemory(false);
            int objects = FindObjectsOfType<GameObject>().Length;
            int lineRenderers = FindObjectsOfType<LineRenderer>().Length;
            
            return $"Memory Report:\\n" +
                   $"Heap: {heap / (1024 * 1024)}MB\\n" +
                   $"GameObjects: {objects}\\n" +
                   $"LineRenderers: {lineRenderers}";
        }
    }
}
