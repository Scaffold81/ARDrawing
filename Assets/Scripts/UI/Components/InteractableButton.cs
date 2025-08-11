using UnityEngine;
using UnityEngine.Events;
using R3;
using System;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;

namespace ARDrawing.UI.Components
{
    /// <summary>
    /// InteractableButton - AR кнопка с finger tracking поддержкой.
    /// Обеспечивает touch detection, visual feedback и события взаимодействия.
    /// InteractableButton - AR button with finger tracking support.
    /// Provides touch detection, visual feedback and interaction events.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractableButton : MonoBehaviour
    {
        [Header("Button Settings")]
        [SerializeField] private string buttonId = "Button";
        [SerializeField] private ButtonType buttonType = ButtonType.Action;
        [SerializeField] private bool isEnabled = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject visualElement;
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material hoverMaterial;
        [SerializeField] private Material pressedMaterial;
        [SerializeField] private Material disabledMaterial;
        
        [Header("Animation Settings")]
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private float animationDuration = 0.2f;
        
        [Header("Touch Detection")]
        [SerializeField] private float touchDistance = 0.02f; // 2cm
        [SerializeField] private float hoverDistance = 0.05f; // 5cm
        [SerializeField] private LayerMask fingerLayer = -1;
        
        [Header("Audio Feedback")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private float audioVolume = 0.5f;
        
        [Header("Events")]
        public UnityEvent OnButtonPressed;
        public UnityEvent OnButtonReleased;
        public UnityEvent<bool> OnHoverChanged;
        
        // Reactive Properties
        private ReactiveProperty<ButtonState> currentState;
        private ReactiveProperty<bool> isHovered;
        private ReactiveProperty<bool> isPressed;
        
        // Disposal tracking
        private bool _isDisposed = false;
        
        // Public Observables
        public Observable<ButtonState> State => currentState?.AsObservable() ?? Observable.Empty<ButtonState>();
        public Observable<bool> IsHovered => isHovered?.AsObservable() ?? Observable.Empty<bool>();
        public Observable<bool> IsPressed => isPressed?.AsObservable() ?? Observable.Empty<bool>();
        
        // Components
        private Collider buttonCollider;
        private AudioSource audioSource;
        private Vector3 originalScale;
        
        // Interaction State
        private bool fingerInRange = false;
        private Vector3 lastFingerPosition;
        private float lastInteractionTime;
        
        // Animation
        private Coroutine currentAnimation;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
            SetupMaterials();
        }
        
        private void Start()
        {
            InitializeButton();
            SubscribeToStateChanges();
        }
        
        private void Update()
        {
            if (isEnabled)
            {
                UpdateInteraction();
            }
        }
        
        private void OnDestroy()
        {
            CleanupButton();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            buttonCollider = GetComponent<Collider>();
            
            if (buttonRenderer == null)
                buttonRenderer = GetComponent<Renderer>();
                
            if (visualElement == null)
                visualElement = gameObject;
                
            originalScale = visualElement.transform.localScale;
            
            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = audioVolume;
            }
        }
        
        private void SetupMaterials()
        {
            if (buttonRenderer != null && normalMaterial == null)
            {
                // Create default materials if not assigned
                normalMaterial = CreateDefaultMaterial(Color.white);
                hoverMaterial = CreateDefaultMaterial(Color.cyan);
                pressedMaterial = CreateDefaultMaterial(Color.green);
                disabledMaterial = CreateDefaultMaterial(Color.gray);
            }
        }
        
