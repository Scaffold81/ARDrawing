using UnityEngine;
using UnityEngine.UI;
using R3;
using R3.Triggers;
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
        private ReactiveProperty<UIMode> currentUIMode;
        private ReactiveProperty<Color> selectedColor;
        
        // Disposal tracking
        private bool _isDisposed = false;
        
        // Public Observables
        public Observable<UIMode> CurrentUIMode => currentUIMode?.AsObservable() ?? Observable.Empty<UIMode>();
        public Observable<Color> SelectedColor => selectedColor?.AsObservable() ?? Observable.Empty<Color>();
        
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
            if (_isDisposed) return;
            
            // Initialize reactive properties
            currentUIMode = new ReactiveProperty<UIMode>(UIMode.Drawing);
            selectedColor = new ReactiveProperty<Color>(Color.white);
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
            }
            
            // Subscribe to DrawingService to update button states
            if (drawingService != null)
            {
                drawingService.ActiveLines.Subscribe(lines => {
                    UpdateButtonStates(lines.Count);
                }).AddTo(this);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnClearButtonPressed()
        {
            if (drawingService != null)
            {
                drawingService.ClearAllLines();
            }
            else
            {
                Debug.LogError("[UIPresenter] DrawingService is null! Cannot clear lines.");
            }
        }
        
        private void OnUndoButtonPressed()
        {
            if (drawingService != null)
            {
                bool undoSuccessful = drawingService.UndoLastLine();
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
                var currentSettings = new DrawingSettings();
                currentSettings.LineColor = color;
                currentSettings.LineWidth = 0.01f; // Default width for Phase 4
                drawingService.SetDrawingSettings(currentSettings);
            }
            ToggleColorPicker();
        }
        
        private void UpdateButtonStates(int lineCount)
        {
            var mainPanelComponent = mainPanel?.GetComponent<MainPanel>();
            if (mainPanelComponent != null)
            {
                mainPanelComponent.SetLineCount(lineCount);
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
            }
        }
        
        private void HideColorPicker()
        {
            if (colorPickerPanel != null)
            {
                colorPickerPanel.SetActive(false);
                currentUIMode.Value = UIMode.Drawing;
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
                OnClearButtonPressed();
            }
            
            // Test Undo button with Z key
            if (Input.GetKeyDown(KeyCode.Z))
            {
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
            var settings = new DrawingSettings();
            settings.LineColor = selectedColor.Value;
            settings.LineWidth = 0.01f;
            return settings;
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
            if (_isDisposed) return;
            _isDisposed = true;
            
            try
            {
                if (currentUIMode != null && !currentUIMode.IsDisposed)
                {
                    currentUIMode.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIPresenter] Error disposing currentUIMode: {ex.Message}");
            }
            finally
            {
                currentUIMode = null;
            }
            
            try
            {
                if (selectedColor != null && !selectedColor.IsDisposed)
                {
                    selectedColor.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIPresenter] Error disposing selectedColor: {ex.Message}");
            }
            finally
            {
                selectedColor = null;
            }
            
            if (showDebugInfo)
                Debug.Log("[UIPresenter] UI system cleaned up safely");
        }
        
        #endregion
    }
    
    public enum UIMode
    {
        Drawing,
        ColorPicker
    }
}
