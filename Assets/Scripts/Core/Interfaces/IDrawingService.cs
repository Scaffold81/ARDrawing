using UnityEngine;
using R3;
using System.Collections.Generic;
using ARDrawing.Core.Models;

namespace ARDrawing.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса рисования линий в AR пространстве.
    /// Interface for drawing lines service in AR space.
    /// </summary>
    public interface IDrawingService
    {
        /// <summary>
        /// Observable поток активных линий рисования.
        /// Observable stream of active drawing lines.
        /// </summary>
        Observable<List<DrawingLine>> ActiveLines { get; }
        
        /// <summary>
        /// Observable поток состояния рисования (активно/неактивно).
        /// Observable stream of drawing state (active/inactive).
        /// </summary>
        Observable<bool> IsDrawing { get; }
        
        /// <summary>
        /// Начинает новую линию рисования в указанной позиции.
        /// Starts a new drawing line at the specified position.
        /// </summary>
        /// <param name="position">Начальная позиция в мировых координатах / Starting position in world coordinates</param>
        void StartLine(Vector3 position);
        
        /// <summary>
        /// Добавляет точку к текущей активной линии.
        /// Adds a point to the current active line.
        /// </summary>
        /// <param name="position">Позиция точки в мировых координатах / Point position in world coordinates</param>
        void AddPointToLine(Vector3 position);
        
        /// <summary>
        /// Завершает текущую линию рисования.
        /// Finishes the current drawing line.
        /// </summary>
        void EndLine();
        
        /// <summary>
        /// Очищает все нарисованные линии.
        /// Clears all drawn lines.
        /// </summary>
        void ClearAllLines();
        
        /// <summary>
        /// Устанавливает настройки рисования (цвет, толщина).
        /// Sets drawing settings (color, thickness).
        /// </summary>
        /// <param name="settings">Настройки рисования / Drawing settings</param>
        void SetDrawingSettings(DrawingSettings settings);
    }
}