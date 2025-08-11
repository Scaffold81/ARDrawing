using Cysharp.Threading.Tasks;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Реальная реализация сервиса сохранения/загрузки рисунков в JSON формате.
    /// Real implementation of save/load service for drawings in JSON format.
    /// </summary>
    public class JsonSaveLoadService : ISaveLoadService
    {
        private readonly string saveDirectory;
        private readonly string fileExtension = ".json";
        private bool enableDebugLog = true;
        
        /// <summary>
        /// Инициализация сервиса с автоматическим созданием директории.
        /// Initialize service with automatic directory creation.
        /// </summary>
        public JsonSaveLoadService()
        {
            saveDirectory = Path.Combine(Application.persistentDataPath, "ARDrawings");
            
            // Создаем директорию если не существует
            CreateSaveDirectoryIfNotExists();
            
            if (enableDebugLog)
                Debug.Log($"[JsonSaveLoadService] Initialized. Save directory: {saveDirectory}");
        }
        
        private void CreateSaveDirectoryIfNotExists()
        {
            try
            {
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                    if (enableDebugLog)
                        Debug.Log($"[JsonSaveLoadService] Created save directory: {saveDirectory}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadService] Failed to create save directory: {ex.Message}");
            }
        }
        
        public async UniTask<SaveResult> SaveDrawingAsync(List<DrawingLine> lines, string fileName)
        {
            try
            {
                if (enableDebugLog)
                    Debug.Log($"[JsonSaveLoadService] Starting save: {fileName} with {lines.Count} lines");
                
                // Валидация входных данных
                if (lines == null)
                    return SaveResult.Failure("Lines list is null");
                    
                if (string.IsNullOrEmpty(fileName))
                    return SaveResult.Failure("File name is empty");
                
                // Очистка имени файла
                string cleanFileName = SanitizeFileName(fileName);
                string fullPath = Path.Combine(saveDirectory, cleanFileName + fileExtension);
                
                // Создание данных для сериализации
                var saveData = new DrawingSaveData
                {
                    version = "1.0",
                    creationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    drawingName = cleanFileName,
                    lines = new List<SerializableDrawingLine>(),
                    settings = DrawingSettings.Default
                };
                
                // Конвертация линий в сериализуемый формат
                foreach (var line in lines)
                {
                    if (line != null && line.Points.Count > 0)
                    {
                        saveData.lines.Add(new SerializableDrawingLine(line));
                    }
                }
                
                // Сериализация в JSON
                string jsonContent = JsonUtility.ToJson(saveData, true);
                
                // Асинхронная запись файла
                await WriteFileAsync(fullPath, jsonContent);
                
                // Получение размера файла
                var fileInfo = new FileInfo(fullPath);
                long fileSize = fileInfo.Exists ? fileInfo.Length : 0;
                
                if (enableDebugLog)
                    Debug.Log($"[JsonSaveLoadService] Successfully saved: {fullPath} ({fileSize} bytes)");
                
                return SaveResult.Success(fullPath, fileSize);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadService] Save failed: {ex.Message}");
                return SaveResult.Failure($"Save failed: {ex.Message}");
            }
        }
        
        public async UniTask<LoadResult<List<DrawingLine>>> LoadDrawingAsync(string fileName)
        {
            try
            {
                if (enableDebugLog)
                    Debug.Log($"[JsonSaveLoadService] Loading: {fileName}");
                
                // Валидация имени файла
                if (string.IsNullOrEmpty(fileName))
                    return LoadResult<List<DrawingLine>>.Failure("File name is empty");
                
                string cleanFileName = SanitizeFileName(fileName);
                string fullPath = Path.Combine(saveDirectory, cleanFileName + fileExtension);
                
                // Проверка существования файла
                if (!File.Exists(fullPath))
                    return LoadResult<List<DrawingLine>>.Failure($"File not found: {fullPath}");
                
                // Асинхронное чтение файла
                string jsonContent = await ReadFileAsync(fullPath);
                
                if (string.IsNullOrEmpty(jsonContent))
                    return LoadResult<List<DrawingLine>>.Failure("File is empty or corrupted");
                
                // Десериализация JSON
                var saveData = JsonUtility.FromJson<DrawingSaveData>(jsonContent);
                
                if (saveData == null)
                    return LoadResult<List<DrawingLine>>.Failure("Failed to parse JSON data");
                
                // Конвертация в DrawingLine объекты
                var loadedLines = new List<DrawingLine>();
                
                if (saveData.lines != null)
                {
                    foreach (var serializableLine in saveData.lines)
                    {
                        if (serializableLine != null && serializableLine.points.Count > 0)
                        {
                            var drawingLine = serializableLine.ToDrawingLine();
                            loadedLines.Add(drawingLine);
                        }
                    }
                }
                
                if (enableDebugLog)
                {
                    Debug.Log($"[JsonSaveLoadService] Successfully loaded: {fileName}");
                    Debug.Log($"[JsonSaveLoadService] Loaded {loadedLines.Count} lines from version {saveData.version}");
                }
                
                return LoadResult<List<DrawingLine>>.Success(loadedLines, fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadService] Load failed: {ex.Message}");
                return LoadResult<List<DrawingLine>>.Failure($"Load failed: {ex.Message}");
            }
        }
        
        public async UniTask<List<string>> GetAvailableSavesAsync()
        {
            try
            {
                await UniTask.SwitchToThreadPool(); // Переключение на background thread
                
                var saveFiles = new List<string>();
                
                if (!Directory.Exists(saveDirectory))
                {
                    if (enableDebugLog)
                        Debug.Log("[JsonSaveLoadService] Save directory doesn't exist yet");
                    return saveFiles;
                }
                
                // Поиск всех .json файлов
                var files = Directory.GetFiles(saveDirectory, "*" + fileExtension);
                
                foreach (var filePath in files)
                {
                    // Извлекаем имя файла без расширения
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    saveFiles.Add(fileName);
                }
                
                // Сортировка по времени изменения (новые первые)
                saveFiles.Sort((a, b) => 
                {
                    var fileA = Path.Combine(saveDirectory, a + fileExtension);
                    var fileB = Path.Combine(saveDirectory, b + fileExtension);
                    return File.GetLastWriteTime(fileB).CompareTo(File.GetLastWriteTime(fileA));
                });
                
                await UniTask.SwitchToMainThread(); // Возврат на main thread
                
                if (enableDebugLog)
                    Debug.Log($"[JsonSaveLoadService] Found {saveFiles.Count} save files");
                
                return saveFiles;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadService] GetAvailableSaves failed: {ex.Message}");
                return new List<string>();
            }
        }
        
        public async UniTask<bool> DeleteSaveAsync(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return false;
                
                string cleanFileName = SanitizeFileName(fileName);
                string fullPath = Path.Combine(saveDirectory, cleanFileName + fileExtension);
                
                if (!File.Exists(fullPath))
                {
                    if (enableDebugLog)
                        Debug.LogWarning($"[JsonSaveLoadService] File not found for deletion: {fullPath}");
                    return false;
                }
                
                await UniTask.SwitchToThreadPool();
                
                File.Delete(fullPath);
                
                await UniTask.SwitchToMainThread();
                
                if (enableDebugLog)
                    Debug.Log($"[JsonSaveLoadService] Successfully deleted: {fullPath}");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadService] Delete failed: {ex.Message}");
                return false;
            }
        }
        
        public bool SaveExists(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return false;
                
                string cleanFileName = SanitizeFileName(fileName);
                string fullPath = Path.Combine(saveDirectory, cleanFileName + fileExtension);
                
                return File.Exists(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadService] SaveExists check failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Получить полный путь к директории сохранений.
        /// Get full path to save directory.
        /// </summary>
        public string GetSaveDirectory() => saveDirectory;
        
        /// <summary>
        /// Получить информацию о размере директории сохранений.
        /// Get save directory size information.
        /// </summary>
        public async UniTask<long> GetSaveDirectorySizeAsync()
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                
                if (!Directory.Exists(saveDirectory))
                    return 0;
                
                var files = Directory.GetFiles(saveDirectory, "*" + fileExtension);
                long totalSize = 0;
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;
                }
                
                await UniTask.SwitchToMainThread();
                
                return totalSize;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveLoadService] GetSaveDirectorySize failed: {ex.Message}");
                return 0;
            }
        }
        
        private async UniTask WriteFileAsync(string filePath, string content)
        {
            await UniTask.SwitchToThreadPool();
            
            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(content);
            }
            
            await UniTask.SwitchToMainThread();
        }
        
        private async UniTask<string> ReadFileAsync(string filePath)
        {
            await UniTask.SwitchToThreadPool();
            
            string content;
            using (var reader = new StreamReader(filePath))
            {
                content = await reader.ReadToEndAsync();
            }
            
            await UniTask.SwitchToMainThread();
            return content;
        }
        
        private string SanitizeFileName(string fileName)
        {
            // Удаляем недопустимые символы из имени файла
            var invalidChars = Path.GetInvalidFileNameChars();
            
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            
            // Ограничиваем длину
            if (fileName.Length > 50)
            {
                fileName = fileName.Substring(0, 50);
            }
            
            return fileName;
        }
    }
}