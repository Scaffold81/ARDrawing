using UnityEngine;
using ARDrawing.Core.Services;
using ARDrawing.Core.Models;

namespace ARDrawing.Testing
{
    /// <summary>
    /// Простой тестовый компонент для быстрой проверки DrawingService.
    /// Simple test component for quick DrawingService verification.
    /// </summary>
    public class SimpleDrawingTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private DrawingService drawingService;
        [SerializeField] private Material lineMaterial;
        
        private void Start()
        {
            // Создаем DrawingService если не назначен
            if (drawingService == null)
            {
                var serviceObject = new GameObject("DrawingService");
                drawingService = serviceObject.AddComponent<DrawingService>();
            }
            
            Debug.Log("[SimpleDrawingTest] DrawingService initialized");
        }
        
        private void Update()
        {
            // Простое тестирование через клавиши
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestDrawing();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (drawingService != null)
                {
                    drawingService.ClearAllLines();
                    Debug.Log("[SimpleDrawingTest] Cleared all lines");
                }
                else
                {
                    Debug.LogError("[SimpleDrawingTest] DrawingService is null - cannot clear lines");
                }
            }
        }
        
        private void TestDrawing()
        {
            if (drawingService == null)
            {
                Debug.LogError("[SimpleDrawingTest] DrawingService is null - cannot create test line");
                return;
            }
            
            Debug.Log("[SimpleDrawingTest] Starting test drawing...");
            
            // Создаем тестовую линию
            Vector3 startPoint = new Vector3(0, 1, 1);
            
            Debug.Log($"[SimpleDrawingTest] Starting line at {startPoint}");
            drawingService.StartLine(startPoint);
            
            // Добавляем несколько точек
            for (int i = 1; i <= 5; i++)
            {
                Vector3 point = startPoint + Vector3.right * (i * 0.2f);
                Debug.Log($"[SimpleDrawingTest] Adding point {i}: {point}");
                drawingService.AddPointToLine(point);
            }
            
            // Завершаем линию
            Debug.Log("[SimpleDrawingTest] Ending line");
            drawingService.EndLine();
            
            Debug.Log("[SimpleDrawingTest] Created test line with 6 points");
        }
    }
}