using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using R3;
using System;
using System.Collections;

namespace ARDrawing.UI.Components
{
    /// <summary>
    /// ARUIButton - UI кнопка для World Space Canvas с AR поддержкой.
    /// Работает с Canvas UI System и поддерживает finger tracking.
    /// ARUIButton - UI button for World Space Canvas with AR support.
    /// Works with Canvas UI System and supports finger tracking.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ARUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
                             IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Button Configuration")]
        [SerializeField] private string buttonId = "ARButton";
        [SerializeField] private ARButtonType buttonType = ARButtonType.Action;
        [SerializeField] private bool useEnhancedEffects = true;
        
        [Header("Visual Effects")]
        [SerializeField] private bool enableScaleAnimation = true;
        [SerializeField] private bool enableColorAnimation = true;
        [SerializeField] private bool enableGlowEffect = true;
        
        [Header("Animation Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private float animationDuration = 0.2f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.cyan;
        [SerializeField] private Color pressedColor = Color.green;
        [SerializeField] private Color disabledColor = Color.gray;
        
        [Header("Audio")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private float audioVolume = 0.5f;
        
        // Components
        private Button button;
        private Image buttonImage;
        private AudioSource audioSource;
        private RectTransform rectTransform;
        
        // State Management
        private readonly ReactiveProperty<ARButtonState> currentState = new ReactiveProperty<ARButtonState>(ARButtonState.Normal);
        private readonly ReactiveProperty<bool> isInteractable = new ReactiveProperty<bool>(true);
        
        // Public Observables
        public Observable<ARButtonState> CurrentState => currentState.AsObservable();
        public Observable<bool> IsInteractable => isInteractable.AsObservable();
        
        // Animation
        private Coroutine currentAnimation;
        private Vector3 originalScale;
        private Color originalColor;
        
        // Events
        public event Action<string> OnButtonPressed;
        public event Action<string> OnButtonReleased;
        public event Action<string, bool> OnHoverChanged;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupButton();
            SubscribeToStateChanges();
        }
        
        private void OnDestroy()
        {
            CleanupButton();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            
            if (buttonImage != null)
            {
                originalColor = buttonImage.color;
            }
            
            if (rectTransform != null)
            {
                originalScale = rectTransform.localScale;
            }
            
            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.playOnAwake = false;
            audioSource.volume = audioVolume;
        }
        
        private void SetupButton()
        {
            if (button != null)
            {
                // Remove default button click to handle manually
                button.onClick.RemoveAllListeners();
                button.transition = Selectable.Transition.None; // We handle transitions manually
            }
            
            // Set initial state
            UpdateVisualState();
        }
        
        #endregion
        
        #region State Management
        
        private void SubscribeToStateChanges()
        {
            currentState.Subscribe(OnStateChanged);
            isInteractable.Subscribe(OnInteractableChanged);
        }
        
        private void OnStateChanged(ARButtonState newState)
        {
            UpdateVisualState();
            
            switch (newState)
            {
                case ARButtonState.Hovered:
                    PlayAudio(hoverSound);
                    if (enableScaleAnimation)
                        AnimateScale(hoverScale);
                    OnHoverChanged?.Invoke(buttonId, true);
                    break;
                    
                case ARButtonState.Pressed:
                    PlayAudio(clickSound);
                    if (enableScaleAnimation)
                        AnimateScale(pressedScale);
                    OnButtonPressed?.Invoke(buttonId);
                    break;
                    
                case ARButtonState.Normal:
                    if (enableScaleAnimation)
                        AnimateScale(1.0f);
                    OnHoverChanged?.Invoke(buttonId, false);
                    break;
            }
        }
        
        private void OnInteractableChanged(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
            
            currentState.Value = interactable ? ARButtonState.Normal : ARButtonState.Disabled;
        }
        
        private void UpdateVisualState()
        {
            if (buttonImage == null) return;
            
            Color targetColor = currentState.Value switch
            {
                ARButtonState.Normal => normalColor,
                ARButtonState.Hovered => hoverColor,
                ARButtonState.Pressed => pressedColor,
                ARButtonState.Disabled => disabledColor,
                _ => normalColor
            };
            
            if (enableColorAnimation)
            {
                AnimateColor(targetColor);
            }
            else
            {
                buttonImage.color = targetColor;
            }
        }
        
        #endregion
        
        #region UI Event Handlers
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isInteractable.Value)
            {
                currentState.Value = ARButtonState.Hovered;
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (isInteractable.Value)
            {
                currentState.Value = ARButtonState.Normal;
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (isInteractable.Value)
            {
                currentState.Value = ARButtonState.Pressed;
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (isInteractable.Value)
            {
                currentState.Value = ARButtonState.Hovered;
                OnButtonReleased?.Invoke(buttonId);
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // Click event handled by OnPointerDown/Up
        }
        
        #endregion
        
        #region Animation
        
        private void AnimateScale(float targetScale)
        {
            if (!enableScaleAnimation || rectTransform == null) return;
            
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            currentAnimation = StartCoroutine(ScaleAnimation(targetScale));
        }
        
        private void AnimateColor(Color targetColor)
        {
            if (!enableColorAnimation || buttonImage == null) return;
            
            StartCoroutine(ColorAnimation(targetColor));
        }
        
        private IEnumerator ScaleAnimation(float targetScale)
        {
            Vector3 startScale = rectTransform.localScale;
            Vector3 endScale = originalScale * targetScale;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;
                float easedProgress = animationCurve.Evaluate(progress);
                
                rectTransform.localScale = Vector3.Lerp(startScale, endScale, easedProgress);
                
                yield return null;
            }
            
            rectTransform.localScale = endScale;
            currentAnimation = null;
        }
        
        private IEnumerator ColorAnimation(Color targetColor)
        {
            Color startColor = buttonImage.color;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;
                
                buttonImage.color = Color.Lerp(startColor, targetColor, progress);
                
                yield return null;
            }
            
            buttonImage.color = targetColor;
        }
        
        #endregion
        
        #region Audio
        
        private void PlayAudio(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Программно нажать кнопку.
        /// Programmatically press button.
        /// </summary>
        public void PressButton()
        {
            if (isInteractable.Value)
            {
                currentState.Value = ARButtonState.Pressed;
                Invoke(nameof(ReleaseButton), 0.1f);
            }
        }
        
        /// <summary>
        /// Программно отпустить кнопку.
        /// Programmatically release button.
        /// </summary>
        public void ReleaseButton()
        {
            if (isInteractable.Value)
            {
                currentState.Value = ARButtonState.Normal;
            }
        }
        
        /// <summary>
        /// Установить активность кнопки.
        /// Set button interactable state.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable.Value = interactable;
        }
        
        /// <summary>
        /// Установить цвета кнопки.
        /// Set button colors.
        /// </summary>
        public void SetColors(Color normal, Color hover, Color pressed, Color disabled)
        {
            normalColor = normal;
            hoverColor = hover;
            pressedColor = pressed;
            disabledColor = disabled;
            
            UpdateVisualState();
        }
        
        /// <summary>
        /// Получить ID кнопки.
        /// Get button ID.
        /// </summary>
        public string GetButtonId()
        {
            return buttonId;
        }
        
        /// <summary>
        /// Установить ID кнопки.
        /// Set button ID.
        /// </summary>
        public void SetButtonId(string id)
        {
            buttonId = id;
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupButton()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            currentState?.Dispose();
            isInteractable?.Dispose();
        }
        
        #endregion
    }
    
    #region Enums
    
    public enum ARButtonType
    {
        Action,
        Toggle,
        ColorPicker,
        Slider,
        Settings
    }
    
    public enum ARButtonState
    {
        Normal,
        Hovered,
        Pressed,
        Disabled
    }
    
    #endregion
}
