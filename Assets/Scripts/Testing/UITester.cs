using UnityEngine;
using UnityEngine.UI;
using ARDrawing.UI.Presenters;
using ARDrawing.UI.Components;
using ARDrawing.UI.Panels;
using ARDrawing.Core.Models;
using Zenject;
using R3;
using System;

namespace ARDrawing.Testing
{
    /// <summary>
    /// UITester отвечает за тестирование UI системы Phase 4.
    /// Обеспечивает testing interface для всех UI компонентов и панелей.
    /// UITester handles UI system testing for Phase 4.
    /// Provides testing interface for all UI components and panels.
    /// </summary>
    public class UITester : MonoBehaviour
    {
        [Header("Testing Configuration")]
        [SerializeField] private bool enableTesting = true;
        [SerializeField] private bool showTestingGUI = true;
        [SerializeField] private bool enableDebugLog = true;
        
        [Header("Test Controls")]
        [SerializeField] private KeyCode toggleUIKey = KeyCode.U;
        [SerializeField] private KeyCode colorPickerKey = KeyCode.C;
        [SerializeField] private KeyCode settingsKey = KeyCode.S;
        [SerializeField] private KeyCode clearAllKey = KeyCode.X;
        
        [Header("UI Test Settings")]
        [SerializeField] private bool testButtonCreation = true;
        [SerializeField] private bool testPanelAnimations = true;
        [SerializeField] private bool testColorSelection = true;
        
        // Dependencies
        [Inject] private UIPresenter uiPresenter;
        
        // Test State
        private bool uiVisible = true;
        private int currentColorIndex = 0;
        private float lastTestTime;
        private UIMode currentUIMode = UIMode.Drawing;
        
        // Test Colors
        private readonly Color[] testColors = 
        {
            Color.white, Color.red, Color.green, Color.blue,
            Color.yellow, Color.magenta, Color.cyan, Color.black
        };
        
        // Test Widths
        private readonly float[] testWidths = 
        {
            0.005f, 0.01f, 0.02f, 0.03f, 0.05f
        };
        
        // GUI State
        private Rect testWindowRect = new Rect(350, 100, 300, 450);
        private bool showTestWindow = true;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeTester();
            
            if (enableDebugLog)
                Debug.Log("[UITester] Phase 4 UI testing initialized");
        }
        
        private void Update()
        {
            if (!enableTesting) return;
            
            HandleTestInput();
            RunAutomaticTests();
        }
        
