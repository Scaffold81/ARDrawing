using UnityEngine;
using UnityEngine.UI;
using R3;
using System;
using ARDrawing.Core.Models;
using ARDrawing.Core.Interfaces;
using ARDrawing.UI.Components;
using Cysharp.Threading.Tasks;
using Zenject;
using System.Collections.Generic;

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
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        
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
        private readonly ReactiveProperty<bool> canSave = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> hasAvailableSaves = new ReactiveProperty<bool>(false);
        
        // Public Observables
        public Observable<Color> CurrentColor => currentColor.AsObservable();
        
        // Quick color buttons
        private Button[] quickColorButtons;
        
        // Dependencies
        [Inject] private ISaveLoadService saveLoadService;
        [Inject] private IDrawingService drawingService;
        
        // Events
        public event Action<Color> OnColorSelected;
        public event Action OnClearRequested;
        public event Action OnUndoRequested;
        public event Action OnColorPickerRequested;
        public event Action OnSaveRequested;
        public event Action OnLoadRequested;
        
        #region Unity Lifecycle
        
        protected override void Start()
        {
            base.Start();
            SetupMainPanel();
            SetupSubscriptions();
        }
        
        #endregion
        
        #region Save/Load Operations
        
        private async UniTask SaveCurrentDrawing()
        {
            try
            {
                // Получаем текущие линии из DrawingService
                var currentLines = await GetCurrentDrawingLines();
                
                if (currentLines.Count == 0)
                {
                    Debug.Log("[MainPanel] No lines to save");
                    return;
                }
                
                // Генерируем имя файла с датой и временем
                string fileName = "drawing_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                // Сохраняем
                var result = await saveLoadService.SaveDrawingAsync(currentLines, fileName);
                
                if (result.success)
                {
                    Debug.Log($"[MainPanel] Drawing saved successfully: {fileName}");
                    OnSaveRequested?.Invoke();
                    
                    // Обновляем состояние кнопки Load
                    await CheckAvailableSaves();
                }
                else
                {
                    Debug.LogError($"[MainPanel] Save failed: {result.errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainPanel] Save error: {ex.Message}");
            }
        }
        
        private async UniTask LoadLatestDrawing()
        {
            try
            {
                // Получаем список доступных сохранений
                var availableSaves = await saveLoadService.GetAvailableSavesAsync();
                
                if (availableSaves.Count == 0)
                {
                    Debug.Log("[MainPanel] No saves available to load");
                    return;
                }
                
                // Загружаем последнее сохранение
                string latestSave = availableSaves[0]; // Список уже отсортирован по дате
                var result = await saveLoadService.LoadDrawingAsync(latestSave);
                
                if (result.success)
                {
                    Debug.Log($"[MainPanel] Drawing loaded successfully: {latestSave}");
                    
                    // Очищаем текущий рисунок и загружаем новый
                    drawingService?.ClearAllLines();
                    
                    // Загружаем линии в DrawingService
                    foreach (var line in result.data)
                    {
                        if (line.Points.Count > 0)
                        {
                            drawingService?.StartLine(line.Points[0]);
                            
                            for (int i = 1; i < line.Points.Count; i++)
                            {
                                drawingService?.AddPointToLine(line.Points[i]);
                            }
                            
                            drawingService?.EndLine();
                        }
                    }
                    
                    OnLoadRequested?.Invoke();
                    
                    // Обновляем состояния кнопок
                    UpdateButtonStatesForDrawing();
                }
                else
                {
                    Debug.LogError($"[MainPanel] Load failed: {result.errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainPanel] Load error: {ex.Message}");
            }
        }
        
        private async UniTask<List<DrawingLine>> GetCurrentDrawingLines()
        {
            await UniTask.Yield();
            return drawingService?.GetAllLines() ?? new List<DrawingLine>();
        }

        private async UniTask CheckAvailableSaves()
        {
            try
            {
                var availableSaves = await saveLoadService.GetAvailableSavesAsync();
                hasAvailableSaves.Value = availableSaves.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainPanel] Failed to check available saves: {ex.Message}");
                hasAvailableSaves.Value = false;
            }
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
            
            // Save Button
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(() => {
                    Debug.Log("[MainPanel] Save button clicked");
                    SaveCurrentDrawing().Forget();
                });
                saveButton.interactable = false; // Initially disabled
            }
            
            // Load Button
            if (loadButton != null)
            {
                loadButton.onClick.AddListener(() => {
                    Debug.Log("[MainPanel] Load button clicked");
                    LoadLatestDrawing().Forget();
                });
                loadButton.interactable = false; // Initially disabled
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
            canSave.Subscribe(UpdateSaveButton);
            hasAvailableSaves.Subscribe(UpdateLoadButton);
            
            // Check for available saves on start
            CheckAvailableSaves().Forget();
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
        
        private void UpdateSaveButton(bool canSaveValue)
        {
            if (saveButton != null)
            {
                saveButton.interactable = canSaveValue;
            }
        }
        
        private void UpdateLoadButton(bool hasAvailableSavesValue)
        {
            if (loadButton != null)
            {
                loadButton.interactable = hasAvailableSavesValue;
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
            canSave.Value = count > 0;
        }
        
        /// <summary>
        /// Обновить состояния кнопок после изменений в рисовании.
        /// Update button states after drawing changes.
        /// </summary>
        public void UpdateButtonStatesForDrawing()
        {
            int lineCount = drawingService?.GetLineCount() ?? 0;
            SetLineCount(lineCount);
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
            canSave?.Dispose();
            hasAvailableSaves?.Dispose();
        }
        
        #endregion
    }
}
