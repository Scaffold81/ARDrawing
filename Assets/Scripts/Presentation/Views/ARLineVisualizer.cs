using UnityEngine;
using ARDrawing.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ARDrawing.Presentation.Views
{
    /// <summary>
    /// ARLineVisualizer - упрощенная версия для совместимости с ARDrawingView.
    /// ARLineVisualizer - simplified version compatible with ARDrawingView.
    /// </summary>
    public class ARLineVisualizer : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private LineRenderer mainLineRenderer;
        
        [Header("LOD Settings")]
        [SerializeField] private int[] lodPointCounts = { 100, 50, 25 }; // Points per LOD level
        [SerializeField] private float[] lodWidthMultipliers = { 1.0f, 0.8f, 0.6f };
        
        // Visual State
        private DrawingLine currentLine;
        private ARDrawingView parentView;
        private int currentLODLevel = 0;
        private bool isVisible = true;
        private float currentAlpha = 1.0f;
        
        // Material State
        private Material mainMaterial;
        private Color originalColor;
        private float originalWidth;
        
        // Performance Caching
        private Vector3[] cachedPoints;
        private int lastPointCount = 0;
        private bool needsUpdate = true;
        
        #region Initialization
        
        /// <summary>
        /// Инициализирует визуализатор с родительским ARDrawingView.
        /// Initializes visualizer with parent ARDrawingView.
        /// </summary>
        public void Initialize(ARDrawingView parent)
        {
            parentView = parent;
            SetupComponents();
            SetupMaterials();
        }
        
        private void SetupComponents()
        {
            // Main LineRenderer
            if (mainLineRenderer == null)
            {
                mainLineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            
            ConfigureLineRenderer(mainLineRenderer);
        }
        
        private void ConfigureLineRenderer(LineRenderer renderer)
        {
            renderer.useWorldSpace = true;
            renderer.positionCount = 0;
            renderer.material = CreateMainMaterial();
            renderer.widthMultiplier = 1.0f;
            renderer.sortingOrder = 0;
            
            // Enhanced visual settings
            renderer.receiveShadows = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
        }
        
        private void SetupMaterials()
        {
            mainMaterial = CreateMainMaterial();
        }
        
        private Material CreateMainMaterial()
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.name = "ARLineMainMaterial";
            material.color = Color.white;
            return material;
        }
        
        #endregion
        
        #region Line Management
        
        /// <summary>
        /// Настраивает визуализатор для отображения указанной линии.
        /// Sets up visualizer to display specified line.
        /// </summary>
        public void SetupForLine(DrawingLine line)
        {
            currentLine = line;
            originalColor = line.Color;
            originalWidth = line.Width;
            
            UpdateMaterialProperties();
            UpdateFromLine(line);
            
            needsUpdate = true;
        }
        
        /// <summary>
        /// Обновляет визуализацию на основе изменений в линии.
        /// Updates visualization based on line changes.
        /// </summary>
        public void UpdateFromLine(DrawingLine line)
        {
            if (line == null || line.Points == null) return;
            
            currentLine = line;
            
            // Check if we need to update points
            if (line.Points.Count != lastPointCount)
            {
                needsUpdate = true;
                lastPointCount = line.Points.Count;
            }
            
            if (needsUpdate)
            {
                UpdateLinePoints();
                needsUpdate = false;
            }
        }
        
        private void UpdateLinePoints()
        {
            if (currentLine == null || currentLine.Points == null) return;
            
            var points = GetLODFilteredPoints();
            
            // Update main line renderer
            if (mainLineRenderer != null)
            {
                mainLineRenderer.positionCount = points.Length;
                mainLineRenderer.SetPositions(points);
            }
            
            cachedPoints = points;
        }
        
        private Vector3[] GetLODFilteredPoints()
        {
            if (currentLine.Points.Count == 0) return new Vector3[0];
            
            int maxPoints = lodPointCounts[Mathf.Min(currentLODLevel, lodPointCounts.Length - 1)];
            
            if (currentLine.Points.Count <= maxPoints)
            {
                return currentLine.Points.ToArray();
            }
            
            // Sample points evenly for LOD
            var filteredPoints = new List<Vector3>();
            float step = (float)(currentLine.Points.Count - 1) / (maxPoints - 1);
            
            for (int i = 0; i < maxPoints; i++)
            {
                int index = Mathf.RoundToInt(i * step);
                index = Mathf.Clamp(index, 0, currentLine.Points.Count - 1);
                filteredPoints.Add(currentLine.Points[index]);
            }
            
            return filteredPoints.ToArray();
        }
        
        private void UpdateMaterialProperties()
        {
            if (currentLine == null) return;
            
            // Update main material
            if (mainMaterial != null)
            {
                mainMaterial.color = ApplyAlpha(currentLine.Color, currentAlpha);
            }
            
            // Update line width
            float lodWidthMultiplier = lodWidthMultipliers[Mathf.Min(currentLODLevel, lodWidthMultipliers.Length - 1)];
            
            if (mainLineRenderer != null)
            {
                mainLineRenderer.widthMultiplier = currentLine.Width * lodWidthMultiplier;
                mainLineRenderer.material = mainMaterial;
            }
        }
        
        private Color ApplyAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, color.a * alpha);
        }
        
        #endregion
        
        #region Visual Effects
        
        /// <summary>
        /// Устанавливает прозрачность визуализатора.
        /// Sets visualizer alpha transparency.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            currentAlpha = Mathf.Clamp01(alpha);
            UpdateMaterialProperties();
        }
        
        /// <summary>
        /// Устанавливает видимость визуализатора.
        /// Sets visualizer visibility.
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;
            
            if (mainLineRenderer != null)
                mainLineRenderer.enabled = visible;
        }
        
        /// <summary>
        /// Устанавливает уровень детализации (LOD).
        /// Sets level of detail (LOD).
        /// </summary>
        public void SetLODLevel(int lodLevel)
        {
            if (currentLODLevel != lodLevel)
            {
                currentLODLevel = Mathf.Clamp(lodLevel, 0, lodPointCounts.Length - 1);
                needsUpdate = true;
                
                // Immediately update if we have a line
                if (currentLine != null)
                {
                    UpdateLinePoints();
                    UpdateMaterialProperties();
                }
            }
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Получает bounds визуализатора для culling.
        /// Gets visualizer bounds for culling.
        /// </summary>
        public Bounds GetBounds()
        {
            if (mainLineRenderer != null && mainLineRenderer.positionCount > 0)
            {
                return mainLineRenderer.bounds;
            }
            
            // Fallback bounds
            return new Bounds(transform.position, Vector3.one);
        }
        
        /// <summary>
        /// Сбрасывает визуализатор к начальному состоянию.
        /// Resets visualizer to initial state.
        /// </summary>
        public void Reset()
        {
            currentLine = null;
            currentLODLevel = 0;
            currentAlpha = 1.0f;
            isVisible = true;
            needsUpdate = true;
            lastPointCount = 0;
            
            // Clear line renderer
            if (mainLineRenderer != null)
            {
                mainLineRenderer.positionCount = 0;
                mainLineRenderer.enabled = true;
            }
            
            cachedPoints = null;
        }
        
        /// <summary>
        /// Получает информацию о состоянии визуализатора.
        /// Gets visualizer state information.
        /// </summary>
        public string GetVisualizerInfo()
        {
            return $"ARLineVisualizer Info:\n" +
                   $"- Current Line Points: {(currentLine?.Points?.Count ?? 0)}\n" +
                   $"- LOD Level: {currentLODLevel}\n" +
                   $"- Alpha: {currentAlpha:F2}\n" +
                   $"- Visible: {isVisible}\n" +
                   $"- Cached Points: {cachedPoints?.Length ?? 0}\n" +
                   $"- Needs Update: {needsUpdate}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            // Cleanup materials
            if (mainMaterial != null)
            {
                DestroyImmediate(mainMaterial);
            }
        }
        
        #endregion
    }
}