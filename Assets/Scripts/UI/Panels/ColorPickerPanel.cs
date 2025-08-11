using UnityEngine;
using UnityEngine.UI;
using System;
using ARDrawing.UI.Panels;

namespace ARDrawing.UI.Panels
{
    /// <summary>
    /// ColorPickerPanel - максимально простая панель выбора цвета для Phase 4.
    /// Клик по цвету сразу применяет его и закрывает панель.
    /// ColorPickerPanel - ultra simple color picker panel for Phase 4.
    /// Click on color immediately applies it and closes panel.
    /// </summary>
    public class ColorPickerPanel : UIPanel
    {
        [Header("Basic Colors")]
        [SerializeField] private Color[] basicColors = 
        {
            Color.white, Color.black, Color.red, Color.green,
            Color.blue, Color.yellow, Color.magenta, Color.cyan
        };
        
        [Header("Color Buttons Layout")]
        [SerializeField] private float buttonSize = 60f;
        [SerializeField] private float buttonSpacing = 10f;
        [SerializeField] private int buttonsPerRow = 4;
        
        // State
        private bool isInitialized = false;
        private Button[] colorButtons;
        
        // Events
        public event Action<Color> OnColorSelected;
        
        #region Unity Lifecycle
        
        protected override void Start()
        {
            base.Start();
        }
        
        #endregion
        
        #region Initialization
        
        public override void ShowPanel(bool animated = true)
        {
            // Инициализировать только при первом показе
            if (!isInitialized)
            {
                InitializeColorPicker();
            }
            
            base.ShowPanel(animated);
        }
        
        private void InitializeColorPicker()
        {
            if (isInitialized) return;
            
            Debug.Log("[ColorPickerPanel] Initializing simple color picker...");
            
            CreateColorButtons();
            isInitialized = true;
            
            Debug.Log("[ColorPickerPanel] Simple color picker initialized successfully");
        }
        
        private void CreateColorButtons()
        {
            colorButtons = new Button[basicColors.Length];
            
            // Создать цветовые кнопки напрямую в этом объекте
            for (int i = 0; i < basicColors.Length; i++)
            {
                CreateSingleColorButton(basicColors[i], i);
            }
            
            Debug.Log($"[ColorPickerPanel] Created {basicColors.Length} color buttons");
        }
        
        private void CreateSingleColorButton(Color color, int index)
        {
            try
            {
                Debug.Log($"[ColorPickerPanel] Creating color button {index} with color {color}");
                
                // Создать GameObject для кнопки
                GameObject buttonGO = new GameObject($"ColorButton_{index}");
                buttonGO.transform.SetParent(transform, false);
                
                // Добавить RectTransform и настроить позицию
                RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
                SetupButtonPosition(rectTransform, index);
                
                // Добавить Image для отображения цвета
                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = color;
                
                // Добавить Button компонент
                Button colorButton = buttonGO.AddComponent<Button>();
                
                // Настроить клик - сразу применить цвет и закрыть
                Color capturedColor = color;
                colorButton.onClick.AddListener(() => {
                    Debug.Log($"[ColorPickerPanel] Color button {index} clicked! Color: {capturedColor}");
                    ApplyColorAndClose(capturedColor);
                });
                
                colorButtons[index] = colorButton;
                
                Debug.Log($"[ColorPickerPanel] Successfully created color button {index}: {color}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ColorPickerPanel] Failed to create color button {index}: {e.Message}");
            }
        }
        
        private void SetupButtonPosition(RectTransform rectTransform, int index)
        {
            // Рассчитать позицию в сетке
            int row = index / buttonsPerRow;
            int col = index % buttonsPerRow;
            
            // Настроить размер
            rectTransform.sizeDelta = new Vector2(buttonSize, buttonSize);
            
            // Рассчитать позицию
            float startX = -(buttonsPerRow - 1) * (buttonSize + buttonSpacing) * 0.5f;
            float startY = (buttonSize + buttonSpacing) * 0.5f;
            
            float posX = startX + col * (buttonSize + buttonSpacing);
            float posY = startY - row * (buttonSize + buttonSpacing);
            
            rectTransform.anchoredPosition = new Vector2(posX, posY);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }
        
        #endregion
        
        #region Color Selection
        
        private void ApplyColorAndClose(Color color)
        {
            Debug.Log($"[ColorPickerPanel] ApplyColorAndClose called with color: {color}");
            Debug.Log($"[ColorPickerPanel] Panel currently active: {gameObject.activeSelf}");
            
            // Отправить событие с выбранным цветом
            OnColorSelected?.Invoke(color);
            Debug.Log($"[ColorPickerPanel] OnColorSelected event invoked with color: {color}");
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Получить все доступные цвета.
        /// Get all available colors.
        /// </summary>
        public Color[] GetAvailableColors()
        {
            return (Color[])basicColors.Clone();
        }
        
        /// <summary>
        /// Установить новую палитру цветов.
        /// Set new color palette.
        /// </summary>
        public void SetColorPalette(Color[] newColors)
        {
            basicColors = newColors;
            
            // Пересоздать кнопки если уже инициализировано
            if (isInitialized)
            {
                ClearButtons();
                isInitialized = false;
                InitializeColorPicker();
            }
        }
        
        /// <summary>
        /// Очистить существующие кнопки.
        /// Clear existing buttons.
        /// </summary>
        private void ClearButtons()
        {
            if (colorButtons != null)
            {
                foreach (var button in colorButtons)
                {
                    if (button != null)
                    {
                        button.GetComponent<Button>().onClick.RemoveAllListeners();
                        DestroyImmediate(button.gameObject);
                    }
                }
                colorButtons = null;
            }
        }
        
        #endregion
    }
}
