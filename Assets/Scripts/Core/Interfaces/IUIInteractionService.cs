using UnityEngine;
using R3;
using ARDrawing.Core.Models;

namespace ARDrawing.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса взаимодействия с UI элементами через hand tracking.
    /// Interface for UI interaction service through hand tracking.
    /// </summary>
    public interface IUIInteractionService
    {
        /// <summary>
        /// Observable поток позиции курсора для UI взаимодействия.
        /// Observable stream of cursor position for UI interaction.
        /// </summary>
        Observable<Vector3> UICursorPosition { get; }
        
        /// <summary>
        /// Observable поток событий нажатия на UI элементы.
        /// Observable stream of UI element click events.
        /// </summary>
        Observable<UIClickData> UIClickEvents { get; }
        
        /// <summary>
        /// Observable поток состояния наведения на UI элементы.
        /// Observable stream of UI element hover state.
        /// </summary>
        Observable<UIHoverData> UIHoverEvents { get; }
        
        /// <summary>
        /// Проверяет взаимодействие с UI элементом в указанной позиции.
        /// Checks interaction with UI element at specified position.
        /// </summary>
        /// <param name="worldPosition">Позиция в мировых координатах / Position in world coordinates</param>
        /// <returns>Данные о взаимодействии с UI / UI interaction data</returns>
        UIInteractionResult CheckUIInteraction(Vector3 worldPosition);
        
        /// <summary>
        /// Регистрирует UI элемент для взаимодействия.
        /// Registers UI element for interaction.
        /// </summary>
        /// <param name="uiElement">UI элемент / UI element</param>
        void RegisterUIElement(IInteractableUI uiElement);
        
        /// <summary>
        /// Отменяет регистрацию UI элемента.
        /// Unregisters UI element.
        /// </summary>
        /// <param name="uiElement">UI элемент / UI element</param>
        void UnregisterUIElement(IInteractableUI uiElement);
    }
}