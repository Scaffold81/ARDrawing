using Zenject;
using ARDrawing.Core.Models;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Фабрика для создания объектов DrawingLine (заглушка для Phase 1).
    /// Factory for creating DrawingLine objects (stub for Phase 1).
    /// </summary>
    public class DrawingLineFactory : IFactory<DrawingLine>
    {
        /// <summary>
        /// Создает новую линию рисования с настройками по умолчанию.
        /// Creates new drawing line with default settings.
        /// </summary>
        public DrawingLine Create()
        {
            return new DrawingLine();
        }
    }
}