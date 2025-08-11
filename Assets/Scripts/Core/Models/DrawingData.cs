using UnityEngine;
using System.Collections.Generic;
using System;

namespace ARDrawing.Core.Models
{
    /// <summary>
    /// Данные для одной линии рисования.
    /// Data for a single drawing line.
    /// </summary>
    [Serializable]
    public class DrawingLine
    {
        [SerializeField] private List<Vector3> _points = new List<Vector3>();
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private float _width = 0.01f;
        [SerializeField] private DateTime _createdTime = DateTime.Now;
        
        /// <summary>
        /// Точки линии в мировых координатах.
        /// Line points in world coordinates.
        /// </summary>
        public List<Vector3> Points => _points;
        
        /// <summary>
        /// Цвет линии.
        /// Line color.
        /// </summary>
        public Color Color
        {
            get => _color;
            set => _color = value;
        }
        
        /// <summary>
        /// Ширина линии.
        /// Line width.
        /// </summary>
        public float Width
        {
            get => _width;
            set => _width = Mathf.Clamp(value, 0.001f, 0.1f);
        }
        
        /// <summary>
        /// Время создания линии.
        /// Line creation time.
        /// </summary>
        public DateTime CreatedTime => _createdTime;
        
        /// <summary>
        /// Количество точек в линии.
        /// Number of points in the line.
        /// </summary>
        public int PointCount => _points.Count;
        
        /// <summary>
        /// Является ли линия пустой.
        /// Whether the line is empty.
        /// </summary>
        public bool IsEmpty => _points.Count == 0;
        
        public DrawingLine()
        {
            _points = new List<Vector3>();
            _createdTime = DateTime.Now;
        }
        
        public DrawingLine(Color color, float width) : this()
        {
            _color = color;
            _width = width;
        }
        
        /// <summary>
        /// Добавляет точку к линии.
        /// Adds a point to the line.
        /// </summary>
        /// <param name="point">Позиция точки в мировых координатах / Point position in world coordinates</param>
        public void AddPoint(Vector3 point)
        {
            _points.Add(point);
        }
        
        /// <summary>
        /// Очищает все точки линии.
        /// Clears all line points.
        /// </summary>
        public void Clear()
        {
            _points.Clear();
            _createdTime = DateTime.Now;
        }
        
        /// <summary>
        /// Получает расстояние от последней точки до указанной позиции.
        /// Gets distance from the last point to the specified position.
        /// </summary>
        /// <param name="position">Позиция для сравнения / Position to compare</param>
        /// <returns>Расстояние или float.MaxValue если линия пустая / Distance or float.MaxValue if line is empty</returns>
        public float GetDistanceFromLastPoint(Vector3 position)
        {
            if (IsEmpty) return float.MaxValue;
            return Vector3.Distance(_points[_points.Count - 1], position);
        }
        
        /// <summary>
        /// Создает копию линии.
        /// Creates a copy of the line.
        /// </summary>
        /// <returns>Копия линии / Copy of the line</returns>
        public DrawingLine Clone()
        {
            var clone = new DrawingLine(_color, _width);
            clone._points.AddRange(_points);
            clone._createdTime = _createdTime;
            return clone;
        }
    }
    
    /// <summary>
    /// Настройки для рисования линий.
    /// Settings for drawing lines.
    /// </summary>
    [Serializable]
    public class DrawingSettings
    {
        [Header("Line Appearance")]
        [SerializeField] private Color _lineColor = Color.white;
        [SerializeField, Range(0.001f, 0.1f)] private float _lineWidth = 0.005f;
        
        [Header("Performance")]
        [SerializeField, Range(1, 1000)] private int _maxLinesCount = 100;
        [SerializeField, Range(10, 10000)] private int _maxPointsPerLine = 1000;
        [SerializeField, Range(0.001f, 0.1f)] private float _minDistanceBetweenPoints = 0.01f;
        
        [Header("Line Pool")]
        [SerializeField, Range(5, 100)] private int _linePoolInitialSize = 20;
        [SerializeField, Range(50, 500)] private int _linePoolMaxSize = 100;
        
        /// <summary>
        /// Цвет линий по умолчанию.
        /// Default line color.
        /// </summary>
        public Color LineColor
        {
            get => _lineColor;
            set => _lineColor = value;
        }
        
