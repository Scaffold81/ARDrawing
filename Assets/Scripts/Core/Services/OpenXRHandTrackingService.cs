using UnityEngine;
using R3;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using System;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Реализация сервиса отслеживания рук через Meta XR API.
    /// Meta XR Hand Tracking service implementation.
    /// </summary>
    public class OpenXRHandTrackingService : MonoBehaviour, IHandTrackingService, IDisposable
    {
        [Header("Hand Tracking Settings")]
        [SerializeField] private float confidenceThreshold = 0.7f;
        [SerializeField] private float touchThreshold = 0.03f;
        [SerializeField] private bool debugOutput = true;
        
        [Header("OVR Hand References")]
        [SerializeField] private OVRHand rightOVRHand;
        [SerializeField] private OVRSkeleton rightHandSkeleton;
        
        // R3 Observable потоки / R3 Observable streams
        private readonly Subject<Vector3> _indexFingerPosition = new();
        private readonly Subject<bool> _isIndexFingerTouching = new();
        private readonly Subject<HandInteractionData> _uiInteraction = new();
        private readonly Subject<float> _handTrackingConfidence = new();
        private readonly Subject<bool> _isRightHandTracked = new();
        
        // Состояние отслеживания / Tracking state
        private bool _isInitialized = false;
        private HandInteractionData _lastInteractionData;
        
        // Кэш для оптимизации / Cache for optimization
        private Vector3 _lastIndexPosition = Vector3.zero;
        private bool _lastTouchState = false;
        private float _lastConfidence = 0f;
        
        public Observable<Vector3> IndexFingerPosition => _indexFingerPosition.AsObservable();
        public Observable<bool> IsIndexFingerTouching => _isIndexFingerTouching.AsObservable();
        public Observable<HandInteractionData> UIInteraction => _uiInteraction.AsObservable();
        public Observable<float> HandTrackingConfidence => _handTrackingConfidence.AsObservable();
        public Observable<bool> IsRightHandTracked => _isRightHandTracked.AsObservable();
        
        /// <summary>
        /// Инициализация сервиса отслеживания рук.
        /// Initialize hand tracking service.
        /// </summary>
        private void Start()
        {
            InitializeHandTracking();
        }
        
        /// <summary>
        /// Автоматический поиск OVR Hand компонентов в сцене.
        /// Automatic search for OVR Hand components in scene.
        /// </summary>
        private void InitializeHandTracking()
        {
            // Автоматически найти OVRHand компоненты если не назначены
            // Automatically find OVRHand components if not assigned
            if (rightOVRHand == null)
            {
                var ovrHands = FindObjectsOfType<OVRHand>();
                foreach (var hand in ovrHands)
                {
                    // Ищем правую руку по имени объекта
                    // Search for right hand by object name
                    if (hand.name.Contains("Right") || hand.name.Contains("right"))
                    {
                        rightOVRHand = hand;
                        break;
                    }
                }
            }
            
            // Найти OVRSkeleton для правой руки
            // Find OVRSkeleton for right hand
            if (rightHandSkeleton == null && rightOVRHand != null)
            {
                rightHandSkeleton = rightOVRHand.GetComponent<OVRSkeleton>();
            }
            
            // Альтернативный поиск OVRSkeleton в сцене
            // Alternative search for OVRSkeleton in scene
            if (rightHandSkeleton == null)
            {
                var skeletons = FindObjectsOfType<OVRSkeleton>();
                foreach (var skeleton in skeletons)
                {
                    if (skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight)
                    {
                        rightHandSkeleton = skeleton;
                        break;
                    }
                }
            }
            
            _isInitialized = rightOVRHand != null || rightHandSkeleton != null;
            
            if (_isInitialized)
            {
                if (debugOutput)
                {
                    Debug.Log("OpenXRHandTrackingService: OVR Hand components found and initialized / OVR Hand компоненты найдены и инициализированы");
                }
            }
            else
            {
                Debug.LogWarning("OpenXRHandTrackingService: No OVR Hand components found. Make sure hands are in the scene / OVR Hand компоненты не найдены. Убедитесь что руки есть в сцене");
            }
        }
        
        /// <summary>
        /// Обновление данных отслеживания рук каждый кадр.
        /// Update hand tracking data every frame.
        /// </summary>
        private void Update()
        {
            if (!_isInitialized)
                return;
                
            UpdateHandTracking();
        }
        
        /// <summary>
        /// Основная логика обновления отслеживания рук.
        /// Main hand tracking update logic.
        /// </summary>
        private void UpdateHandTracking()
        {
            bool isHandTracked = false;
            float confidence = 0f;
            
            // Проверяем OVRHand если доступен
            // Check OVRHand if available
            if (rightOVRHand != null)
            {
                isHandTracked = rightOVRHand.IsTracked;
                confidence = ConvertConfidenceToFloat(rightOVRHand.HandConfidence);
            }
            // Иначе используем OVRSkeleton
            // Otherwise use OVRSkeleton
            else if (rightHandSkeleton != null)
            {
                isHandTracked = rightHandSkeleton.IsInitialized && rightHandSkeleton.IsDataValid;
                confidence = CalculateSkeletonConfidence();
            }
            
            if (isHandTracked && confidence >= confidenceThreshold)
            {
                ProcessRightHandData();
            }
            else
            {
                // Рука не отслеживается или низкая уверенность
                // Hand not tracked or low confidence
                _isRightHandTracked.OnNext(false);
                _handTrackingConfidence.OnNext(confidence);
            }
        }
        
        /// <summary>
        /// Конвертирует TrackingConfidence в float.
        /// Convert TrackingConfidence to float.
        /// </summary>
        /// <param name="confidence">Уверенность отслеживания / Tracking confidence</param>
        /// <returns>Значение от 0 до 1 / Value from 0 to 1</returns>
        private float ConvertConfidenceToFloat(OVRHand.TrackingConfidence confidence)
        {
            switch (confidence)
            {
                case OVRHand.TrackingConfidence.Low:
                    return 0.3f;
                case OVRHand.TrackingConfidence.High:
                    return 1.0f;
                default:
                    return 0.0f;
            }
        }
        
        /// <summary>
        /// Обработка данных правой руки через OVR API.
        /// Process right hand data through OVR API.
        /// </summary>
        private void ProcessRightHandData()
        {
            try
            {
                Vector3 indexPosition = Vector3.zero;
                bool hasValidPosition = false;
                
                // Получаем позицию указательного пальца
                // Get index finger position
                if (rightOVRHand != null)
                {
                    // Используем OVRHand API
                    // Use OVRHand API
                    var indexTransform = rightOVRHand.PointerPose;
                    if (indexTransform != null)
                    {
                        indexPosition = indexTransform.position;
                        hasValidPosition = true;
                    }
                }
                else if (rightHandSkeleton != null && rightHandSkeleton.Bones != null)
                {
                    // Используем OVRSkeleton API
                    // Use OVRSkeleton API
                    var indexTipBone = GetBoneTransform(OVRSkeleton.BoneId.Hand_IndexTip);
                    if (indexTipBone != null)
                    {
                        indexPosition = indexTipBone.position;
                        hasValidPosition = true;
                    }
                }
                
                if (hasValidPosition)
                {
                    var confidence = GetCurrentConfidence();
                    
                    // Обновляем позицию указательного пальца
                    // Update index finger position
                    UpdateIndexFingerPosition(indexPosition);
                    
                    // Определяем состояние касания (pinch gesture)
                    // Determine touch state (pinch gesture)
                    var isTouching = DetectPinchGesture();
                    UpdateTouchState(isTouching);
                    
                    // Создаем данные взаимодействия
                    // Create interaction data
                    var interactionData = new HandInteractionData(
                        indexPosition,
                        isTouching,
                        confidence,
                        true // правая рука / right hand
                    );
                    
                    // Отправляем обновления через Observable
                    // Send updates through Observable
                    _uiInteraction.OnNext(interactionData);
                    _isRightHandTracked.OnNext(true);
                    _handTrackingConfidence.OnNext(confidence);
                    
                    _lastInteractionData = interactionData;
                }
            }
            catch (Exception ex)
            {
                if (debugOutput)
                {
                    Debug.LogError($"OpenXRHandTrackingService: Error processing hand data / Ошибка обработки данных руки: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Получение Transform кости по ID через OVRSkeleton.
        /// Get bone Transform by ID through OVRSkeleton.
        /// </summary>
        /// <param name="boneId">ID кости / Bone ID</param>
        /// <returns>Transform кости или null / Bone Transform or null</returns>
        private Transform GetBoneTransform(OVRSkeleton.BoneId boneId)
        {
            if (rightHandSkeleton?.Bones == null) return null;
            
            foreach (var bone in rightHandSkeleton.Bones)
            {
                if (bone.Id == boneId)
                {
                    return bone.Transform;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Расчет уверенности для OVRSkeleton.
        /// Calculate confidence for OVRSkeleton.
        /// </summary>
        /// <returns>Уровень уверенности / Confidence level</returns>
        private float CalculateSkeletonConfidence()
        {
            if (rightHandSkeleton?.Bones == null) return 0f;
            
            int validBones = 0;
            int totalBones = rightHandSkeleton.Bones.Count;
            
            foreach (var bone in rightHandSkeleton.Bones)
            {
                if (bone.Transform != null)
                {
                    validBones++;
                }
            }
            
            return totalBones > 0 ? (float)validBones / totalBones : 0f;
        }
        
        /// <summary>
        /// Получение текущей уверенности отслеживания.
        /// Get current tracking confidence.
        /// </summary>
        /// <returns>Уровень уверенности / Confidence level</returns>
        private float GetCurrentConfidence()
        {
            if (rightOVRHand != null)
            {
                return ConvertConfidenceToFloat(rightOVRHand.HandConfidence);
            }
            else
            {
                return CalculateSkeletonConfidence();
            }
        }
        
        /// <summary>
        /// Определение жеста pinch через OVR API.
        /// Detect pinch gesture through OVR API.
        /// </summary>
        /// <returns>True если жест активен / True if gesture is active</returns>
        private bool DetectPinchGesture()
        {
            if (rightOVRHand != null)
            {
                // Используем встроенное определение pinch в OVRHand
                // Use built-in pinch detection in OVRHand
                return rightOVRHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            }
            else if (rightHandSkeleton != null)
            {
                // Расчет расстояния между указательным и большим пальцем
                // Calculate distance between index and thumb finger
                var indexTip = GetBoneTransform(OVRSkeleton.BoneId.Hand_IndexTip);
                var thumbTip = GetBoneTransform(OVRSkeleton.BoneId.Hand_ThumbTip);
                
                if (indexTip != null && thumbTip != null)
                {
                    float distance = Vector3.Distance(indexTip.position, thumbTip.position);
                    return distance <= touchThreshold;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Обновление позиции указательного пальца с фильтрацией дрожания.
        /// Update index finger position with jitter filtering.
        /// </summary>
        /// <param name="newPosition">Новая позиция / New position</param>
        private void UpdateIndexFingerPosition(Vector3 newPosition)
        {
            // Простая фильтрация дрожания
            // Simple jitter filtering
            float distance = Vector3.Distance(_lastIndexPosition, newPosition);
            
            if (distance > 0.001f) // Минимальное расстояние для обновления / Minimum distance for update
            {
                _lastIndexPosition = newPosition;
                _indexFingerPosition.OnNext(newPosition);
            }
        }
        
        /// <summary>
        /// Обновление состояния касания с дебаунсингом.
        /// Update touch state with debouncing.
        /// </summary>
        /// <param name="isTouching">Новое состояние касания / New touch state</param>
        private void UpdateTouchState(bool isTouching)
        {
            if (_lastTouchState != isTouching)
            {
                _lastTouchState = isTouching;
                _isIndexFingerTouching.OnNext(isTouching);
                
                if (debugOutput)
                {
                    Debug.Log($"OpenXRHandTrackingService: Touch state changed to {isTouching} / Состояние касания изменено на {isTouching}");
                }
            }
        }
        
        /// <summary>
        /// Освобождение ресурсов.
        /// Release resources.
        /// </summary>
        public void Dispose()
        {
            _indexFingerPosition?.Dispose();
            _isIndexFingerTouching?.Dispose();
            _uiInteraction?.Dispose();
            _handTrackingConfidence?.Dispose();
            _isRightHandTracked?.Dispose();
            
            if (debugOutput)
            {
                Debug.Log("OpenXRHandTrackingService: Disposed / Освобожден");
            }
        }
        
        /// <summary>
        /// Очистка при уничтожении объекта.
        /// Cleanup on object destruction.
        /// </summary>
        private void OnDestroy()
        {
            Dispose();
        }
        
        /// <summary>
        /// Получение текущих данных взаимодействия для внешнего использования.
        /// Get current interaction data for external use.
        /// </summary>
        /// <returns>Последние данные взаимодействия / Latest interaction data</returns>
        public HandInteractionData GetCurrentInteractionData()
        {
            return _lastInteractionData;
        }
        
        /// <summary>
        /// Настройка параметров отслеживания во время выполнения.
        /// Configure tracking parameters at runtime.
        /// </summary>
        /// <param name="newConfidenceThreshold">Новый порог уверенности / New confidence threshold</param>
        /// <param name="newTouchThreshold">Новый порог касания / New touch threshold</param>
        public void UpdateTrackingSettings(float newConfidenceThreshold, float newTouchThreshold)
        {
            confidenceThreshold = Mathf.Clamp01(newConfidenceThreshold);
            touchThreshold = Mathf.Max(0.01f, newTouchThreshold);
            
            if (debugOutput)
            {
                Debug.Log($"OpenXRHandTrackingService: Settings updated - Confidence: {confidenceThreshold}, Touch: {touchThreshold}");
            }
        }
    }
}