using UnityEngine;
using UnityEngine.UI;
using R3;
using System;
using ARDrawing.Core.Models;
using ARDrawing.UI.Components;

namespace ARDrawing.UI.Panels
{
    /// <summary>
    /// MainPanel - основная панель инструментов для AR рисования.
    /// Содержит основные кнопки управления: цвет, очистка, отмена, настройки.
    /// MainPanel - main tool panel for AR drawing.
    /// Contains main control buttons: color, clear, undo, settings.
    /// </summary>
    public class MainPanel : UIPanel
    {
        [Header("Main Controls")]
        [SerializeField] private Button colorButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button undoButton;
        
        [Header("Quick Color Palette")]
        [SerializeField] private Transform quickColorContainer;
        [SerializeField] private Button quickColorButtonPrefab;
        [SerializeField] private Color[] quickColors = 
        {
            Color.white, Color.red, Color.green, Color.blue,
            Color.yellow, Color.black
        };
        
        // State Management
        private readonly ReactiveProperty<Color> currentColor = new ReactiveProperty<Color>(Color.white);
        private readonly ReactiveProperty<bool> canUndo = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> canClear = new ReactiveProperty<bool>(false);
        
        // Public Observables
        public Observable<Color> CurrentColor => currentColor.AsObservable();
        
        // Quick color buttons
        private Button[] quickColorButtons;
        
        // Events
        public event Action<Color> OnColorSelected;
        public event Action OnClearRequested;
        public event Action OnUndoRequested;
        public event Action OnColorPickerRequested;
        
        #region Unity Lifecycle
        
        protected override void Start()
        {
            base.Start();
            SetupMainPanel();
            SetupSubscriptions();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void InitializePanel()
        {
            base.InitializePanel();
            
            // Set initial values
            currentColor.Value = Color.white;
        }
        
        private void SetupMainPanel()
        {
            SetupMainButtons();
            SetupQuickColors();
        }
        
        private void SetupMainButtons()
        {
            // Color Button
            if (colorButton != null)
            {
                colorButton.onClick.AddListener(() => {
                    Debug.Log("[MainPanel] Color button clicked");
                    OnColorPickerRequested?.Invoke();
                });
            }
            
            // Clear Button
            if (clearButton != null)
            {
                clearButton.onClick.AddListener(() => {
                    Debug.Log("[MainPanel] Clear button clicked");
                    OnClearRequested?.Invoke();
                });
                clearButton.interactable = false; // Initially disabled
            }
            
            // Undo Button
            if (undoButton != null)
            {
                undoButton.onClick.AddListener(() => {
                    Debug.Log("[MainPanel] Undo button clicked");
                    OnUndoRequested?.Invoke();
                });
                undoButton.interactable = false; // Initially disabled
            }
        }
        
        private void SetupQuickColors()
        {
            if (quickColorContainer == null || quickColorButtonPrefab == null) return;
            
            // Clear existing buttons
            foreach (Transform child in quickColorContainer)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
            
            quickColorButtons = new Button[quickColors.Length];
            
            for (int i = 0; i < quickColors.Length; i++)
            {
                Color color = quickColors[i];
                Button colorButton = Instantiate(quickColorButtonPrefab, quickColorContainer);
                
                // Setup button visual
                Image buttonImage = colorButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = color;
                }
                
                // Setup button interaction
                colorButton.onClick.AddListener(() => SelectQuickColor(color));
                
                quickColorButtons[i] = colorButton;
            }
        }
        
        private void SetupSubscriptions()
        {
            // Subscribe to state changes
            currentColor.Subscribe(color => { /* Visual feedback if needed */ });
            canUndo.Subscribe(UpdateUndoButton);
            canClear.Subscribe(UpdateClearButton);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void SelectQuickColor(Color color)
        {
            currentColor.Value = color;
            OnColorSelected?.Invoke(color);
        }
        
        #endregion
        
        #region Display Updates
        
        private void UpdateUndoButton(bool canUndoValue)
        {
            if (undoButton != null)
            {
                undoButton.interactable = canUndoValue;
            }
        }
        
        private void UpdateClearButton(bool canClearValue)
        {
            if (clearButton != null)
            {
                clearButton.interactable = canClearValue;
                Debug.Log($"[MainPanel] Clear button interactable set to: {canClearValue}");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Установить текущий цвет.
        /// Set current color.
        /// </summary>
        public void SetCurrentColor(Color color)
        {
            currentColor.Value = color;
        }
        
        /// <summary>
        /// Установить состояния кнопок на основе количества линий.
        /// Set button states based on line count.
        /// </summary>
        public void SetLineCount(int count)
        {
            canUndo.Value = count > 0;
            canClear.Value = count > 0;
        }
        
        /// <summary>
        /// Получить текущий цвет.
        /// Get current color.
        /// </summary>
        public Color GetCurrentColor()
        {
            return currentColor.Value;
        }
        
        #endregion
        
        #region Cleanup
        
        protected override void CleanupPanel()
        {
            base.CleanupPanel();
            
            currentColor?.Dispose();
            canUndo?.Dispose();
            canClear?.Dispose();
        }
        
        #endregion
    }
}