        /// <summary>
        /// Ширина линий по умолчанию.
        /// Default line width.
        /// </summary>
        public float LineWidth
        {
            get => _lineWidth;
            set => _lineWidth = Mathf.Clamp(value, 0.001f, 0.1f);
        }
        
        /// <summary>
        /// Максимальное количество линий.
        /// Maximum number of lines.
        /// </summary>
        public int MaxLinesCount
        {
            get => _maxLinesCount;
            set => _maxLinesCount = Mathf.Clamp(value, 1, 1000);
        }
        
        /// <summary>
        /// Максимальное количество точек в одной линии.
        /// Maximum number of points per line.
        /// </summary>
        public int MaxPointsPerLine
        {
            get => _maxPointsPerLine;
            set => _maxPointsPerLine = Mathf.Clamp(value, 10, 10000);
        }
        
        /// <summary>
        /// Минимальное расстояние между точками.
        /// Minimum distance between points.
        /// </summary>
        public float MinDistanceBetweenPoints
        {
            get => _minDistanceBetweenPoints;
            set => _minDistanceBetweenPoints = Mathf.Clamp(value, 0.001f, 0.1f);
        }
        
        /// <summary>
        /// Начальный размер пула линий.
        /// Initial size of line pool.
        /// </summary>
        public int LinePoolInitialSize
        {
            get => _linePoolInitialSize;
            set => _linePoolInitialSize = Mathf.Clamp(value, 5, 100);
        }
        
        /// <summary>
        /// Максимальный размер пула линий.
        /// Maximum size of line pool.
        /// </summary>
        public int LinePoolMaxSize
        {
            get => _linePoolMaxSize;
            set => _linePoolMaxSize = Mathf.Clamp(value, 50, 500);
        }
        
        public static DrawingSettings Default => new DrawingSettings
        {
            _lineColor = Color.white,
            _lineWidth = 0.005f,
            _maxLinesCount = 100,
            _maxPointsPerLine = 1000,
            _minDistanceBetweenPoints = 0.01f,
            _linePoolInitialSize = 20,
            _linePoolMaxSize = 100
        };
    }
    
    /// <summary>
    /// Данные линии рендера для пула объектов.
    /// Line renderer data for object pooling.
    /// </summary>
    public class PooledLineRenderer
    {
        public LineRenderer LineRenderer { get; set; }
        public GameObject GameObject { get; set; }
        public bool IsActive { get; set; }
        public DrawingLine AssociatedLine { get; set; }
        
        public PooledLineRenderer(LineRenderer lineRenderer)
        {
            LineRenderer = lineRenderer;
            GameObject = lineRenderer.gameObject;
            IsActive = false;
            AssociatedLine = null;
        }
        
        /// <summary>
        /// Активирует линию рендера.
        /// Activates the line renderer.
        /// </summary>
        /// <param name="line">Ассоциированная линия данных / Associated line data</param>
        public void Activate(DrawingLine line)
        {
            AssociatedLine = line;
            IsActive = true;
            GameObject.SetActive(true);
        }
        
        /// <summary>
        /// Деактивирует линию рендера.
        /// Deactivates the line renderer.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            AssociatedLine = null;
            GameObject.SetActive(false);
            LineRenderer.positionCount = 0;
        }
        
        /// <summary>
        /// Обновляет визуализацию линии.
        /// Updates line visualization.
        /// </summary>
        public void UpdateLine()
        {
            if (AssociatedLine == null || !IsActive) return;
            
            var points = AssociatedLine.Points;
            LineRenderer.positionCount = points.Count;
            
            if (points.Count > 0)
            {
                LineRenderer.SetPositions(points.ToArray());
                LineRenderer.startWidth = AssociatedLine.Width;
                LineRenderer.endWidth = AssociatedLine.Width;
                LineRenderer.startColor = AssociatedLine.Color;
                LineRenderer.endColor = AssociatedLine.Color;
            }
        }
    }
    
    /// <summary>
    /// Типы инструментов рисования.
    /// Drawing tool types.
    /// </summary>
    public enum DrawingTool
    {
        Pen,
        Eraser,
        Line,
        Shape
    }
    
    /// <summary>
    /// Режимы UI взаимодействия.
    /// UI interaction modes.
    /// </summary>
    public enum UIInteractionMode
    {
        Drawing,
        ColorPicker
    }
}