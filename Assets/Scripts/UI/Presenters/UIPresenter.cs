using UnityEngine;
using UnityEngine.UI;
using R3;
using System;
using System.Collections.Generic;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using ARDrawing.UI.Components;
using ARDrawing.UI.Panels;
using Zenject;

namespace ARDrawing.UI.Presenters
{
    /// <summary>
    /// UIPresenter - упрощенный MVP presenter для Phase 4.
    /// Управляет только MainPanel и ColorPickerPanel.
    /// UIPresenter - simplified MVP presenter for Phase 4.
    /// Manages only MainPanel and ColorPickerPanel.
    /// </summary>
    public class UIPresenter : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject colorPickerPanel;
        [SerializeField] private bool showDebugInfo = true;
        
        // Dependencies
        [Inject] private IDrawingService drawingService;
        
        // UI State
        private readonly ReactiveProperty<UIMode> currentUIMode = new ReactiveProperty<UIMode>(UIMode.Drawing);
        private readonly ReactiveProperty<Color> selectedColor = new ReactiveProperty<Color>(Color.white);
        
        // Public Observables
        public Observable<UIMode> CurrentUIMode => currentUIMode.AsObservable();
        public Observable<Color> SelectedColor => selectedColor.AsObservable();
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeUI();
            SubscribeToPanelEvents();
        }
        
        private void Update()
        {
            HandleUIInput();
        }
        
        private void OnDestroy()
        {
            CleanupUI();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeUI()
        {
            selectedColor.Value = Color.white;
            currentUIMode.Value = UIMode.Drawing;
            
            if (showDebugInfo)
                Debug.Log("[UIPresenter] UI system initialized");
        }
        
        private void SubscribeToPanelEvents()
        {
            // Subscribe to MainPanel events
            var mainPanelComponent = mainPanel?.GetComponent<MainPanel>();
            if (mainPanelComponent != null)
            {
                mainPanelComponent.OnColorPickerRequested += () => ToggleColorPicker();
                mainPanelComponent.OnClearRequested += () => OnClearButtonPressed();
                mainPanelComponent.OnUndoRequested += () => OnUndoButtonPressed();
                mainPanelComponent.OnColorSelected += (color) => OnColorSelected(color);
            }
            
            // Subscribe to ColorPickerPanel events
            var colorPickerComponent = colorPickerPanel?.GetComponent<ColorPickerPanel>();
            if (colorPickerComponent != null)
            {
                colorPickerComponent.OnColorSelected += (color) => OnColorSelected(color);
                colorPickerComponent.OnColorPickerClosed += () => OnColorPickerClosed();
            }
            
            // Subscribe to DrawingService to update button states
            if (drawingService != null)
            {
                drawingService.ActiveLines.Subscribe(lines => {
                    UpdateButtonStates(lines.Count);
                });
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnClearButtonPressed()
        {
            if (showDebugInfo)
                Debug.Log("[UIPresenter] Clear button pressed!");
                
            if (drawingService != null)
            {
                drawingService.ClearAllLines();
                if (showDebugInfo)
                    Debug.Log("[UIPresenter] ClearAllLines() called on DrawingService");
            }
            else
            {
                Debug.LogError("[UIPresenter] DrawingService is null! Cannot clear lines.");
            }
        }
        
        private void OnUndoButtonPressed()
        {
            if (showDebugInfo)
                Debug.Log("[UIPresenter] Undo button pressed!");
                
            if (drawingService != null)
            {
                bool undoSuccessful = drawingService.UndoLastLine();
                if (showDebugInfo)
                {
                    if (undoSuccessful)
                        Debug.Log("[UIPresenter] Successfully undone last line");
                    else
                        Debug.Log("[UIPresenter] Undo failed - no lines to undo or currently drawing");
                }
            }
            else
            {
                Debug.LogError("[UIPresenter] DrawingService is null! Cannot undo.");
            }
        }
        
        private void OnColorSelected(Color color)
        {
            selectedColor.Value = color;
            
            // Apply to drawing service
            if (drawingService != null)
            {
                var currentSettings = new DrawingSettings
                {
                    LineColor = color,
                    LineWidth = 0.01f // Default width for Phase 4
                };
                drawingService.SetDrawingSettings(currentSettings);
            }
            ToggleColorPicker();
            if (showDebugInfo)
                Debug.Log($"[UIPresenter] Color selected: {color}");
        }
        
        private void OnColorPickerClosed()
        {
            // Панель закрылась, вернуться в Drawing режим
            currentUIMode.Value = UIMode.Drawing;
            
            if (showDebugInfo)
                Debug.Log("[UIPresenter] Color picker closed, returned to Drawing mode");
        }
        
        private void UpdateButtonStates(int lineCount)
        {
            var mainPanelComponent = mainPanel?.GetComponent<MainPanel>();
            if (mainPanelComponent != null)
            {
                mainPanelComponent.SetLineCount(lineCount);
                
                if (showDebugInfo)
                    Debug.Log($"[UIPresenter] Updated button states for {lineCount} lines");
            }
        }
        
        #endregion
        
        #region UI Mode Management
        
        private void ToggleColorPicker()
        {
            if (currentUIMode.Value == UIMode.ColorPicker)
            {
                HideColorPicker();
            }
            else
            {
                ShowColorPicker();
            }
        }
        
        private void ShowColorPicker()
        {
            if (colorPickerPanel != null)
            {
                colorPickerPanel.SetActive(true);
                currentUIMode.Value = UIMode.ColorPicker;
                
                if (showDebugInfo)
                    Debug.Log("[UIPresenter] Color Picker opened");
            }
        }
        
        private void HideColorPicker()
        {
            if (colorPickerPanel != null)
            {
                colorPickerPanel.SetActive(false);
                currentUIMode.Value = UIMode.Drawing;
                
                if (showDebugInfo)
                    Debug.Log("[UIPresenter] Color Picker closed");
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleUIInput()
        {
            // Keyboard shortcuts for testing
            if (Input.GetKeyDown(KeyCode.C))
            {
                ToggleColorPicker();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HideColorPicker();
            }
            
            // Test Clear button with X key
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.Log("[UIPresenter] X key pressed - testing Clear function");
                OnClearButtonPressed();
            }
            
            // Test Undo button with Z key
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Debug.Log("[UIPresenter] Z key pressed - testing Undo function");
                OnUndoButtonPressed();
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Программно установить цвет.
        /// Programmatically set color.
        /// </summary>
        public void SetSelectedColor(Color color)
        {
            OnColorSelected(color);
        }
        
        /// <summary>
        /// Получить текущие настройки рисования.
        /// Get current drawing settings.
        /// </summary>
        public DrawingSettings GetCurrentDrawingSettings()
        {
            return new DrawingSettings
            {
                LineColor = selectedColor.Value,
                LineWidth = 0.01f
            };
        }
        
        /// <summary>
        /// Показать/скрыть UI.
        /// Show/hide UI.
        /// </summary>
        public void SetUIVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        /// <summary>
        /// Получить информацию о UI состоянии.
        /// Get UI state information.
        /// </summary>
        public string GetUIInfo()
        {
            return $"UI Presenter State:\n" +
                   $"- Mode: {currentUIMode.Value}\n" +
                   $"- Visible: {gameObject.activeSelf}\n" +
                   $"- Color: {selectedColor.Value}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupUI()
        {
            currentUIMode?.Dispose();
            selectedColor?.Dispose();
            
            if (showDebugInfo)
                Debug.Log("[UIPresenter] UI system cleaned up");
        }
        
        #endregion
    }
    
    public enum UIMode
    {
        Drawing,
        ColorPicker
    }
}
