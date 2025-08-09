using UnityEngine;
using R3;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using System;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Сервис отслеживания рук через OpenXR API (заглушка для Phase 1).
    /// Hand tracking service through OpenXR API (stub for Phase 1).
    /// </summary>
    public class OpenXRHandTrackingService : IHandTrackingService, IDisposable
    {
        private readonly Subject<Vector3> _indexFingerPosition = new();
        private readonly Subject<bool> _isIndexFingerTouching = new();
        private readonly Subject<HandInteractionData> _uiInteraction = new();
        private readonly Subject<float> _handTrackingConfidence = new();
        private readonly Subject<bool> _isRightHandTracked = new();
        
        public Observable<Vector3> IndexFingerPosition => _indexFingerPosition.AsObservable();
        public Observable<bool> IsIndexFingerTouching => _isIndexFingerTouching.AsObservable();
        public Observable<HandInteractionData> UIInteraction => _uiInteraction.AsObservable();
        public Observable<float> HandTrackingConfidence => _handTrackingConfidence.AsObservable();
        public Observable<bool> IsRightHandTracked => _isRightHandTracked.AsObservable();
        
        /// <summary>
        /// Инициализация сервиса отслеживания рук.
        /// Initialize hand tracking service.
        /// </summary>
        public OpenXRHandTrackingService()
        {
            Debug.Log("OpenXRHandTrackingService: Initialized (stub) / Инициализирован (заглушка)");
            
            // TODO Phase 2: Реальная реализация OpenXR Hand Tracking
            // TODO Phase 2: Real OpenXR Hand Tracking implementation
        }
        
        public void Dispose()
        {
            _indexFingerPosition?.Dispose();
            _isIndexFingerTouching?.Dispose();
            _uiInteraction?.Dispose();
            _handTrackingConfidence?.Dispose();
            _isRightHandTracked?.Dispose();
        }
    }
}