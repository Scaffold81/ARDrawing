using UnityEngine;
using R3;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using System;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Реализация сервиса отслеживания рук через Meta XR API с улучшенным определением касания.
    /// Meta XR Hand Tracking service implementation with improved touch detection.
    /// </summary>
    public class OpenXRHandTrackingService : MonoBehaviour, IHandTrackingService, IDisposable
    {
        [Header("Hand Tracking Settings")]
        [SerializeField] private float confidenceThreshold = 0.7f;
        [SerializeField] private bool debugOutput = true;
        
        [Header("Touch Detection Settings")]
        [SerializeField] private TouchDetectionSettings touchSettings = TouchDetectionSettings.Default;
        
        [Header("OVR Hand References")]
        [SerializeField] private OVRHand rightOVRHand;
        [SerializeField] private OVRSkeleton rightHandSkeleton;
        
        // R3 Observable потоки / R3 Observable streams
        private Subject<Vector3> _indexFingerPosition;
        private Subject<bool> _isIndexFingerTouching;
        private Subject<HandInteractionData> _uiInteraction;
        private Subject<float> _handTrackingConfidence;
        private Subject<bool> _isRightHandTracked;
        private Subject<TouchEventData> _touchEvents;
        
        // Disposal tracking
        private bool _isDisposed = false;
        
        // Touch State Manager / Менеджер состояний касания
        private TouchStateManager _touchStateManager;
        private IDisposable _touchEventsSubscription;
        private IDisposable _touchStateSubscription;
        
        // Состояние отслеживания / Tracking state
        private bool _isInitialized = false;
        private HandInteractionData _lastInteractionData;
        
        // Кэш для оптимизации / Cache for optimization
        private Vector3 _lastIndexPosition = Vector3.zero;
        private Vector3 _lastThumbPosition = Vector3.zero;
        private bool _lastTouchState = false;
        private float _lastConfidence = 0f;
        
        public Observable<Vector3> IndexFingerPosition => _indexFingerPosition?.AsObservable() ?? Observable.Empty<Vector3>();
        public Observable<bool> IsIndexFingerTouching => _isIndexFingerTouching?.AsObservable() ?? Observable.Empty<bool>();
        public Observable<HandInteractionData> UIInteraction => _uiInteraction?.AsObservable() ?? Observable.Empty<HandInteractionData>();
        public Observable<float> HandTrackingConfidence => _handTrackingConfidence?.AsObservable() ?? Observable.Empty<float>();
        public Observable<bool> IsRightHandTracked => _isRightHandTracked?.AsObservable() ?? Observable.Empty<bool>();
        
        /// <summary>
        /// Дополнительный Observable для детальных событий касания.
        /// Additional Observable for detailed touch events.
        /// </summary>
        public Observable<TouchEventData> TouchEvents => _touchEvents?.AsObservable() ?? Observable.Empty<TouchEventData>();
        
        /// <summary>
        /// Инициализация сервиса отслеживания рук.
        /// Initialize hand tracking service.
        /// </summary>
        private void Start()
        {
            InitializeObservables();
            InitializeHandTracking();
            InitializeTouchStateManager();
        }
        
        /// <summary>
        /// Инициализация Observable потоков.
        /// Initialize Observable streams.
        /// </summary>
        private void InitializeObservables()
        {
            if (_isDisposed) return;
            
            _indexFingerPosition = new Subject<Vector3>();
            _isIndexFingerTouching = new Subject<bool>();
            _uiInteraction = new Subject<HandInteractionData>();
            _handTrackingConfidence = new Subject<float>();
            _isRightHandTracked = new Subject<bool>();
            _touchEvents = new Subject<TouchEventData>();
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
        /// Инициализация менеджера состояний касания.
        /// Initialize touch state manager.
        /// </summary>
        private void InitializeTouchStateManager()
        {
            _touchStateManager = new TouchStateManager(touchSettings);
            
            // Subscribe to touch events
            _touchEventsSubscription = _touchStateManager.TouchEvents
                .Subscribe(OnTouchEvent);
                
            _touchStateSubscription = _touchStateManager.TouchStateChanged
                .Subscribe(OnTouchStateChanged);
            
            if (debugOutput)
            {
                Debug.Log("OpenXRHandTrackingService: TouchStateManager initialized with improved detection");
            }
        }
        
        /// <summary>
        /// Обработка события касания.
        /// Handle touch event.
        /// </summary>
        /// <param name="touchEvent">Данные события / Event data</param>
        private void OnTouchEvent(TouchEventData touchEvent)
        {
            if (_isDisposed) return;
            // Обновляем Observable потоки
            // Update Observable streams
            _indexFingerPosition.OnNext(touchEvent.position);
            
            // Определяем состояние касания
            // Determine touch state
            bool isTouching = touchEvent.state == TouchState.Started || touchEvent.state == TouchState.Active;
            
            if (_lastTouchState != isTouching)
            {
                _lastTouchState = isTouching;
                _isIndexFingerTouching.OnNext(isTouching);
                
                if (debugOutput)
                {
                    Debug.Log($"OpenXRHandTrackingService: Touch state changed to {touchEvent.state} / Состояние касания изменено на {touchEvent.state}");
                }
            }
            
            // Создаем данные взаимодействия с дополнительной информацией
            // Create interaction data with additional information
            var interactionData = new HandInteractionData(
                touchEvent.position,
                isTouching,
                touchEvent.confidence,
                true // правая рука / right hand
            );
            
            _uiInteraction.OnNext(interactionData);
            _lastInteractionData = interactionData;
            
            // Отправляем детальное событие касания
            // Send detailed touch event
            _touchEvents?.OnNext(touchEvent);
        }
        
        /// <summary>
        /// Обработка изменения состояния касания.
        /// Handle touch state change.
        /// </summary>
        /// <param name="newState">Новое состояние / New state</param>
        private void OnTouchStateChanged(TouchState newState)
        {
            if (debugOutput)
            {
                Debug.Log($"OpenXRHandTrackingService: Touch state transition to {newState} / Переход состояния касания в {newState}");
            }
        }
        
        /// <summary>
        /// Обновление данных отслеживания рук каждый кадр.
        /// Update hand tracking data every frame.
        /// </summary>
        private void Update()
        {
            if (!_isInitialized || _touchStateManager == null)
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
                
                // Сбрасываем состояние касания при потере отслеживания
                // Reset touch state when tracking is lost
                if (_touchStateManager != null)
                {
                    _touchStateManager.ResetState();
                }
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
                Vector3 thumbPosition = Vector3.zero;
                bool hasValidPositions = false;
                
                // Получаем позицию указательного и большого пальца
                // Get index and thumb finger positions
                if (rightOVRHand != null)
                {
                    // Используем OVRHand API
                    // Use OVRHand API
                    var indexTransform = rightOVRHand.PointerPose;
                    if (indexTransform != null)
                    {
                        indexPosition = indexTransform.position;
                        
                        // Пробуем получить позицию большого пальца через скелет
                        // Try to get thumb position through skeleton
                        if (rightHandSkeleton != null)
                        {
                            var thumbTipBone = GetBoneTransform(OVRSkeleton.BoneId.Hand_ThumbTip);
                            if (thumbTipBone != null)
                            {
                                thumbPosition = thumbTipBone.position;
                                hasValidPositions = true;
                            }
                        }
                    }
                }
                else if (rightHandSkeleton != null && rightHandSkeleton.Bones != null)
                {
                    // Используем OVRSkeleton API
                    // Use OVRSkeleton API
                    var indexTipBone = GetBoneTransform(OVRSkeleton.BoneId.Hand_IndexTip);
                    var thumbTipBone = GetBoneTransform(OVRSkeleton.BoneId.Hand_ThumbTip);
                    
                    if (indexTipBone != null && thumbTipBone != null)
                    {
                        indexPosition = indexTipBone.position;
                        thumbPosition = thumbTipBone.position;
                        hasValidPositions = true;
                    }
                }
                
                if (hasValidPositions)
                {
                    var confidence = GetCurrentConfidence();
                    
                    // Обновляем TouchStateManager с новыми данными
                    // Update TouchStateManager with new data
                    var touchEvent = _touchStateManager.UpdateTouchState(indexPosition, thumbPosition, confidence);
                    
                    // Обновляем базовые Observable потоки
                    // Update basic Observable streams
                    _isRightHandTracked.OnNext(true);
                    _handTrackingConfidence.OnNext(confidence);
                    
                    // Кэшируем позиции
                    // Cache positions
                    _lastIndexPosition = indexPosition;
                    _lastThumbPosition = thumbPosition;
                    _lastConfidence = confidence;
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
        /// Получить текущую позицию указательного пальца.
        /// Get current index finger position.
        /// </summary>
        /// <returns>Текущая позиция / Current position</returns>
        public Vector3 GetCurrentIndexFingerPosition()
        {
            return _lastIndexPosition;
        }
        
        /// <summary>
        /// Обновление настроек определения касания во время выполнения.
        /// Update touch detection settings at runtime.
        /// </summary>
        /// <param name="newSettings">Новые настройки / New settings</param>
        public void UpdateTouchSettings(TouchDetectionSettings newSettings)
        {
            touchSettings = newSettings;
            _touchStateManager?.UpdateSettings(newSettings);
            
            if (debugOutput)
            {
                Debug.Log($"OpenXRHandTrackingService: Touch settings updated - Threshold: {newSettings.pinchThreshold}");
            }
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
        /// Получение текущего состояния касания.
        /// Get current touch state.
        /// </summary>
        /// <returns>Текущее состояние / Current state</returns>
        public TouchState GetCurrentTouchState()
        {
            return _touchStateManager?.CurrentState ?? TouchState.None;
        }
        
        /// <summary>
        /// Освобождение ресурсов.
        /// Release resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            // Dispose subscriptions first
            try
            {
                _touchEventsSubscription?.Dispose();
                _touchEventsSubscription = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disposing touch events subscription: {ex.Message}");
            }
            
            try
            {
                _touchStateSubscription?.Dispose();
                _touchStateSubscription = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disposing touch state subscription: {ex.Message}");
            }
            
            // Dispose TouchStateManager
            try
            {
                _touchStateManager?.Dispose();
                _touchStateManager = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disposing touch state manager: {ex.Message}");
            }
            
            // Dispose Observable subjects
            DisposeSubject(ref _indexFingerPosition, "IndexFingerPosition");
            DisposeSubject(ref _isIndexFingerTouching, "IsIndexFingerTouching");
            DisposeSubject(ref _uiInteraction, "UIInteraction");
            DisposeSubject(ref _handTrackingConfidence, "HandTrackingConfidence");
            DisposeSubject(ref _isRightHandTracked, "IsRightHandTracked");
            DisposeSubject(ref _touchEvents, "TouchEvents");
            
            if (debugOutput)
            {
                Debug.Log("OpenXRHandTrackingService: Disposed safely");
            }
        }
        
        /// <summary>
        /// Безопасное освобождение Subject.
        /// Safe disposal of Subject.
        /// </summary>
        private void DisposeSubject<T>(ref Subject<T> subject, string name)
        {
            if (subject != null)
            {
                try
                {
                    if (!subject.IsDisposed)
                    {
                        subject.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error disposing {name} subject: {ex.Message}");
                }
                finally
                {
                    subject = null;
                }
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
    }
}