        private Material CreateDefaultMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.name = $"ButtonMaterial_{color.ToString()}";
            return material;
        }
        
        private void InitializeButton()
        {
            if (_isDisposed) return;
            
            // Initialize reactive properties
            currentState = new ReactiveProperty<ButtonState>(ButtonState.Normal);
            isHovered = new ReactiveProperty<bool>(false);
            isPressed = new ReactiveProperty<bool>(false);
            
            UpdateVisualState();
            
            Debug.Log($"[InteractableButton] {buttonId} initialized - Type: {buttonType}");
        }
        
        #endregion
        
        #region State Management
        
        private void SubscribeToStateChanges()
        {
            // Subscribe to state changes for visual updates
            currentState.Subscribe(OnStateChanged);
            isHovered.Subscribe(OnHoverStateChanged);
            isPressed.Subscribe(OnPressStateChanged);
        }
        
        private void OnStateChanged(ButtonState newState)
        {
            UpdateVisualState();
            
            switch (newState)
            {
                case ButtonState.Hovered:
                    PlayAudio(hoverSound);
                    if (enableAnimations)
                        AnimateScale(hoverScale);
                    break;
                    
                case ButtonState.Pressed:
                    PlayAudio(clickSound);
                    if (enableAnimations)
                        AnimateScale(pressedScale);
                    OnButtonPressed?.Invoke();
                    break;
                    
                case ButtonState.Normal:
                    if (enableAnimations)
                        AnimateScale(1.0f);
                    break;
            }
        }
        
        private void OnHoverStateChanged(bool hovered)
        {
            OnHoverChanged?.Invoke(hovered);
        }
        
        private void OnPressStateChanged(bool pressed)
        {
            if (!pressed)
            {
                OnButtonReleased?.Invoke();
            }
        }
        
        private void UpdateVisualState()
        {
            if (buttonRenderer == null) return;
            
            if (!isEnabled)
            {
                buttonRenderer.material = disabledMaterial;
                return;
            }
            
            switch (currentState.Value)
            {
                case ButtonState.Normal:
                    buttonRenderer.material = normalMaterial;
                    break;
                case ButtonState.Hovered:
                    buttonRenderer.material = hoverMaterial;
                    break;
                case ButtonState.Pressed:
                    buttonRenderer.material = pressedMaterial;
                    break;
                case ButtonState.Disabled:
                    buttonRenderer.material = disabledMaterial;
                    break;
            }
        }
        
        #endregion
        
        #region Interaction Detection
        
        private void UpdateInteraction()
        {
            // Simple finger detection - можно заменить на HandTrackingService
            DetectFingerInteraction();
        }
        
        private void DetectFingerInteraction()
        {
            // Simplified finger detection using mouse for testing
            // В production заменить на HandTrackingService
            
            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, fingerLayer))
                {
                    if (hit.collider == buttonCollider)
                    {
                        HandleFingerInteraction(hit.point, true);
                        return;
                    }
                }
            }
            
            // Check hover without press
            Ray hoverRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(hoverRay, out RaycastHit hoverHit, Mathf.Infinity))
            {
                if (hoverHit.collider == buttonCollider)
                {
                    float distance = Vector3.Distance(hoverHit.point, transform.position);
                    if (distance <= hoverDistance)
                    {
                        HandleFingerInteraction(hoverHit.point, false);
                        return;
                    }
                }
            }
            
            // No interaction
            HandleNoInteraction();
        }
        
        private void HandleFingerInteraction(Vector3 fingerPosition, bool isTouch)
        {
            fingerInRange = true;
            lastFingerPosition = fingerPosition;
            lastInteractionTime = Time.time;
            
            float distanceToButton = Vector3.Distance(fingerPosition, transform.position);
            
            if (isTouch && distanceToButton <= touchDistance)
            {
                // Touch detected
                if (!isPressed.Value)
                {
                    isPressed.Value = true;
                    currentState.Value = ButtonState.Pressed;
                }
            }
            else if (distanceToButton <= hoverDistance)
            {
                // Hover detected
                if (!isHovered.Value)
                {
                    isHovered.Value = true;
                    currentState.Value = ButtonState.Hovered;
                }
            }
        }
        
        private void HandleNoInteraction()
        {
            if (fingerInRange)
            {
                fingerInRange = false;
                isHovered.Value = false;
                isPressed.Value = false;
                currentState.Value = ButtonState.Normal;
            }
        }
        
        #endregion
        
        #region Animation
        
        private void AnimateScale(float targetScale)
        {
            if (!enableAnimations) return;
            
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            currentAnimation = StartCoroutine(AnimateScaleCoroutine(targetScale));
        }
        
        private System.Collections.IEnumerator AnimateScaleCoroutine(float targetScale)
        {
            Vector3 startScale = visualElement.transform.localScale;
            Vector3 endScale = originalScale * targetScale;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;
                
                // Smooth animation curve
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                
                visualElement.transform.localScale = Vector3.Lerp(startScale, endScale, easedProgress);
                
                yield return null;
            }
            
            visualElement.transform.localScale = endScale;
            currentAnimation = null;
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
        /// Programmatically press the button.
        /// </summary>
        public void PressButton()
        {
            if (!isEnabled) return;
            
            isPressed.Value = true;
            currentState.Value = ButtonState.Pressed;
            
            // Auto-release after short delay
            Invoke(nameof(ReleaseButton), 0.1f);
        }
        
        /// <summary>
        /// Программно отпустить кнопку.
        /// Programmatically release the button.
        /// </summary>
        public void ReleaseButton()
        {
            isPressed.Value = false;
            currentState.Value = fingerInRange ? ButtonState.Hovered : ButtonState.Normal;
        }
        
        /// <summary>
        /// Включить/выключить кнопку.
        /// Enable/disable the button.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            currentState.Value = enabled ? ButtonState.Normal : ButtonState.Disabled;
            buttonCollider.enabled = enabled;
        }
        
        /// <summary>
        /// Установить визуальные материалы.
        /// Set visual materials.
        /// </summary>
        public void SetMaterials(Material normal, Material hover, Material pressed, Material disabled)
        {
            normalMaterial = normal;
            hoverMaterial = hover;
            pressedMaterial = pressed;
            disabledMaterial = disabled;
            
            UpdateVisualState();
        }
        
        /// <summary>
        /// Получить информацию о состоянии кнопки.
        /// Get button state information.
        /// </summary>
        public string GetButtonInfo()
        {
            return $"Button {buttonId}:\n" +
                   $"- Type: {buttonType}\n" +
                   $"- State: {currentState.Value}\n" +
                   $"- Enabled: {isEnabled}\n" +
                   $"- Hovered: {isHovered.Value}\n" +
                   $"- Pressed: {isPressed.Value}\n" +
                   $"- Last Interaction: {lastInteractionTime}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupButton()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            // Stop any running animations
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
            
            // Dispose reactive properties safely
            DisposeReactiveProperty(ref currentState, "CurrentState");
            DisposeReactiveProperty(ref isHovered, "IsHovered");
            DisposeReactiveProperty(ref isPressed, "IsPressed");
        }
        
        private void DisposeReactiveProperty<T>(ref ReactiveProperty<T> property, string name)
        {
            if (property != null)
            {
                try
                {
                    if (!property.IsDisposed)
                    {
                        property.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[InteractableButton] Error disposing {name}: {ex.Message}");
                }
                finally
                {
                    property = null;
                }
            }
        }
        
        #endregion
    }
    
    #region Enums
    
    public enum ButtonType
    {
        Action,      // Простое действие
        Toggle,      // Переключатель
        ColorPicker, // Выбор цвета
        Slider       // Слайдер
    }
    
    public enum ButtonState
    {
        Normal,
        Hovered,
        Pressed,
        Disabled
    }
    
    #endregion
}