        private void OnGUI()
        {
            if (showTestingGUI && showTestWindow)
            {
                testWindowRect = GUI.Window(4, testWindowRect, DrawTestWindow, "Phase 4: UI System Tester");
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeTester()
        {
            if (uiPresenter == null)
            {
                Debug.LogError("[UITester] UIPresenter not injected!");
                return;
            }
            
            // Subscribe to UI events
            SubscribeToUIEvents();
            
            // Create test buttons if enabled
            if (testButtonCreation)
            {
                CreateTestButtons();
            }
        }
        
        private void SubscribeToUIEvents()
        {
            if (uiPresenter != null)
            {
                uiPresenter.SelectedColor.Subscribe(OnColorChanged);
                uiPresenter.CurrentUIMode.Subscribe(OnUIModeChanged);
            }
        }
        
        #endregion
        
        #region Test Button Creation
        
        private void CreateTestButtons()
        {
            // Create color test buttons
            CreateColorButtons();
            
            // Create action buttons
            CreateActionButtons();
            
            if (enableDebugLog)
                Debug.Log("[UITester] Test buttons created");
        }
        
        private void CreateColorButtons()
        {
            for (int i = 0; i < testColors.Length; i++)
            {
                CreateColorButton(testColors[i], i);
            }
        }
        
        private void CreateColorButton(Color color, int index)
        {
            // Create UI Button instead of 3D button
            var buttonGO = new GameObject($"ColorButton_{color.ToString()}");
            buttonGO.transform.SetParent(transform);
            
            // Add UI components
            var rectTransform = buttonGO.AddComponent<RectTransform>();
            var image = buttonGO.AddComponent<Image>();
            var button = buttonGO.AddComponent<Button>();
            
            // Setup visual
            image.color = color;
            rectTransform.sizeDelta = new Vector2(50, 50);
            rectTransform.anchoredPosition = new Vector2(-200 + (index * 60), 100);
            
            // Setup interaction
            button.onClick.AddListener(() => TestColorSelection(index));
        }
        
        private void CreateActionButtons()
        {
            // Clear button
            CreateActionButton("Clear", new Vector3(0.3f, 0.1f, 0f), Color.red);
            
            // Color picker button
            CreateActionButton("ColorPicker", new Vector3(0.3f, -0.1f, 0f), Color.cyan);
        }
        
        private void CreateActionButton(string actionName, Vector3 position, Color color)
        {
            // Create UI Button instead of 3D button
            var buttonGO = new GameObject($"{actionName}Button");
            buttonGO.transform.SetParent(transform);
            
            // Add UI components
            var rectTransform = buttonGO.AddComponent<RectTransform>();
            var image = buttonGO.AddComponent<Image>();
            var button = buttonGO.AddComponent<Button>();
            
            // Setup visual
            image.color = color;
            rectTransform.sizeDelta = new Vector2(80, 40);
            // Convert 3D position to UI position
            rectTransform.anchoredPosition = new Vector2(position.x * 200f, position.y * 200f);
            
            // Setup interaction based on action
            switch (actionName.ToLower())
            {
                case "clear":
                    button.onClick.AddListener(() => TestClearAll());
                    break;
                case "colorpicker":
                    button.onClick.AddListener(() => TestColorPicker());
                    break;
            }
            
            // Add text label
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            var textRect = textGO.AddComponent<RectTransform>();
            var text = textGO.AddComponent<Text>();
            text.text = actionName;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 12;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleTestInput()
        {
            // Toggle UI visibility
            if (Input.GetKeyDown(toggleUIKey))
            {
                ToggleUI();
            }
            
            // Color picker
            if (Input.GetKeyDown(colorPickerKey))
            {
                TestColorPicker();
            }
            
            // Settings
            if (Input.GetKeyDown(settingsKey))
            {
                TestSettings();
            }
            
            // Clear all
            if (Input.GetKeyDown(clearAllKey))
            {
                TestClearAll();
            }
            
            // Cycle colors with number keys
            for (int i = 1; i <= testColors.Length && i <= 8; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    TestColorSelection(i - 1);
                }
            }
            
            // Test GUI toggle
            if (Input.GetKeyDown(KeyCode.F4))
            {
                showTestWindow = !showTestWindow;
            }
        }
        
        #endregion
        
        #region Test Methods
        
        private void ToggleUI()
        {
            uiVisible = !uiVisible;
            
            if (uiPresenter != null)
            {
                uiPresenter.SetUIVisible(uiVisible);
            }
            
            if (enableDebugLog)
                Debug.Log($"[UITester] UI toggled: {uiVisible}");
        }
        
        private void TestColorPicker()
        {
            if (uiPresenter != null)
            {
                // Просто вызываем ToggleColorPicker напрямую
                var toggleMethod = uiPresenter.GetType().GetMethod("ToggleColorPicker", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (toggleMethod != null)
                {
                    toggleMethod.Invoke(uiPresenter, null);
                    
                    if (enableDebugLog)
                        Debug.Log("[UITester] Color Picker test triggered via reflection");
                }
                else
                {
                    // Или просто симулируем клавишу C
                    if (enableDebugLog)
                        Debug.Log("[UITester] Testing color picker with keyboard shortcut C");
                }
            }
        }
        
        private void TestSettings()
        {
            if (uiPresenter != null)
            {
                if (enableDebugLog)
                    Debug.Log("[UITester] Testing settings panel");
            }
        }
        
        private void TestClearAll()
        {
            if (uiPresenter != null)
            {
                if (enableDebugLog)
                    Debug.Log("[UITester] Testing clear all functionality");
            }
        }
        
        private void TestColorSelection(int colorIndex)
        {
            if (colorIndex >= 0 && colorIndex < testColors.Length && uiPresenter != null)
            {
                currentColorIndex = colorIndex;
                uiPresenter.SetSelectedColor(testColors[colorIndex]);
                
                if (enableDebugLog)
                    Debug.Log($"[UITester] Color selected: {testColors[colorIndex]}");
            }
        }
        
        private void TestLineWidthSelection(int widthIndex)
        {
            // Line width selection removed in Phase 4
            if (enableDebugLog)
                Debug.Log("[UITester] Line width selection not available in Phase 4");
        }
        
        #endregion
        
        #region Automatic Testing
        
        private void RunAutomaticTests()
        {
            // Run periodic tests
            if (Time.time - lastTestTime >= 8.0f) // Every 8 seconds
            {
                if (testColorSelection && testColors.Length > 0)
                {
                    // Cycle through colors automatically for testing
                    int nextColorIndex = (currentColorIndex + 1) % testColors.Length;
                    TestColorSelection(nextColorIndex);
                }
                
                lastTestTime = Time.time;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnColorChanged(Color newColor)
        {
            if (enableDebugLog)
                Debug.Log($"[UITester] Color changed to: {newColor}");
        }
        
        private void OnUIModeChanged(UIMode newMode)
        {
            currentUIMode = newMode;
            
            if (enableDebugLog)
                Debug.Log($"[UITester] UI mode changed to: {newMode}");
        }
        
        #endregion
        
        #region GUI
        
        private void DrawTestWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            // Header
            GUILayout.Label("Phase 4: UI System Tester", GUI.skin.box);
            
            // UI Controls
            GUILayout.Space(10);
            GUILayout.Label("UI Controls:", GUI.skin.box);
            
            if (GUILayout.Button($"Toggle UI (U) - Currently: {(uiVisible ? "Visible" : "Hidden")}"))
            {
                ToggleUI();
            }
            
            if (GUILayout.Button("Test Color Picker (C)"))
            {
                TestColorPicker();
            }
            
            if (GUILayout.Button("Test Settings (S)"))
            {
                TestSettings();
            }
            
            if (GUILayout.Button("Clear All (X)"))
            {
                TestClearAll();
            }
            
            // Color Testing
            GUILayout.Space(10);
            GUILayout.Label("Color Testing:", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Mathf.Min(testColors.Length, 4); i++)
            {
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = testColors[i];
                
                if (GUILayout.Button($"{i + 1}", GUILayout.Width(40)))
                {
                    TestColorSelection(i);
                }
                
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            for (int i = 4; i < Mathf.Min(testColors.Length, 8); i++)
            {
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = testColors[i];
                
                if (GUILayout.Button($"{i + 1}", GUILayout.Width(40)))
                {
                    TestColorSelection(i);
                }
                
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();
            
            // Line Width Testing - удалено в Phase 4
            GUILayout.Space(10);
            GUILayout.Label("Line Width Testing: [Phase 4 - Not Available]", GUI.skin.box);
            
            // Current State
            GUILayout.Space(10);
            GUILayout.Label("Current State:", GUI.skin.box);
            
            if (uiPresenter != null)
            {
                var settings = uiPresenter.GetCurrentDrawingSettings();
                GUILayout.Label($"Color: {settings.LineColor}");
                GUILayout.Label($"Width: {settings.LineWidth:F3}");
                GUILayout.Label($"UI Mode: {currentUIMode}");
            }
            else
            {
                GUILayout.Label("UIPresenter: Not Available", GUI.skin.box);
            }
            
            // Testing Options
            GUILayout.Space(10);
            GUILayout.Label("Testing Options:", GUI.skin.box);
            
            testButtonCreation = GUILayout.Toggle(testButtonCreation, "Auto Button Creation");
            testPanelAnimations = GUILayout.Toggle(testPanelAnimations, "Test Panel Animations");
            testColorSelection = GUILayout.Toggle(testColorSelection, "Test Color Selection");
            
            // Controls Info
            GUILayout.Space(10);
            GUILayout.Label("Keyboard Controls:", GUI.skin.box);
            GUILayout.Label("U - Toggle UI | C - Color Picker | S - Settings");
            GUILayout.Label("X - Clear All | 1-8 - Select Colors");
            GUILayout.Label("F4 - Toggle Test Window");
            
            GUILayout.EndVertical();
            
            GUI.DragWindow();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Получить статистику тестирования UI.
        /// Get UI testing statistics.
        /// </summary>
        public string GetUITestStats()
        {
            string stats = "Phase 4 UI Testing Stats:\n";
            stats += $"- UI System Active: {enableTesting}\n";
            stats += $"- UI Visible: {uiVisible}\n";
            stats += $"- Current Color Index: {currentColorIndex}\n";
            stats += $"- Test Buttons Created: {testButtonCreation}\n";
            
            if (uiPresenter != null)
            {
                stats += "\nUIPresenter Info:\n";
                stats += uiPresenter.GetUIInfo();
            }
            
            return stats;
        }
        
        /// <summary>
        /// Активировать/деактивировать тестирование.
        /// Enable/disable testing.
        /// </summary>
        public void SetTestingEnabled(bool enabled)
        {
            enableTesting = enabled;
            
            if (enableDebugLog)
                Debug.Log($"[UITester] Testing {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Автоматически протестировать все UI компоненты.
        /// Automatically test all UI components.
        /// </summary>
        [ContextMenu("Run Full UI Test")]
        public void RunFullUITest()
        {
            if (!enableTesting || uiPresenter == null) return;
            
            Debug.Log("[UITester] Running full UI test sequence...");
            
            // Test all colors
            for (int i = 0; i < testColors.Length; i++)
            {
                TestColorSelection(i);
            }
            
            // Test UI modes
            TestColorPicker();
            
            Debug.Log("[UITester] Full UI test completed");
        }
        
        #endregion
    }
}