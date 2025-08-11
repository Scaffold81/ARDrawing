using UnityEngine;
using R3;
using System;
using ARDrawing.Core.Models;

namespace ARDrawing.UI.Panels
{
    /// <summary>
    /// UIPanel - базовый класс для всех UI панелей в AR интерфейсе.
    /// Обеспечивает общую функциональность: показ/скрытие, анимации, состояние.
    /// UIPanel - base class for all UI panels in AR interface.
    /// Provides common functionality: show/hide, animations, state.
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("Panel Configuration")]
        [SerializeField] protected bool startVisible = false;
        [SerializeField] protected bool useAnimations = true;
        [SerializeField] protected float animationDuration = 0.3f;
        
        [Header("Animation Settings")]
        [SerializeField] protected AnimationType showAnimation = AnimationType.Scale;
        [SerializeField] protected AnimationType hideAnimation = AnimationType.Scale;
        [SerializeField] protected AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        // State Management
        protected readonly ReactiveProperty<bool> isVisible = new ReactiveProperty<bool>(false);
        protected readonly ReactiveProperty<PanelState> currentState = new ReactiveProperty<PanelState>(PanelState.Hidden);
        
        // Public Observables
        public Observable<bool> IsVisible => isVisible.AsObservable();
        public Observable<PanelState> CurrentState => currentState.AsObservable();
        
        // Components
        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;
        
        // Animation
        protected Coroutine currentAnimation;
        protected Vector3 originalScale;
        protected Vector3 originalPosition;
        
        // Events
        public event Action OnPanelShown;
        public event Action OnPanelHidden;
        public event Action<PanelState> OnStateChanged;
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            InitializeComponents();
        }
        
        protected virtual void Start()
        {
            InitializePanel();
            SubscribeToStateChanges();
            
            if (startVisible)
            {
                ShowPanel();
            }
            else
            {
                HidePanel(false);
            }
        }
        
        protected virtual void OnDestroy()
        {
            CleanupPanel();
        }
        
        #endregion
        
        #region Initialization
        
        protected virtual void InitializeComponents()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            rectTransform = GetComponent<RectTransform>();
            
            if (rectTransform != null)
            {
                originalScale = rectTransform.localScale;
                originalPosition = rectTransform.localPosition;
            }
        }
        
        protected virtual void InitializePanel()
        {
            // Override in derived classes for specific initialization
        }
        
        protected virtual void SubscribeToStateChanges()
        {
            currentState.Subscribe(OnStateChangedInternal);
        }
        
        protected virtual void OnStateChangedInternal(PanelState newState)
        {
            OnStateChanged?.Invoke(newState);
        }
        
        #endregion
        
        #region Panel Visibility
        
        /// <summary>
        /// Показать панель с анимацией.
        /// Show panel with animation.
        /// </summary>
        public virtual void ShowPanel(bool animated = true)
        {
            if (currentState.Value == PanelState.Showing || currentState.Value == PanelState.Visible)
                return;
                
            currentState.Value = PanelState.Showing;
            gameObject.SetActive(true);
            
            if (animated && useAnimations)
            {
                PlayShowAnimation();
            }
            else
            {
                SetVisibleImmediate();
            }
        }
        
        /// <summary>
        /// Скрыть панель с анимацией.
        /// Hide panel with animation.
        /// </summary>
        public virtual void HidePanel(bool animated = true)
        {
            if (currentState.Value == PanelState.Hiding || currentState.Value == PanelState.Hidden)
                return;
                
            currentState.Value = PanelState.Hiding;
            
            if (animated && useAnimations)
            {
                PlayHideAnimation();
            }
            else
            {
                SetHiddenImmediate();
            }
        }
        
        /// <summary>
        /// Переключить видимость панели.
        /// Toggle panel visibility.
        /// </summary>
        public virtual void TogglePanel(bool animated = true)
        {
            if (isVisible.Value)
            {
                HidePanel(animated);
            }
            else
            {
                ShowPanel(animated);
            }
        }
        
        protected virtual void SetVisibleImmediate()
        {
            isVisible.Value = true;
            currentState.Value = PanelState.Visible;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            if (rectTransform != null)
            {
                rectTransform.localScale = originalScale;
                rectTransform.localPosition = originalPosition;
            }
            
            OnPanelShown?.Invoke();
        }
        
        protected virtual void SetHiddenImmediate()
        {
            isVisible.Value = false;
            currentState.Value = PanelState.Hidden;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            
            gameObject.SetActive(false);
            OnPanelHidden?.Invoke();
        }
        
        #endregion
        
        #region Animation
        
        protected virtual void PlayShowAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            currentAnimation = StartCoroutine(ShowAnimationCoroutine());
        }
        
        protected virtual void PlayHideAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            currentAnimation = StartCoroutine(HideAnimationCoroutine());
        }
        
        protected virtual System.Collections.IEnumerator ShowAnimationCoroutine()
        {
            // Setup initial state
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            
            if (rectTransform != null && showAnimation == AnimationType.Scale)
            {
                rectTransform.localScale = Vector3.zero;
            }
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;
                float easedProgress = animationCurve.Evaluate(progress);
                
                // Animate based on type
                switch (showAnimation)
                {
                    case AnimationType.Fade:
                        if (canvasGroup != null)
                            canvasGroup.alpha = easedProgress;
                        break;
                        
                    case AnimationType.Scale:
                        if (canvasGroup != null)
                            canvasGroup.alpha = easedProgress;
                        if (rectTransform != null)
                            rectTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, easedProgress);
                        break;
                        
                    case AnimationType.Slide:
                        if (canvasGroup != null)
                            canvasGroup.alpha = easedProgress;
                        if (rectTransform != null)
                        {
                            Vector3 offscreenPos = originalPosition + Vector3.up * 100f;
                            rectTransform.localPosition = Vector3.Lerp(offscreenPos, originalPosition, easedProgress);
                        }
                        break;
                }
                
                yield return null;
            }
            
            // Ensure final state
            SetVisibleImmediate();
            currentAnimation = null;
        }
        
        protected virtual System.Collections.IEnumerator HideAnimationCoroutine()
        {
            float elapsed = 0f;
            
            Vector3 startScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
            Vector3 startPosition = rectTransform != null ? rectTransform.localPosition : Vector3.zero;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;
                float easedProgress = animationCurve.Evaluate(progress);
                
                // Animate based on type
                switch (hideAnimation)
                {
                    case AnimationType.Fade:
                        if (canvasGroup != null)
                            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, easedProgress);
                        break;
                        
                    case AnimationType.Scale:
                        if (canvasGroup != null)
                            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, easedProgress);
                        if (rectTransform != null)
                            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, easedProgress);
                        break;
                        
                    case AnimationType.Slide:
                        if (canvasGroup != null)
                            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, easedProgress);
                        if (rectTransform != null)
                        {
                            Vector3 offscreenPos = originalPosition + Vector3.up * 100f;
                            rectTransform.localPosition = Vector3.Lerp(startPosition, offscreenPos, easedProgress);
                        }
                        break;
                }
                
                yield return null;
            }
            
            // Ensure final state
            SetHiddenImmediate();
            currentAnimation = null;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Получить текущее состояние панели.
        /// Get current panel state.
        /// </summary>
        public PanelState GetCurrentState()
        {
            return currentState.Value;
        }
        
        /// <summary>
        /// Проверить, видима ли панель.
        /// Check if panel is visible.
        /// </summary>
        public bool GetIsVisible()
        {
            return isVisible.Value;
        }
        
        /// <summary>
        /// Установить тип анимации показа.
        /// Set show animation type.
        /// </summary>
        public void SetShowAnimation(AnimationType animation)
        {
            showAnimation = animation;
        }
        
        /// <summary>
        /// Установить тип анимации скрытия.
        /// Set hide animation type.
        /// </summary>
        public void SetHideAnimation(AnimationType animation)
        {
            hideAnimation = animation;
        }
        
        /// <summary>
        /// Установить длительность анимации.
        /// Set animation duration.
        /// </summary>
        public void SetAnimationDuration(float duration)
        {
            animationDuration = duration;
        }
        
        #endregion
        
        #region Cleanup
        
        protected virtual void CleanupPanel()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            isVisible?.Dispose();
            currentState?.Dispose();
        }
        
        #endregion
    }
    
    #region Enums
    
    public enum PanelState
    {
        Hidden,
        Showing,
        Visible,
        Hiding
    }
    
    public enum AnimationType
    {
        None,
        Fade,
        Scale,
        Slide
    }
    
    #endregion
}
