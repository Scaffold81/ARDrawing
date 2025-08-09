using UnityEngine;
using ARDrawing.Core.Models;

namespace ARDrawing.Core.Config
{
    /// <summary>
    /// ScriptableObject для хранения настроек рисования в AR приложении.
    /// ScriptableObject for storing drawing settings in AR application.
    /// </summary>
    [CreateAssetMenu(fileName = "DrawingSettings", menuName = "ARDrawing/Drawing Settings")]
    public class DrawingSettingsConfig : ScriptableObject
    {
        [Header("Line Settings / Настройки линий")]
        [SerializeField] private Color defaultLineColor = Color.white;
        [SerializeField] [Range(0.001f, 0.1f)] private float defaultLineThickness = 0.01f;
        
        [Header("Touch Sensitivity / Чувствительность касания")]
        [SerializeField] [Range(0.1f, 1.0f)] private float touchSensitivity = 0.8f;
        [SerializeField] [Range(0.001f, 0.02f)] private float minPointDistance = 0.005f;
        
        [Header("Performance / Производительность")]
        [SerializeField] [Range(100, 5000)] private int maxPointsPerLine = 1000;
        [SerializeField] [Range(8f, 33f)] private float updateThrottleMs = 16f;
        
        [Header("Available Colors / Доступные цвета")]
        [SerializeField] private Color[] availableColors = {
            Color.white,
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta
        };
        
        [Header("Available Thicknesses / Доступные толщины")]
        [SerializeField] private float[] availableThicknesses = {
            0.005f,  // Тонкая / Thin
            0.01f,   // Обычная / Normal  
            0.02f,   // Толстая / Thick
            0.03f    // Очень толстая / Very thick
        };
        
        /// <summary>
        /// Конвертирует настройки в структуру DrawingSettings.
        /// Converts settings to DrawingSettings structure.
        /// </summary>
        public DrawingSettings ToDrawingSettings()
        {
            return new DrawingSettings
            {
                lineColor = defaultLineColor,
                lineThickness = defaultLineThickness,
                touchSensitivity = touchSensitivity,
                minPointDistance = minPointDistance,
                maxPointsPerLine = maxPointsPerLine,
                updateThrottleMs = updateThrottleMs
            };
        }
        
        /// <summary>
        /// Получает массив доступных цветов.
        /// Gets array of available colors.
        /// </summary>
        public Color[] GetAvailableColors() => availableColors;
        
        /// <summary>
        /// Получает массив доступных толщин линий.
        /// Gets array of available line thicknesses.
        /// </summary>
        public float[] GetAvailableThicknesses() => availableThicknesses;
        
        /// <summary>
        /// Валидация настроек при изменении в Inspector.
        /// Validate settings when changed in Inspector.
        /// </summary>
        private void OnValidate()
        {
            // Проверяем корректность значений
            // Check value correctness
            if (defaultLineThickness <= 0)
                defaultLineThickness = 0.01f;
                
            if (minPointDistance <= 0)
                minPointDistance = 0.005f;
                
            if (maxPointsPerLine < 100)
                maxPointsPerLine = 100;
                
            if (updateThrottleMs < 8f)
                updateThrottleMs = 8f;
                
            // Проверяем что есть хотя бы один цвет
            // Check that there's at least one color
            if (availableColors == null || availableColors.Length == 0)
            {
                availableColors = new Color[] { Color.white };
            }
            
            // Проверяем что есть хотя бы одна толщина
            // Check that there's at least one thickness
            if (availableThicknesses == null || availableThicknesses.Length == 0)
            {
                availableThicknesses = new float[] { 0.01f };
            }
        }
    }
}