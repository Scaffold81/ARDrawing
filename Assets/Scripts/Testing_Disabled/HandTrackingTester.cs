using UnityEngine;
using Zenject;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using R3;
using System;

namespace ARDrawing.Testing
{
    /// <summary>
    /// Компонент для тестирования OpenXR Hand Tracking в реальном времени.
    /// Component for testing OpenXR Hand Tracking in real-time.
    /// </summary>
    public class HandTrackingTester : MonoBehaviour, IDisposable
    {
        [Header("Visual Debug")]
        [SerializeField] private GameObject fingerPositionCube;
        [SerializeField] private Material touchingMaterial;
        [SerializeField] private Material notTouchingMaterial;
        [SerializeField] private bool enableDebugOutput = true;
        
        [Header("Test Objects")]
        [SerializeField] private Transform testCube;
        
        // Dependency Injection
        [Inject] private IHandTrackingService _handTrackingService;
        
        // R3 подписки / R3 subscriptions
        private CompositeDisposable _subscriptions = new();
        
        // Визуальные компоненты / Visual components
        private Renderer _cubeRenderer;
        private Vector3 _lastFingerPosition;
        private bool _isTracking = false;
        
        /// <summary>
        /// Инициализация тестера после DI инъекции.
        /// Initialize tester after DI injection.
        /// </summary>
        private void Start()
        {
            SetupVisualComponents();
            SubscribeToHandTracking();
            CreateTestObjects();
        }
        
        /// <summary>
        /// Настройка визуальных компонентов для отладки.
        /// Setup visual components for debugging.
        /// </summary>
        private void SetupVisualComponents()
        {
            // Создаем куб для отображения позиции пальца
            // Create cube to display finger position
            if (fingerPositionCube == null)
            {
                fingerPositionCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fingerPositionCube.name = "FingerPositionCube";
                fingerPositionCube.transform.localScale = Vector3.one * 0.02f;
                
                // Убираем коллайдер
                // Remove collider
                var collider = fingerPositionCube.GetComponent<Collider>();
                if (collider != null)
                    DestroyImmediate(collider);
            }
            
            _cubeRenderer = fingerPositionCube.GetComponent<Renderer>();
            
            // Создаем материалы если не назначены
            // Create materials if not assigned
            if (notTouchingMaterial == null)
            {
                notTouchingMaterial = new Material(Shader.Find("Standard"));
                notTouchingMaterial.color = Color.green;
            }
            
            if (touchingMaterial == null)
            {
                touchingMaterial = new Material(Shader.Find("Standard"));
                touchingMaterial.color = Color.red;
            }
            
            _cubeRenderer.material = notTouchingMaterial;
        }
        
        /// <summary>
        /// Подписка на события Hand Tracking через R3.
        /// Subscribe to Hand Tracking events through R3.
        /// </summary>
        private void SubscribeToHandTracking()
        {
            if (_handTrackingService == null)
            {
                Debug.LogError("HandTrackingTester: IHandTrackingService not injected / Сервис отслеживания рук не инъектирован");
                return;
            }
            
            // Подписка на позицию указательного пальца
            // Subscribe to index finger position
            _handTrackingService.IndexFingerPosition
                .Subscribe(OnFingerPositionChanged)
                .AddTo(_subscriptions);
            
            // Подписка на состояние касания
            // Subscribe to touch state
            _handTrackingService.IsIndexFingerTouching
                .Subscribe(OnTouchStateChanged)
                .AddTo(_subscriptions);
            
            // Подписка на уверенность отслеживания
            // Subscribe to tracking confidence
            _handTrackingService.HandTrackingConfidence
                .Subscribe(OnConfidenceChanged)
                .AddTo(_subscriptions);
            
            // Подписка на состояние отслеживания руки
            // Subscribe to hand tracking state
            _handTrackingService.IsRightHandTracked
                .Subscribe(OnHandTrackingStateChanged)
                .AddTo(_subscriptions);
            
            // Подписка на данные UI взаимодействия
            // Subscribe to UI interaction data
            _handTrackingService.UIInteraction
                .Subscribe(OnUIInteractionChanged)
                .AddTo(_subscriptions);
                
            if (enableDebugOutput)
            {
                Debug.Log("HandTrackingTester: Subscribed to hand tracking events / Подписался на события отслеживания рук");
            }
        }
        
        /// <summary>
        /// Создание тестовых объектов для взаимодействия.
        /// Create test objects for interaction.
        /// </summary>
        private void CreateTestObjects()
        {
            if (testCube == null)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "TestCube";
                cube.transform.position = new Vector3(0, 1.5f, 1f);
                cube.transform.localScale = Vector3.one * 0.1f;
                cube.GetComponent<Renderer>().material.color = Color.cyan;
                
                testCube = cube.transform;
            }
        }
        
