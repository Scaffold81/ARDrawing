using UnityEngine;
using System;

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
}