using UnityEngine;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace ARDrawing.Presentation.Views
{
    /// <summary>
    /// ARDrawingView отвечает за enhanced визуализацию линий рисования.
    /// ARDrawingView handles enhanced visualization of drawing lines.
    /// </summary>
    public class ARDrawingView : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private bool enableEnhancedEffects = true;
        [SerializeField] private bool enableDebugLog = true;
        
        [Header("Performance")]
        [SerializeField] private int maxVisibleLines = 30;
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance = 5.0f;
        
        // Dependencies
        [Inject] private IDrawingService drawingService;
        
        // Visual Components
        private Dictionary<DrawingLine, LineRenderer> lineRenderers = new Dictionary<DrawingLine, LineRenderer>();
        private Queue<LineRenderer> rendererPool = new Queue<LineRenderer>();
        private List<Material> dynamicMaterials = new List<Material>();
        
        // Disposal tracking
        private bool _isDisposed = false;
        
        // Reactive Subscriptions
        private IDisposable activeLinesSubscription;
        
        // Performance
        private Camera arCamera;
        private int visibleLinesCount = 0;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            arCamera = Camera.main;
            if (arCamera == null)
                arCamera = FindFirstObjectByType<Camera>();
        }
        
        private void Start()
        {
            InitializeSystem();
            SubscribeToDrawingEvents();
        }
        
        private void Update()
        {
            if (enableLOD)
                UpdateLODSystem();
        }
        
        private void OnDestroy()
        {
            CleanupSystem();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeSystem()
        {
            CreateRendererPool();
        }
        
        private void CreateRendererPool()
        {
            for (int i = 0; i < maxVisibleLines; i++)
            {
                var renderer = CreateLineRenderer();
                renderer.gameObject.SetActive(false);
                rendererPool.Enqueue(renderer);
            }
        }
        
        private LineRenderer CreateLineRenderer()
        {
            var go = new GameObject("EnhancedLineRenderer");
            go.transform.SetParent(transform);
            
            var renderer = go.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.material = CreateDefaultMaterial();
            renderer.widthMultiplier = 0.01f;
            renderer.positionCount = 0;
            
            return renderer;
        }
        
        private Material CreateDefaultMaterial()
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = Color.white;
            dynamicMaterials.Add(material); // Track for cleanup
            return material;
        }
        
        #endregion
        
        #region Drawing Events
        
        private void SubscribeToDrawingEvents()
        {
            if (drawingService == null)
            {
                Debug.LogError("[ARDrawingView] DrawingService not injected!");
                return;
            }
            
            activeLinesSubscription = drawingService.ActiveLines
                .Subscribe(OnActiveLinesChanged);
        }
        
        private void OnActiveLinesChanged(List<DrawingLine> activeLines)
        {
            try
            {
                UpdateLineRenderers(activeLines);
                visibleLinesCount = activeLines.Count;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARDrawingView] Error updating line renderers: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Line Rendering
        
        private void UpdateLineRenderers(List<DrawingLine> activeLines)
        {
            // Remove renderers for lines that are no longer active
            var linesToRemove = new List<DrawingLine>();
            foreach (var kvp in lineRenderers)
            {
                if (!activeLines.Contains(kvp.Key))
                {
                    linesToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var line in linesToRemove)
            {
                RemoveLineRenderer(line);
            }
            
            // Add/update renderers for active lines
            foreach (var line in activeLines)
            {
                if (!lineRenderers.ContainsKey(line))
                {
                    AddLineRenderer(line);
                }
                else
                {
                    UpdateLineRenderer(line);
                }
            }
        }
        
        private void AddLineRenderer(DrawingLine line)
        {
            var renderer = GetRendererFromPool();
            if (renderer == null)
            {
                Debug.LogWarning("[ARDrawingView] No available renderers in pool");
                return;
            }
            
            SetupRenderer(renderer, line);
            lineRenderers[line] = renderer;
        }
        
        private void RemoveLineRenderer(DrawingLine line)
        {
            if (lineRenderers.TryGetValue(line, out var renderer))
            {
                ReturnRendererToPool(renderer);
                lineRenderers.Remove(line);
            }
        }
        
        private void UpdateLineRenderer(DrawingLine line)
        {
            if (lineRenderers.TryGetValue(line, out var renderer))
            {
                UpdateRendererFromLine(renderer, line);
            }
        }
        
        private void SetupRenderer(LineRenderer renderer, DrawingLine line)
        {
            renderer.gameObject.SetActive(true);
            renderer.material.color = line.Color;
            renderer.widthMultiplier = line.Width;
            
            UpdateRendererFromLine(renderer, line);
        }
        
        private void UpdateRendererFromLine(LineRenderer renderer, DrawingLine line)
        {
            if (line.Points == null || line.Points.Count == 0)
            {
                renderer.positionCount = 0;
                return;
            }
            
            var points = GetLODFilteredPoints(line);
            renderer.positionCount = points.Length;
            renderer.SetPositions(points);
        }
        
        private Vector3[] GetLODFilteredPoints(DrawingLine line)
        {
            if (!enableLOD || arCamera == null)
                return line.Points.ToArray();
            
            // Simple LOD - reduce points based on distance to camera
            if (line.Points.Count > 0)
            {
                float distance = Vector3.Distance(arCamera.transform.position, line.Points[0]);
                int maxPoints = distance < lodDistance ? 100 : 50;
                
                if (line.Points.Count <= maxPoints)
                    return line.Points.ToArray();
                
                // Sample points evenly
                var filteredPoints = new List<Vector3>();
                float step = (float)(line.Points.Count - 1) / (maxPoints - 1);
                
                for (int i = 0; i < maxPoints; i++)
                {
                    int index = Mathf.RoundToInt(i * step);
                    index = Mathf.Clamp(index, 0, line.Points.Count - 1);
                    filteredPoints.Add(line.Points[index]);
                }
                
                return filteredPoints.ToArray();
            }
            
            return line.Points.ToArray();
        }
        
        private LineRenderer GetRendererFromPool()
        {
            if (rendererPool.Count > 0)
            {
                var renderer = rendererPool.Dequeue();
                return renderer;
            }
            
            // Create new if pool is empty but under limit
            if (lineRenderers.Count < maxVisibleLines)
            {
                return CreateLineRenderer();
            }
            
            return null;
        }
        
        private void ReturnRendererToPool(LineRenderer renderer)
        {
            renderer.positionCount = 0;
            renderer.gameObject.SetActive(false);
            rendererPool.Enqueue(renderer);
        }
        
        #endregion
        
        #region LOD System
        
        private void UpdateLODSystem()
        {
            if (arCamera == null) return;
            
            Vector3 cameraPos = arCamera.transform.position;
            
            foreach (var kvp in lineRenderers)
            {
                var line = kvp.Key;
                var renderer = kvp.Value;
                
                if (line.Points.Count > 0)
                {
                    float distance = Vector3.Distance(cameraPos, line.Points[0]);
                    
                    // Update renderer based on distance
                    if (distance > lodDistance * 2f)
                    {
                        renderer.enabled = false; // Cull very distant lines
                    }
                    else
                    {
                        renderer.enabled = true;
                        UpdateRendererFromLine(renderer, line);
                    }
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Включает/выключает enhanced эффекты.
        /// Enables/disables enhanced effects.
        /// </summary>
        public void SetEnhancedEffects(bool enabled)
        {
            enableEnhancedEffects = enabled;
        }
        
        /// <summary>
        /// Получает статистику производительности.
        /// Gets performance statistics.
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"ARDrawingView Performance Stats:\n" +
                   $"- Visible Lines: {visibleLinesCount}\n" +
                   $"- Active Renderers: {lineRenderers.Count}\n" +
                   $"- Pool Size: {rendererPool.Count}\n" +
                   $"- Enhanced Effects: {enableEnhancedEffects}\n" +
                   $"- LOD Enabled: {enableLOD}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupSystem()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            // Dispose subscription
            try
            {
                activeLinesSubscription?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARDrawingView] Error disposing subscription: {ex.Message}");
            }
            finally
            {
                activeLinesSubscription = null;
            }
            
            // Cleanup all renderers safely
            try
            {
                foreach (var kvp in lineRenderers)
                {
                    var renderer = kvp.Value;
                    if (renderer != null && renderer.gameObject != null)
                    {
                        Destroy(renderer.gameObject);
                    }
                }
                lineRenderers.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARDrawingView] Error cleaning up line renderers: {ex.Message}");
            }
            
            // Cleanup pool safely
            try
            {
                while (rendererPool.Count > 0)
                {
                    var renderer = rendererPool.Dequeue();
                    if (renderer != null && renderer.gameObject != null)
                    {
                        Destroy(renderer.gameObject);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARDrawingView] Error cleaning up renderer pool: {ex.Message}");
            }
            
            // Cleanup dynamic materials
            try
            {
                foreach (var material in dynamicMaterials)
                {
                    if (material != null)
                    {
                        DestroyImmediate(material);
                    }
                }
                dynamicMaterials.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARDrawingView] Error cleaning up materials: {ex.Message}");
            }
            
            if (enableDebugLog)
                Debug.Log("[ARDrawingView] Enhanced view system cleaned up safely");
        }
        
        #endregion
    }
}