using UnityEngine;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using ARDrawing.Core.Services;
using R3;
using System;

namespace ARDrawing.Testing
{
    /// <summary>
    /// Симулятор Hand Tracking для тестирования без VR гарнитуры с улучшенным определением касания.
    /// Hand Tracking simulator for testing without VR headset with improved touch detection.
    /// </summary>
    public class HandTrackingSimulator : MonoBehaviour, IHandTrackingService, IDisposable
    {
        [Header("Simulation Settings")]
        [SerializeField] private bool enableSimulation = true;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private KeyCode pinchKey = KeyCode.Space;
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        
        [Header("Touch Detection")]
        [SerializeField] private TouchDetectionSettings touchSettings = TouchDetectionSettings.Default;
        
        [Header("Visual Debug")]
        [SerializeField] private Transform fingerCursor;
        [SerializeField] private bool showDebugGUI = true;
        
        // R3 Observable потоки / R3 Observable streams
        private readonly Subject<Vector3> _indexFingerPosition = new();
        private readonly Subject<bool> _isIndexFingerTouching = new();
        private readonly Subject<HandInteractionData> _uiInteraction = new();
        private readonly Subject<float> _handTrackingConfidence = new();
        private readonly Subject<bool> _isRightHandTracked = new();
        
        // Touch State Manager / Менеджер состояний касания
        private TouchStateManager _touchStateManager;
        private IDisposable _touchEventsSubscription;
        private IDisposable _touchStateSubscription;
        
        // Состояние симуляции / Simulation state
        private Vector3 _currentFingerPosition = Vector3.zero;
        private Vector3 _thumbPosition = Vector3.zero; // Виртуальная позиция большого пальца
        private bool _isPinching = false;
        private Camera _mainCamera;
        private TouchState _currentTouchState = TouchState.None;
        
        public Observable<Vector3> IndexFingerPosition => _indexFingerPosition.AsObservable();
        public Observable<bool> IsIndexFingerTouching => _isIndexFingerTouching.AsObservable();
        public Observable<HandInteractionData> UIInteraction => _uiInteraction.AsObservable();
        public Observable<float> HandTrackingConfidence => _handTrackingConfidence.AsObservable();
        public Observable<bool> IsRightHandTracked => _isRightHandTracked.AsObservable();
        
        /// <summary>
        /// Инициализация симулятора.
        /// Initialize simulator.
        /// </summary>
        private void Start()
        {
            if (!enableSimulation) return;
            
            _mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            _currentFingerPosition = new Vector3(0, 1.5f, 1f);
            _thumbPosition = _currentFingerPosition + Vector3.right * 0.05f; // Большой палец рядом
            
            CreateFingerCursor();
            InitializeTouchStateManager();
            
            Debug.Log("HandTrackingSimulator: Initialized with TouchStateManager / Инициализирован с TouchStateManager");
            Debug.Log("Controls: Mouse - move finger, Space/LMB - pinch, R - reset / Управление: Мышь - движение пальца, Space/ЛКМ - щипок, R - сброс");
        }
        
        /// <summary>
        /// Инициализация TouchStateManager для улучшенного определения касания.
        /// Initialize TouchStateManager for improved touch detection.
        /// </summary>
        private void InitializeTouchStateManager()
        {
            _touchStateManager = new TouchStateManager(touchSettings);
            
            // Подписка на события касания
            // Subscribe to touch events
            _touchEventsSubscription = _touchStateManager.TouchEvents
                .Subscribe(OnTouchEvent);
                
            _touchStateSubscription = _touchStateManager.TouchStateChanged
                .Subscribe(OnTouchStateChanged);
        }
        
        /// <summary>
        /// Обработка события касания от TouchStateManager.
        /// Handle touch event from TouchStateManager.
        /// </summary>
        /// <param name="touchEvent">Данные события / Event data</param>
        private void OnTouchEvent(TouchEventData touchEvent)
        {
            // Обновляем Observable потоки
            // Update Observable streams
            _indexFingerPosition.OnNext(touchEvent.position);
            
            bool isTouching = touchEvent.state == TouchState.Started || touchEvent.state == TouchState.Active;
            _isIndexFingerTouching.OnNext(isTouching);
            
            // Создаем данные взаимодействия
            // Create interaction data
            var interactionData = new HandInteractionData(
                touchEvent.position,
                isTouching,
                touchEvent.confidence,
                true
            );
            
            _uiInteraction.OnNext(interactionData);
        }
        
