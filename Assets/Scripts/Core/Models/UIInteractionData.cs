using UnityEngine;

namespace ARDrawing.Core.Models
{
    /// <summary>
    /// Данные клика по UI элементу.
    /// UI element click data.
    /// </summary>
    [System.Serializable]
    public struct UIClickData
    {
        /// <summary>Позиция клика в мировых координатах / Click position in world coordinates</summary>
        public Vector3 worldPosition;
        
        /// <summary>UI элемент, по которому кликнули / Clicked UI element</summary>
        public string elementId;
        
        /// <summary>Тип клика (начало, конец) / Click type (start, end)</summary>
        public ClickType clickType;
        
        /// <summary>Временная метка / Timestamp</summary>
        public float timestamp;
        
        public UIClickData(Vector3 position, string id, ClickType type)
        {
            worldPosition = position;
            elementId = id;
            clickType = type;
            timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Данные наведения на UI элемент.
    /// UI element hover data.
    /// </summary>
    [System.Serializable]
    public struct UIHoverData
    {
        /// <summary>Позиция курсора / Cursor position</summary>
        public Vector3 cursorPosition;
        
        /// <summary>UI элемент под курсором / UI element under cursor</summary>
        public string elementId;
        
        /// <summary>Состояние наведения / Hover state</summary>
        public bool isHovering;
        
        /// <summary>Временная метка / Timestamp</summary>
        public float timestamp;
        
        public UIHoverData(Vector3 position, string id, bool hovering)
        {
            cursorPosition = position;
            elementId = id;
            isHovering = hovering;
            timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Результат взаимодействия с UI элементом.
    /// UI interaction result.
    /// </summary>
    [System.Serializable]
    public struct UIInteractionResult
    {
        /// <summary>Найден ли UI элемент для взаимодействия / UI element found for interaction</summary>
        public bool hasInteraction;
        
        /// <summary>Идентификатор UI элемента / UI element identifier</summary>
        public string elementId;
        
        /// <summary>Расстояние до UI элемента / Distance to UI element</summary>
        public float distance;
        
        /// <summary>Позиция точки взаимодействия / Interaction point position</summary>
        public Vector3 interactionPoint;
        
        public UIInteractionResult(bool interaction, string id, float dist, Vector3 point)
        {
            hasInteraction = interaction;
            elementId = id;
            distance = dist;
            interactionPoint = point;
        }
    }
    
    /// <summary>
    /// Тип клика по UI элементу.
    /// UI element click type.
    /// </summary>
    public enum ClickType
    {
        /// <summary>Начало клика / Click start</summary>
        Started,
        
        /// <summary>Конец клика / Click end</summary>
        Ended,
        
        /// <summary>Клик в процессе / Click ongoing</summary>
        Ongoing
    }
    
    /// <summary>
    /// Интерфейс для интерактивных UI элементов.
    /// Interface for interactive UI elements.
    /// </summary>
    public interface IInteractableUI
    {
        /// <summary>Уникальный идентификатор элемента / Unique element identifier</summary>
        string ElementId { get; }
        
        /// <summary>Позиция элемента в мировых координатах / Element position in world coordinates</summary>
        Vector3 WorldPosition { get; }
        
        /// <summary>Размер области взаимодействия / Interaction area size</summary>
        Vector3 InteractionBounds { get; }
        
        /// <summary>Активен ли элемент для взаимодействия / Is element active for interaction</summary>
        bool IsInteractable { get; }
        
        /// <summary>
        /// Вызывается при начале взаимодействия.
        /// Called when interaction starts.
        /// </summary>
        void OnInteractionStart(Vector3 interactionPoint);
        
        /// <summary>
        /// Вызывается при завершении взаимодействия.
        /// Called when interaction ends.
        /// </summary>
        void OnInteractionEnd(Vector3 interactionPoint);
        
        /// <summary>
        /// Вызывается при наведении курсора.
        /// Called when cursor hovers.
        /// </summary>
        void OnHoverStart(Vector3 cursorPosition);
        
        /// <summary>
        /// Вызывается при уходе курсора.
        /// Called when cursor leaves.
        /// </summary>
        void OnHoverEnd();
    }
}