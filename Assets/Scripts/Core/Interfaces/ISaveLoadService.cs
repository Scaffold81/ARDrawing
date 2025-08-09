using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using ARDrawing.Core.Models;

namespace ARDrawing.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса сохранения/загрузки рисунков в JSON формате.
    /// Interface for save/load service for drawings in JSON format.
    /// </summary>
    public interface ISaveLoadService
    {
        /// <summary>
        /// Асинхронно сохраняет рисунок в файл.
        /// Asynchronously saves drawing to file.
        /// </summary>
        /// <param name="lines">Список линий для сохранения / List of lines to save</param>
        /// <param name="fileName">Имя файла / File name</param>
        /// <returns>Результат операции сохранения / Save operation result</returns>
        UniTask<SaveResult> SaveDrawingAsync(List<DrawingLine> lines, string fileName);
        
        /// <summary>
        /// Асинхронно загружает рисунок из файла.
        /// Asynchronously loads drawing from file.
        /// </summary>
        /// <param name="fileName">Имя файла / File name</param>
        /// <returns>Загруженные линии или результат ошибки / Loaded lines or error result</returns>
        UniTask<LoadResult<List<DrawingLine>>> LoadDrawingAsync(string fileName);
        
        /// <summary>
        /// Асинхронно получает список доступных сохранений.
        /// Asynchronously gets list of available saves.
        /// </summary>
        /// <returns>Список имен файлов сохранений / List of save file names</returns>
        UniTask<List<string>> GetAvailableSavesAsync();
        
        /// <summary>
        /// Асинхронно удаляет сохранение.
        /// Asynchronously deletes a save.
        /// </summary>
        /// <param name="fileName">Имя файла для удаления / File name to delete</param>
        /// <returns>Результат операции удаления / Delete operation result</returns>
        UniTask<bool> DeleteSaveAsync(string fileName);
        
        /// <summary>
        /// Проверяет существование файла сохранения.
        /// Checks if save file exists.
        /// </summary>
        /// <param name="fileName">Имя файла / File name</param>
        /// <returns>True если файл существует / True if file exists</returns>
        bool SaveExists(string fileName);
    }
}