        /// <summary>
        /// Обработка изменения позиции указательного пальца.
        /// Handle index finger position change.
        /// </summary>
        /// <param name="position">Новая позиция / New position</param>
        private void OnFingerPositionChanged(Vector3 position)
        {
            _lastFingerPosition = position;
            
            if (fingerPositionCube != null)
            {
                fingerPositionCube.transform.position = position;
            }
        }
        
        /// <summary>
        /// Обработка изменения состояния касания.
        /// Handle touch state change.
        /// </summary>
        /// <param name="isTouching">Состояние касания / Touch state</param>
        private void OnTouchStateChanged(bool isTouching)
        {
            if (_cubeRenderer != null)
            {
                _cubeRenderer.material = isTouching ? touchingMaterial : notTouchingMaterial;
            }
            
            if (enableDebugOutput)
            {
                Debug.Log($"HandTrackingTester: Touch state: {isTouching} / Состояние касания: {isTouching}");
            }
            
            // Тестируем взаимодействие с тестовым кубом
            // Test interaction with test cube
            if (isTouching && testCube != null)
            {
                float distance = Vector3.Distance(_lastFingerPosition, testCube.position);
                if (distance < 0.1f)
                {
                    testCube.GetComponent<Renderer>().material.color = Color.yellow;
                    Debug.Log("HandTrackingTester: Touching test cube! / Касание тестового куба!");
                }
            }
            else if (testCube != null)
            {
                testCube.GetComponent<Renderer>().material.color = Color.cyan;
            }
        }
        
        /// <summary>
        /// Обработка изменения уверенности отслеживания.
        /// Handle tracking confidence change.
        /// </summary>
        /// <param name="confidence">Уровень уверенности / Confidence level</param>
        private void OnConfidenceChanged(float confidence)
        {
            if (enableDebugOutput && confidence > 0.5f)
            {
                Debug.Log($"HandTrackingTester: Confidence: {confidence:F2} / Уверенность: {confidence:F2}");
            }
        }
        
        /// <summary>
        /// Обработка изменения состояния отслеживания руки.
        /// Handle hand tracking state change.
        /// </summary>
        /// <param name="isTracked">Отслеживается ли рука / Is hand tracked</param>
        private void OnHandTrackingStateChanged(bool isTracked)
        {
            _isTracking = isTracked;
            
            if (fingerPositionCube != null)
            {
                fingerPositionCube.SetActive(isTracked);
            }
            
            if (enableDebugOutput)
            {
                Debug.Log($"HandTrackingTester: Hand tracking: {isTracked} / Отслеживание руки: {isTracked}");
            }
        }
        
        /// <summary>
        /// Обработка данных UI взаимодействия.
        /// Handle UI interaction data.
        /// </summary>
        /// <param name="interactionData">Данные взаимодействия / Interaction data</param>
        private void OnUIInteractionChanged(HandInteractionData interactionData)
        {
            // Периодически выводим данные взаимодействия
            // Periodically output interaction data
            if (enableDebugOutput && interactionData.isTouching && Time.time % 1f < 0.1f)
            {
                Debug.Log($"HandTrackingTester: UI Interaction - Confidence: {interactionData.confidence:F2}");
            }
        }
        
        /// <summary>
        /// Отображение текущего статуса в GUI.
        /// Display current status in GUI.
        /// </summary>
        private void OnGUI()
        {
            GUI.color = Color.white;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            
            GUILayout.Label("Hand Tracking Test / Тест отслеживания рук", GUI.skin.box);
            GUILayout.Label($"Tracking: {(_isTracking ? "✓" : "✗")} / Отслеживание: {(_isTracking ? "Да" : "Нет")}");
            GUILayout.Label($"Position / Позиция: {_lastFingerPosition}");
            
            if (_handTrackingService != null)
            {
                GUILayout.Label("Service Injected ✓ / Сервис инъектирован ✓");
            }
            else
            {
                GUILayout.Label("Service NOT Injected ✗ / Сервис НЕ инъектирован ✗");
            }
            
            GUILayout.Label("Controls / Управление:");
            GUILayout.Label("- Make pinch gesture to test touch / Сделайте жест щипок для теста касания");
            GUILayout.Label("- Green cube = finger position / Зеленый куб = позиция пальца");
            GUILayout.Label("- Red when touching / Красный при касании");
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Освобождение ресурсов.
        /// Release resources.
        /// </summary>
        public void Dispose()
        {
            _subscriptions?.Dispose();
            
            if (enableDebugOutput)
            {
                Debug.Log("HandTrackingTester: Disposed / Освобожден");
            }
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