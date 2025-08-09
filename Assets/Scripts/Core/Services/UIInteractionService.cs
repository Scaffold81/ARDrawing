using UnityEngine;
using R3;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using System;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Сервис взаимодействия с UI элементами (заглушка для Phase 1).
    /// UI interaction service (stub for Phase 1).
    /// </summary>
    public class UIInteractionService : IUIInteractionService, IDisposable
    {
        private readonly Subject<Vector3> _uiCursorPosition = new();
        private readonly Subject<UIClickData> _uiClickEvents = new();
        private readonly Subject<UIHoverData> _uiHoverEvents = new();
        
        public Observable<Vector3> UICursorPosition => _uiCursorPosition.AsObservable();
        public Observable<UIClickData> UIClickEvents => _uiClickEvents.AsObservable();
        public Observable<UIHoverData> UIHoverEvents => _uiHoverEvents.AsObservable();
        
        /// <summary>
        /// Инициализация сервиса UI взаимодействий.
        /// Initialize UI interaction service.
        /// </summary>
        public UIInteractionService()
        {
            Debug.Log("UIInteractionService: Initialized (stub) / Инициализирован (заглушка)");
            
            // TODO Phase 4: Реальная реализация UI взаимодействий
            // TODO Phase 4: Real UI interaction implementation
        }
        
        public UIInteractionResult CheckUIInteraction(Vector3 worldPosition)
        {
            // TODO Phase 4: Реализация проверки взаимодействия с UI
            return new UIInteractionResult(false, "", 0f, Vector3.zero);
        }
        
        public void RegisterUIElement(IInteractableUI uiElement)
        {
            // TODO Phase 4: Реализация регистрации UI элементов
            Debug.Log($"UIInteractionService: RegisterUIElement {uiElement.ElementId}");
        }
        
        public void UnregisterUIElement(IInteractableUI uiElement)
        {
            // TODO Phase 4: Реализация отмены регистрации UI элементов
            Debug.Log($"UIInteractionService: UnregisterUIElement {uiElement.ElementId}");
        }
        
        public void Dispose()
        {
            _uiCursorPosition?.Dispose();
            _uiClickEvents?.Dispose();
            _uiHoverEvents?.Dispose();
        }
    }
}