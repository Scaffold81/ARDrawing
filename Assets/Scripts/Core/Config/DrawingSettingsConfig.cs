using UnityEngine;
using ARDrawing.Core.Models;

namespace ARDrawing.Core.Config
{
    /// <summary>
    /// ScriptableObject конфигурация для настроек рисования.
    /// ScriptableObject configuration for drawing settings.
    /// </summary>
    [CreateAssetMenu(fileName = "DrawingSettings", menuName = "AR Drawing/Drawing Settings", order = 1)]
    public class DrawingSettingsConfig : ScriptableObject
    {
        [Header("Line Appearance")]
        [SerializeField] private Color _lineColor = Color.white;
        [SerializeField, Range(0.001f, 0.1f)] private float _lineWidth = 0.005f;
        
        [Header("Performance Limits")]
        [SerializeField, Range(1, 1000)] private int _maxLinesCount = 100;
        [SerializeField, Range(10, 10000)] private int _maxPointsPerLine = 1000;
        [SerializeField, Range(0.001f, 0.1f)] private float _minDistanceBetweenPoints = 0.01f;
        
        [Header("Object Pool Settings")]
        [SerializeField, Range(5, 100)] private int _linePoolInitialSize = 20;
        [SerializeField, Range(50, 500)] private int _linePoolMaxSize = 100;
        
        [Header("Drawing Behavior")]
        [SerializeField, Range(0.1f, 2.0f)] private float _touchSensitivity = 1.0f;
        [SerializeField] private bool _enableSmoothing = true;
        [SerializeField, Range(2, 10)] private int _smoothingIterations = 3;
        
        [Header("Available Colors")]
        [SerializeField] private Color[] _availableColors = new Color[]
        {
            Color.white,
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            new Color(1f, 0.5f, 0f), // Orange
            new Color(0.5f, 0f, 1f), // Purple
            Color.black
        };
        
        [Header("Line Width Presets")]
        [SerializeField] private float[] _lineWidthPresets = new float[]
        {
            0.002f, // Тонкая
            0.005f, // Обычная
            0.01f,  // Толстая
            0.02f,  // Очень толстая
            0.05f   // Маркер
        };
        
        /// <summary>
        /// Конвертирует ScriptableObject в структуру DrawingSettings.
        /// Converts ScriptableObject to DrawingSettings structure.
        /// </summary>
        /// <returns>Структура настроек / Settings structure</returns>
        public DrawingSettings ToDrawingSettings()
        {
            return new DrawingSettings
            {
                LineColor = _lineColor,
                LineWidth = _lineWidth,
                MaxLinesCount = _maxLinesCount,
                MaxPointsPerLine = _maxPointsPerLine,
                MinDistanceBetweenPoints = _minDistanceBetweenPoints,
                LinePoolInitialSize = _linePoolInitialSize,
                LinePoolMaxSize = _linePoolMaxSize
            };
        }
        
        /// <summary>
        /// Обновляет настройки из структуры DrawingSettings.
        /// Updates settings from DrawingSettings structure.
        /// </summary>
        /// <param name="settings">Структура настроек / Settings structure</param>
        public void FromDrawingSettings(DrawingSettings settings)
        {
            _lineColor = settings.LineColor;
            _lineWidth = settings.LineWidth;
            _maxLinesCount = settings.MaxLinesCount;
            _maxPointsPerLine = settings.MaxPointsPerLine;
            _minDistanceBetweenPoints = settings.MinDistanceBetweenPoints;
            _linePoolInitialSize = settings.LinePoolInitialSize;
            _linePoolMaxSize = settings.LinePoolMaxSize;
        }
        
        /// <summary>
        /// Получает массив доступных цветов.
        /// Gets array of available colors.
        /// </summary>
        /// <returns>Массив цветов / Array of colors</returns>
        public Color[] GetAvailableColors()
        {
            return (Color[])_availableColors.Clone();
        }
        
        /// <summary>
        /// Получает массив предустановленных толщин линий.
        /// Gets array of line width presets.
        /// </summary>
        /// <returns>Массив толщин / Array of widths</returns>
        public float[] GetLineWidthPresets()
        {
            return (float[])_lineWidthPresets.Clone();
        }
        
