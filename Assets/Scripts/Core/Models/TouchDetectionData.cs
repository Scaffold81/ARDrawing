using UnityEngine;
using R3;
using System;

namespace ARDrawing.Core.Models
{
    /// <summary>
    /// Состояния касания для pinch жеста.
    /// Touch states for pinch gesture.
    /// </summary>
    public enum TouchState
    {
        /// <summary>Касание не обнаружено / No touch detected</summary>
        None,
        
        /// <summary>Начало касания / Touch started</summary>
        Started,
        
        /// <summary>Продолжение касания / Touch continuing</summary>
        Active,
        
        /// <summary>Окончание касания / Touch ended</summary>
        Ended
    }
    
    /// <summary>
    /// Данные события касания с состоянием и таймингом.
    /// Touch event data with state and timing.
    /// </summary>
    [System.Serializable]
    public struct TouchEventData
    {
        /// <summary>Состояние касания / Touch state</summary>
        public TouchState state;
        
        /// <summary>Позиция пальца / Finger position</summary>
        public Vector3 position;
        
        /// <summary>Сила касания (0-1) / Touch strength (0-1)</summary>
        public float strength;
        
        /// <summary>Продолжительность касания / Touch duration</summary>
        public float duration;
        
        /// <summary>Временная метка / Timestamp</summary>
        public float timestamp;
        
        /// <summary>Уверенность отслеживания / Tracking confidence</summary>
        public float confidence;
        
        public TouchEventData(TouchState touchState, Vector3 pos, float str, float dur, float conf)
        {
            state = touchState;
            position = pos;
            strength = str;
            duration = dur;
            timestamp = Time.time;
            confidence = conf;
        }
    }
    
    /// <summary>
    /// Настройки для определения касания.
    /// Touch detection settings.
    /// </summary>
    [System.Serializable]
    public struct TouchDetectionSettings
    {
        /// <summary>Порог расстояния для pinch жеста (м) / Distance threshold for pinch gesture (m)</summary>
        [Range(0.01f, 0.1f)]
        public float pinchThreshold;
        
        /// <summary>Гистерезис для стабильности / Hysteresis for stability</summary>
        [Range(0.005f, 0.05f)]
        public float hysteresis;
        
        /// <summary>Минимальное время для регистрации касания (сек) / Minimum time to register touch (sec)</summary>
        [Range(0.01f, 0.5f)]
        public float minTouchDuration;
        
        /// <summary>Максимальное время между касаниями (сек) / Maximum time between touches (sec)</summary>
        [Range(0.1f, 2.0f)]
        public float maxTouchGap;
        
        /// <summary>Минимальная уверенность для касания / Minimum confidence for touch</summary>
        [Range(0.1f, 1.0f)]
        public float minConfidence;
        
        /// <summary>Сглаживание для стабильности / Smoothing for stability</summary>
        [Range(0.1f, 1.0f)]
        public float smoothingFactor;
        
        public static TouchDetectionSettings Default => new TouchDetectionSettings
        {
            pinchThreshold = 0.03f,
            hysteresis = 0.01f,
            minTouchDuration = 0.05f,
            maxTouchGap = 0.3f,
            minConfidence = 0.7f,
            smoothingFactor = 0.8f
        };
    }
}