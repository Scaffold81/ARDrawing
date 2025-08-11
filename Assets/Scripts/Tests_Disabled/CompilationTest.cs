using UnityEngine;
using ARDrawing.Core.Models;
using ARDrawing.Core.Interfaces;
using ARDrawing.UI.Presenters;
using ARDrawing.UI.Panels;
using ARDrawing.UI.Components;

namespace ARDrawing.Tests
{
    /// <summary>
    /// Тестовый скрипт для проверки компиляции всех зависимостей Phase 4.
    /// Test script to verify compilation of all Phase 4 dependencies.
    /// </summary>
    public class CompilationTest : MonoBehaviour
    {
        // Test instantiation of all key types
        private void TestCompilation()
        {
            // Models
            var settings = DrawingSettings.Default;
            var line = new DrawingLine();
            var handData = new HandInteractionData();
            var saveResult = SaveResult.Success("test", 123);
            var loadResult = LoadResult<int>.Success(42, "path");
            
            // UI Components  
            var button = GetComponent<InteractableButton>();
            var mainPanel = GetComponent<MainPanel>();
            var colorPanel = GetComponent<ColorPickerPanel>();
            var uiPanel = GetComponent<UIPanel>();
            var presenter = GetComponent<UIPresenter>();
            
            Debug.Log($"Compilation test passed! Settings: {settings}");
        }
    }
}