        /// <summary>
        /// Получает цвет по индексу.
        /// Gets color by index.
        /// </summary>
        /// <param name="index">Индекс цвета / Color index</param>
        /// <returns>Цвет или белый если индекс неверный / Color or white if invalid index</returns>
        public Color GetColorByIndex(int index)
        {
            return index >= 0 && index < _availableColors.Length ? _availableColors[index] : Color.white;
        }
        
        /// <summary>
        /// Получает толщину линии по индексу.
        /// Gets line width by index.
        /// </summary>
        /// <param name="index">Индекс толщины / Width index</param>
        /// <returns>Толщина или значение по умолчанию / Width or default value</returns>
        public float GetLineWidthByIndex(int index)
        {
            return index >= 0 && index < _lineWidthPresets.Length ? _lineWidthPresets[index] : 0.005f;
        }
        
        /// <summary>
        /// Применяет настройки производительности для Quest 2.
        /// Applies performance settings for Quest 2.
        /// </summary>
        public void ApplyQuest2OptimizedSettings()
        {
            _maxLinesCount = 50;
            _maxPointsPerLine = 500;
            _minDistanceBetweenPoints = 0.015f;
            _linePoolInitialSize = 15;
            _linePoolMaxSize = 60;
            _enableSmoothing = false;
        }
        
        /// <summary>
        /// Применяет настройки производительности для Quest 3.
        /// Applies performance settings for Quest 3.
        /// </summary>
        public void ApplyQuest3OptimizedSettings()
        {
            _maxLinesCount = 100;
            _maxPointsPerLine = 1000;
            _minDistanceBetweenPoints = 0.01f;
            _linePoolInitialSize = 20;
            _linePoolMaxSize = 100;
            _enableSmoothing = true;
            _smoothingIterations = 3;
        }
        
        /// <summary>
        /// Применяет настройки для высокого качества (для тестирования).
        /// Applies high quality settings (for testing).
        /// </summary>
        public void ApplyHighQualitySettings()
        {
            _maxLinesCount = 200;
            _maxPointsPerLine = 2000;
            _minDistanceBetweenPoints = 0.005f;
            _linePoolInitialSize = 30;
            _linePoolMaxSize = 150;
            _enableSmoothing = true;
            _smoothingIterations = 5;
        }
        
        /// <summary>
        /// Сбрасывает настройки к значениям по умолчанию.
        /// Resets settings to default values.
        /// </summary>
        [ContextMenu("Reset to Default")]
        public void ResetToDefault()
        {
            var defaultSettings = DrawingSettings.Default;
            FromDrawingSettings(defaultSettings);
        }
        
        /// <summary>
        /// Валидирует настройки и исправляет некорректные значения.
        /// Validates settings and fixes incorrect values.
        /// </summary>
        [ContextMenu("Validate Settings")]
        public void ValidateSettings()
        {
            _lineWidth = Mathf.Clamp(_lineWidth, 0.001f, 0.1f);
            _maxLinesCount = Mathf.Clamp(_maxLinesCount, 1, 1000);
            _maxPointsPerLine = Mathf.Clamp(_maxPointsPerLine, 10, 10000);
            _minDistanceBetweenPoints = Mathf.Clamp(_minDistanceBetweenPoints, 0.001f, 0.1f);
            _linePoolInitialSize = Mathf.Clamp(_linePoolInitialSize, 5, 100);
            _linePoolMaxSize = Mathf.Clamp(_linePoolMaxSize, 50, 500);
            _touchSensitivity = Mathf.Clamp(_touchSensitivity, 0.1f, 2.0f);
            _smoothingIterations = Mathf.Clamp(_smoothingIterations, 2, 10);
            
            // Убеждаемся что начальный размер пула не больше максимального
            if (_linePoolInitialSize > _linePoolMaxSize)
            {
                _linePoolInitialSize = _linePoolMaxSize / 2;
            }
        }
        
        private void OnValidate()
        {
            ValidateSettings();
        }
    }
}