        /// <summary>
        /// Обработка изменения состояния касания.
        /// Handle touch state change.
        /// </summary>
        /// <param name="newState">Новое состояние / New state</param>
        private void OnTouchStateChanged(TouchState newState)
        {
            _currentTouchState = newState;
            
            // Обновляем цвет курсора в зависимости от состояния
            // Update cursor color based on state
            if (fingerCursor != null)
            {
                var renderer = fingerCursor.GetComponent<Renderer>();
                if (renderer != null)
                {
                    switch (newState)
                    {
                        case TouchState.None:
                            renderer.material.color = Color.green;
                            break;
                        case TouchState.Started:
                            renderer.material.color = Color.yellow;
                            break;
                        case TouchState.Active:
                            renderer.material.color = Color.red;
                            break;
                        case TouchState.Ended:
                            renderer.material.color = new Color(1f, 0.5f, 0f); // Orange
                            break;
                    }
                }
            }
            
            Debug.Log($"HandTrackingSimulator: Touch state changed to {newState} / Состояние касания изменено на {newState}");
        }
        
        /// <summary>
        /// Создание визуального курсора для пальца.
        /// Create visual cursor for finger.
        /// </summary>
        private void CreateFingerCursor()
        {
            if (fingerCursor == null)
            {
                var cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cursor.name = "FingerCursor";
                cursor.transform.localScale = Vector3.one * 0.02f;
                cursor.GetComponent<Renderer>().material.color = Color.green;
                
                // Убираем коллайдер
                // Remove collider
                DestroyImmediate(cursor.GetComponent<Collider>());
                
                fingerCursor = cursor.transform;
            }
        }
        
        /// <summary>
        /// Обновление симуляции каждый кадр.
        /// Update simulation every frame.
        /// </summary>
        private void Update()
        {
            if (!enableSimulation) return;
            
            UpdateMouseInput();
            UpdateKeyboardInput();
            UpdateFingerPosition();
            UpdatePinchSimulation();
            SendBasicObservableUpdates();
        }
        
        /// <summary>
        /// Обработка ввода мыши для движения пальца.
        /// Handle mouse input for finger movement.
        /// </summary>
        private void UpdateMouseInput()
        {
            if (_mainCamera == null) return;
            
            // Конвертируем позицию мыши в мировые координаты
            // Convert mouse position to world coordinates
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 1f; // Расстояние от камеры / Distance from camera
            
            Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);
            
            // Плавное движение к позиции мыши
            // Smooth movement to mouse position
            _currentFingerPosition = Vector3.Lerp(
                _currentFingerPosition, 
                worldPosition, 
                moveSpeed * Time.deltaTime
            );
            
            // Обновляем позицию большого пальца относительно указательного
            // Update thumb position relative to index finger
            UpdateThumbPosition();
        }
        
        /// <summary>
        /// Обновление позиции большого пальца для симуляции pinch.
        /// Update thumb position for pinch simulation.
        /// </summary>
        private void UpdateThumbPosition()
        {
            // Если pinch активен, приближаем большой палец к указательному
            // If pinch is active, move thumb closer to index finger
            Vector3 targetThumbOffset = _isPinching 
                ? Vector3.right * 0.01f  // Близко для pinch / Close for pinch
                : Vector3.right * 0.05f; // Далеко в обычном состоянии / Far in normal state
                
            _thumbPosition = _currentFingerPosition + targetThumbOffset;
        }
        
