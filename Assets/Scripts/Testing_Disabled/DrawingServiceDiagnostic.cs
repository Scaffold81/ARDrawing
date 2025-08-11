using UnityEngine;
using ARDrawing.Core.Services;
using ARDrawing.Core.Models;

namespace ARDrawing.Testing
{
    /// <summary>
    /// Диагностический компонент для отладки проблем с DrawingService.
    /// Diagnostic component for debugging DrawingService issues.
    /// </summary>
    public class DrawingServiceDiagnostic : MonoBehaviour
    {
        [Header("Diagnostic Settings")]
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private Material testMaterial;
        [SerializeField] private Transform cameraTransform;
        
        [Header("Test Line Settings")]
        [SerializeField] private Vector3 testStartPosition = new Vector3(0, 1, 2);
        [SerializeField] private Color testLineColor = Color.red;
        [SerializeField] private float testLineWidth = 0.05f;
        
        private DrawingService drawingService;
        
        private void Start()
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main?.transform;
                
            RunDiagnostic();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                RunDiagnostic();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                CreateTestLineManual();
            }
            
            if (Input.GetKeyDown(KeyCode.G))
            {
                CreateTestLineInFrontOfCamera();
            }
        }
        
        [ContextMenu("Run Full Diagnostic")]
        public void RunDiagnostic()
        {
            Debug.Log("=== DRAWING SERVICE DIAGNOSTIC START ===");
            
            // 1. Проверка DrawingService
            CheckDrawingService();
            
            // 2. Проверка материала
            CheckMaterial();
            
            // 3. Проверка настроек
            CheckSettings();
            
            // 4. Проверка камеры
            CheckCamera();
            
            // 5. Создание тестовой линии
            CreateDiagnosticLine();
            
            Debug.Log("=== DRAWING SERVICE DIAGNOSTIC END ===");
        }
        
        private void CheckDrawingService()
        {
            Debug.Log("1. Checking DrawingService...");
            
            drawingService = FindFirstObjectByType<DrawingService>();
            if (drawingService == null)
            {
                Debug.LogError("❌ DrawingService not found in scene!");
                
                // Создаем DrawingService
                var serviceObject = new GameObject("DrawingService");
                drawingService = serviceObject.AddComponent<DrawingService>();
                Debug.Log("✅ Created new DrawingService");
            }
            else
            {
                Debug.Log("✅ DrawingService found");
            }
            
            // Проверяем состояние
            if (drawingService != null)
            {
                Debug.Log($"   - DrawingService active: {drawingService.gameObject.activeInHierarchy}");
                Debug.Log($"   - Component enabled: {drawingService.enabled}");
            }
        }
        
        private void CheckMaterial()
        {
            Debug.Log("2. Checking Material...");
            
            if (testMaterial == null)
            {
                Debug.LogWarning("⚠️ Test material not assigned, creating default...");
                
                // Создаем простой материал
                testMaterial = new Material(Shader.Find("Sprites/Default"));
                testMaterial.name = "DiagnosticLineMaterial";
                testMaterial.color = Color.red;
                
                Debug.Log("✅ Created default material");
            }
            else
            {
                Debug.Log("✅ Material assigned");
                Debug.Log($"   - Material name: {testMaterial.name}");
                Debug.Log($"   - Shader: {testMaterial.shader.name}");
            }
        }
        
        private void CheckSettings()
        {
            Debug.Log("3. Checking Settings...");
            
            var settings = new DrawingSettings
            {
                LineColor = testLineColor,
                LineWidth = testLineWidth,
                MaxLinesCount = 10,
                MaxPointsPerLine = 100,
                MinDistanceBetweenPoints = 0.01f,
                LinePoolInitialSize = 5,
                LinePoolMaxSize = 20
            };
            
            if (drawingService != null)
            {
                drawingService.SetDrawingSettings(settings);
                Debug.Log("✅ Settings applied to DrawingService");
                Debug.Log($"   - Line Color: {settings.LineColor}");
                Debug.Log($"   - Line Width: {settings.LineWidth}");
            }
        }
        
        private void CheckCamera()
        {
            Debug.Log("4. Checking Camera...");
            
            if (cameraTransform == null)
            {
                Debug.LogWarning("⚠️ Camera not found!");
                return;
            }
            
            Debug.Log("✅ Camera found");
            Debug.Log($"   - Camera position: {cameraTransform.position}");
            Debug.Log($"   - Camera rotation: {cameraTransform.rotation.eulerAngles}");
            
            // Рассчитываем позицию перед камерой
            Vector3 inFrontOfCamera = cameraTransform.position + cameraTransform.forward * 2f;
            Debug.Log($"   - Position 2m in front of camera: {inFrontOfCamera}");
        }
        
        private void CreateDiagnosticLine()
        {
            Debug.Log("5. Creating diagnostic line...");
            
            if (drawingService == null)
            {
                Debug.LogError("❌ Cannot create line - DrawingService is null");
                return;
            }
            
            // Создаем линию перед камерой
            Vector3 startPos = cameraTransform != null 
                ? cameraTransform.position + cameraTransform.forward * 2f + Vector3.up * 0.5f
                : testStartPosition;
                
            Debug.Log($"   - Start position: {startPos}");
            
            try
            {
                drawingService.StartLine(startPos);
                Debug.Log("✅ StartLine called successfully");
                
                // Добавляем несколько точек
                for (int i = 1; i <= 5; i++)
                {
                    Vector3 point = startPos + Vector3.right * (i * 0.2f);
                    drawingService.AddPointToLine(point);
                    Debug.Log($"   - Added point {i}: {point}");
                }
                
                drawingService.EndLine();
                Debug.Log("✅ EndLine called successfully");
                
                // Проверяем результат
                var stats = drawingService.GetPoolStatistics();
                Debug.Log($"   - Pool stats: Total={stats.TotalCreated}, Active={stats.ActiveObjects}");
                
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error creating line: {ex.Message}");
                Debug.LogError($"   Stack trace: {ex.StackTrace}");
            }
        }
        
        private void CreateTestLineManual()
        {
            Debug.Log("=== MANUAL TEST LINE ===");
            
            if (drawingService == null)
            {
                Debug.LogError("DrawingService not available for manual test");
                return;
            }
            
            Vector3 startPos = new Vector3(0, 1, 2);
            
            drawingService.StartLine(startPos);
            drawingService.AddPointToLine(startPos + Vector3.right * 0.5f);
            drawingService.AddPointToLine(startPos + Vector3.right * 1f + Vector3.up * 0.5f);
            drawingService.AddPointToLine(startPos + Vector3.right * 1.5f);
            drawingService.EndLine();
            
            Debug.Log("Manual test line created");
        }
        
        private void CreateTestLineInFrontOfCamera()
        {
            Debug.Log("=== CAMERA FRONT TEST LINE ===");
            
            if (drawingService == null || cameraTransform == null)
            {
                Debug.LogError("DrawingService or Camera not available");
                return;
            }
            
            Vector3 centerPos = cameraTransform.position + cameraTransform.forward * 3f;
            
            drawingService.StartLine(centerPos + Vector3.left);
            drawingService.AddPointToLine(centerPos);
            drawingService.AddPointToLine(centerPos + Vector3.right);
            drawingService.AddPointToLine(centerPos + Vector3.right + Vector3.up);
            drawingService.EndLine();
            
            Debug.Log($"Camera front test line created at {centerPos}");
        }
        
        private void OnGUI()
        {
            if (!enableDetailedLogging) return;
            
            GUI.Box(new Rect(10, 10, 300, 120), "Drawing Service Diagnostic");
            
            GUI.Label(new Rect(20, 35, 280, 20), $"DrawingService: {(drawingService != null ? "✅" : "❌")}");
            GUI.Label(new Rect(20, 55, 280, 20), $"Material: {(testMaterial != null ? "✅" : "❌")}");
            GUI.Label(new Rect(20, 75, 280, 20), $"Camera: {(cameraTransform != null ? "✅" : "❌")}");
            
            GUI.Label(new Rect(20, 100, 280, 20), "D - Diagnostic | T - Test | G - Camera Front");
            
            if (drawingService != null)
            {
                var stats = drawingService.GetPoolStatistics();
                GUI.Label(new Rect(20, 140, 280, 20), $"Pool: {stats.ActiveObjects}/{stats.TotalCreated}");
            }
        }
    }
}