using UnityEngine;
using R3;
using ARDrawing.Core.Models;

namespace ARDrawing.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса отслеживания рук через OpenXR API.
    /// Interface for hand tracking service through OpenXR API.
    /// </summary>
    public interface IHandTrackingService
    {
        /// <summary>
        /// Observable поток позиций указательного пальца правой руки.
        /// Observable stream of right hand index finger positions.
        /// </summary>
        Observable<Vector3> IndexFingerPosition { get; }
        
        /// <summary>
        /// Observable поток состояний касания (pinch gesture).
        /// Observable stream of touch states (pinch gesture).
        /// </summary>
        Observable<bool> IsIndexFingerTouching { get; }
        
        /// <summary>
        /// Observable поток данных взаимодействия с руками.
        /// Observable stream of hand interaction data.
        /// </summary>
        Observable<HandInteractionData> UIInteraction { get; }
        
        /// <summary>
        /// Observable поток уровня уверенности отслеживания (0-1).
        /// Observable stream of tracking confidence level (0-1).
        /// </summary>
        Observable<float> HandTrackingConfidence { get; }
        
        /// <summary>
        /// Observable поток определения активной руки для рисования.
        /// Observable stream of active hand detection for drawing.
        /// </summary>
        Observable<bool> IsRightHandTracked { get; }
        
        /// <summary>
        /// Получить текущую позицию указательного пальца.
        /// Get current index finger position.
        /// </summary>
        /// <returns>Текущая позиция / Current position</returns>
        Vector3 GetCurrentIndexFingerPosition();
    }
}