        /// <summary>
        /// Обработка клавиатурного ввода.
        /// Handle keyboard input.
        /// </summary>
        private void UpdateKeyboardInput()
        {
            // Управление движением стрелками
            // Arrow key movement control
            Vector3 movement = Vector3.zero;
            
            if (Input.GetKey(KeyCode.LeftArrow))
                movement.x -= moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow))
                movement.x += moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.UpArrow))
                movement.y += moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow))
                movement.y -= moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.PageUp))
                movement.z += moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.PageDown))
                movement.z -= moveSpeed * Time.deltaTime;
                
            _currentFingerPosition += movement;
            
            // Сброс позиции
            // Reset position
            if (Input.GetKeyDown(resetKey))
            {
                _currentFingerPosition = new Vector3(0, 1.5f, 1f);
                Debug.Log("HandTrackingSimulator: Position reset / Позиция сброшена");
            }
        }
        
        /// <summary>
        /// Обновление позиции пальца.
        /// Update finger position.
        /// </summary>
        private void UpdateFingerPosition()
        {
            if (fingerCursor != null)
            {
                fingerCursor.position = _currentFingerPosition;
            }
        }
        
        /// <summary>
        /// Симуляция pinch жеста через TouchStateManager.
        /// Simulate pinch gesture through TouchStateManager.
        /// </summary>
        private void UpdatePinchSimulation()
        {
            bool currentPinching = Input.GetKey(pinchKey) || Input.GetMouseButton(0);
            
            if (currentPinching != _isPinching)
            {
                _isPinching = currentPinching;
                UpdateThumbPosition(); // Обновляем позицию большого пальца
            }
            
            // Обновляем TouchStateManager с текущими позициями
            // Update TouchStateManager with current positions
            if (_touchStateManager != null)
            {
                _touchStateManager.UpdateTouchState(_currentFingerPosition, _thumbPosition, 1.0f);
            }
        }
        
        /// <summary>
        /// Отправка базовых обновлений через Observable потоки.
        /// Send basic updates through Observable streams.
        /// </summary>
        private void SendBasicObservableUpdates()
        {
            // Отправляем базовые данные
            // Send basic data
            _handTrackingConfidence.OnNext(1.0f); // Полная уверенность в симуляции / Full confidence in simulation
            _isRightHandTracked.OnNext(true);
        }
        
        /// <summary>
        /// Отображение GUI с инструкциями и состоянием.
        /// Display GUI with instructions and state.
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugGUI || !enableSimulation) return;
            
            GUI.color = Color.white;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 350));
            
            GUILayout.Label("Hand Tracking Simulator / Симулятор отслеживания рук", GUI.skin.box);
            GUILayout.Space(10);
            
            GUILayout.Label("Controls / Управление:", GUI.skin.label);
            GUILayout.Label("• Mouse - Move finger / Мышь - движение пальца");
            GUILayout.Label("• Space/LMB - Pinch gesture / Space/ЛКМ - жест щипок");
            GUILayout.Label("• Arrow Keys - Precise movement / Стрелки - точное движение");
            GUILayout.Label("• Page Up/Down - Z movement / Page Up/Down - движение по Z");
            GUILayout.Label("• R - Reset position / R - сброс позиции");
            
            GUILayout.Space(10);
            GUILayout.Label($"Position / Позиция: {_currentFingerPosition}");
            GUILayout.Label($"Touch State / Состояние касания: {_currentTouchState}");
            GUILayout.Label($"Pinching / Щипок: {(_isPinching ? "Yes / Да" : "No / Нет")}");
            
            GUILayout.Space(10);
            
            // Настройки TouchStateManager
            // TouchStateManager settings
            GUILayout.Label("Touch Detection Settings:", GUI.skin.label);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Threshold: {touchSettings.pinchThreshold:F3}");
            touchSettings.pinchThreshold = GUILayout.HorizontalSlider(touchSettings.pinchThreshold, 0.01f, 0.1f, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Hysteresis: {touchSettings.hysteresis:F3}");
            touchSettings.hysteresis = GUILayout.HorizontalSlider(touchSettings.hysteresis, 0.005f, 0.05f, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Update Settings / Обновить настройки"))
            {
                _touchStateManager?.UpdateSettings(touchSettings);
            }
            
            if (GUILayout.Button("Reset Position / Сброс позиции"))
            {
                _currentFingerPosition = new Vector3(0, 1.5f, 1f);
            }
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Получить текущую позицию указательного пальца.
        /// Get current index finger position.
        /// </summary>
        /// <returns>Текущая позиция / Current position</returns>
        public Vector3 GetCurrentIndexFingerPosition()
        {
            return _currentFingerPosition;
        }
        
        /// <summary>
        /// Освобождение ресурсов.
        /// Release resources.
        /// </summary>
        public void Dispose()
        {
            _touchEventsSubscription?.Dispose();
            _touchStateSubscription?.Dispose();
            _touchStateManager?.Dispose();
            
            _indexFingerPosition?.Dispose();
            _isIndexFingerTouching?.Dispose();
            _uiInteraction?.Dispose();
            _handTrackingConfidence?.Dispose();
            _isRightHandTracked?.Dispose();
            
            Debug.Log("HandTrackingSimulator: Disposed / Освобожден");
        }
        
        /// <summary>
        /// Очистка при уничтожении.
        /// Cleanup on destruction.
        /// </summary>
        private void OnDestroy()
        {
            Dispose();
        }
    }
}