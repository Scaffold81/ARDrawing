using UnityEngine;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using R3;
using System;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Симулятор Hand Tracking для тестирования без VR гарнитуры.
    /// Hand Tracking simulator for testing without VR headset.
    /// </summary>
    public class HandTrackingSimulator : MonoBehaviour, IHandTrackingService, IDisposable
    {
        [Header("Simulation Settings")]
        [SerializeField] private bool enableSimulation = true;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private KeyCode pinchKey = KeyCode.Space;
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        
        [Header("Visual Debug")]
        [SerializeField] private Transform fingerCursor;
        [SerializeField] private bool showDebugGUI = true;
        
        // R3 Observable потоки / R3 Observable streams
        private readonly Subject<Vector3> _indexFingerPosition = new();
        private readonly Subject<bool> _isIndexFingerTouching = new();
        private readonly Subject<HandInteractionData> _uiInteraction = new();
        private readonly Subject<float> _handTrackingConfidence = new();
        private readonly Subject<bool> _isRightHandTracked = new();
        
        // Состояние симуляции / Simulation state
        private Vector3 _currentFingerPosition = Vector3.zero;
        private bool _isPinching = false;
        private bool _wasMousePressed = false;
        private Camera _mainCamera;
        
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
            
            _mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            _currentFingerPosition = new Vector3(0, 1.5f, 1f);
            
            CreateFingerCursor();
            
            Debug.Log("HandTrackingSimulator: Initialized for Editor testing / Инициализирован для тестирования в Editor");
            Debug.Log("Controls: Mouse - move finger, Space/LMB - pinch, R - reset / Управление: Мышь - движение пальца, Space/ЛКМ - щипок, R - сброс");
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
            UpdatePinchState();
            SendObservableUpdates();
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
        /// Обновление состояния pinch жеста.
        /// Update pinch gesture state.
        /// </summary>
        private void UpdatePinchState()
        {
            bool currentPinching = Input.GetKey(pinchKey) || Input.GetMouseButton(0);
            
            if (currentPinching != _isPinching)
            {
                _isPinching = currentPinching;
                
                // Изменяем цвет курсора
                // Change cursor color
                if (fingerCursor != null)
                {
                    var renderer = fingerCursor.GetComponent<Renderer>();
                    renderer.material.color = _isPinching ? Color.red : Color.green;
                }
                
                Debug.Log($"HandTrackingSimulator: Pinch {(_isPinching ? "started" : "ended")} / Щипок {(_isPinching ? "начат" : "закончен")}");
            }
        }
        
        /// <summary>
        /// Отправка обновлений через Observable потоки.
        /// Send updates through Observable streams.
        /// </summary>
        private void SendObservableUpdates()
        {
            // Отправляем данные через R3 Observable
            // Send data through R3 Observable
            _indexFingerPosition.OnNext(_currentFingerPosition);
            _isIndexFingerTouching.OnNext(_isPinching);
            _handTrackingConfidence.OnNext(1.0f); // Полная уверенность в симуляции / Full confidence in simulation
            _isRightHandTracked.OnNext(true);
            
            // Создаем данные взаимодействия
            // Create interaction data
            var interactionData = new HandInteractionData(
                _currentFingerPosition,
                _isPinching,
                1.0f,
                true
            );
            
            _uiInteraction.OnNext(interactionData);
        }
        
        /// <summary>
        /// Отображение GUI с инструкциями.
        /// Display GUI with instructions.
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugGUI || !enableSimulation) return;
            
            GUI.color = Color.white;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            
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
            GUILayout.Label($"Pinching / Щипок: {(_isPinching ? "Yes / Да" : "No / Нет")}");
            
            GUILayout.Space(10);
            if (GUILayout.Button("Reset Position / Сброс позиции"))
            {
                _currentFingerPosition = new Vector3(0, 1.5f, 1f);
            }
            
            GUILayout.EndArea();
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