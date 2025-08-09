using UnityEngine;
using System;
using System.Collections.Generic;

namespace ARDrawing.Core.Models
{
    /// <summary>
    /// Данные взаимодействия с руками для отслеживания жестов.
    /// Hand interaction data for gesture tracking.
    /// </summary>
    [System.Serializable]
    public struct HandInteractionData
    {
        /// <summary>Позиция указательного пальца / Index finger position</summary>
        public Vector3 indexFingerPosition;
        
        /// <summary>Состояние касания (pinch) / Touch state (pinch)</summary>
        public bool isTouching;
        
        /// <summary>Уверенность отслеживания (0-1) / Tracking confidence (0-1)</summary>
        public float confidence;
        
        /// <summary>Активная рука (true = правая, false = левая) / Active hand (true = right, false = left)</summary>
        public bool isRightHand;
        
        /// <summary>Временная метка / Timestamp</summary>
        public float timestamp;
        
        public HandInteractionData(Vector3 fingerPos, bool touching, float conf, bool rightHand)
        {
            indexFingerPosition = fingerPos;
            isTouching = touching;
            confidence = conf;
            isRightHand = rightHand;
            timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Данные линии рисования с точками и настройками.
    /// Drawing line data with points and settings.
    /// </summary>
    [System.Serializable]
    public class DrawingLine
    {
        /// <summary>Список точек линии / List of line points</summary>
        public List<Vector3> points;
        
        /// <summary>Цвет линии / Line color</summary>
        public Color color;
        
        /// <summary>Толщина линии / Line thickness</summary>
        public float thickness;
        
        /// <summary>Временная метка создания / Creation timestamp</summary>
        public float creationTime;
        
        /// <summary>Уникальный идентификатор линии / Unique line identifier</summary>
        public string lineId;
        
        public DrawingLine()
        {
            points = new List<Vector3>();
            color = Color.white;
            thickness = 0.01f;
            creationTime = Time.time;
            lineId = Guid.NewGuid().ToString();
        }
        
        public DrawingLine(Color lineColor, float lineThickness) : this()
        {
            color = lineColor;
            thickness = lineThickness;
        }
    }
    
    /// <summary>
    /// Настройки рисования (цвет, толщина, чувствительность).
    /// Drawing settings (color, thickness, sensitivity).
    /// </summary>
    [System.Serializable]
    public struct DrawingSettings
    {
        /// <summary>Цвет линии / Line color</summary>
        public Color lineColor;
        
        /// <summary>Толщина линии / Line thickness</summary>
        public float lineThickness;
        
        /// <summary>Чувствительность касания / Touch sensitivity</summary>
        public float touchSensitivity;
        
        /// <summary>Минимальное расстояние между точками / Minimum distance between points</summary>
        public float minPointDistance;
        
        /// <summary>Максимальное количество точек в линии / Maximum points per line</summary>
        public int maxPointsPerLine;
        
        /// <summary>Время throttling обновлений (мс) / Update throttling time (ms)</summary>
        public float updateThrottleMs;
        
        public static DrawingSettings Default => new DrawingSettings
        {
            lineColor = Color.white,
            lineThickness = 0.01f,
            touchSensitivity = 0.8f,
            minPointDistance = 0.005f,
            maxPointsPerLine = 1000,
            updateThrottleMs = 16f // ~60 FPS
        };
    }
}