using System.Collections.Generic;

namespace ARDrawing.Core.Models
{
    /// <summary>
    /// Результат операции сохранения.
    /// Save operation result.
    /// </summary>
    public struct SaveResult
    {
        /// <summary>Успешность операции / Operation success</summary>
        public bool success;
        
        /// <summary>Сообщение об ошибке (если есть) / Error message (if any)</summary>
        public string errorMessage;
        
        /// <summary>Путь к сохраненному файлу / Path to saved file</summary>
        public string filePath;
        
        /// <summary>Размер файла в байтах / File size in bytes</summary>
        public long fileSize;
        
        public SaveResult(bool isSuccess, string error = "", string path = "", long size = 0)
        {
            success = isSuccess;
            errorMessage = error;
            filePath = path;
            fileSize = size;
        }
        
        public static SaveResult Success(string filePath, long fileSize) => 
            new SaveResult(true, "", filePath, fileSize);
            
        public static SaveResult Failure(string errorMessage) => 
            new SaveResult(false, errorMessage);
    }
    
    /// <summary>
    /// Результат операции загрузки с данными.
    /// Load operation result with data.
    /// </summary>
    /// <typeparam name="T">Тип загружаемых данных / Type of loaded data</typeparam>
    public struct LoadResult<T>
    {
        /// <summary>Успешность операции / Operation success</summary>
        public bool success;
        
        /// <summary>Загруженные данные / Loaded data</summary>
        public T data;
        
        /// <summary>Сообщение об ошибке (если есть) / Error message (if any)</summary>
        public string errorMessage;
        
        /// <summary>Путь к загруженному файлу / Path to loaded file</summary>
        public string filePath;
        
        public LoadResult(bool isSuccess, T loadedData, string error = "", string path = "")
        {
            success = isSuccess;
            data = loadedData;
            errorMessage = error;
            filePath = path;
        }
        
        public static LoadResult<T> Success(T data, string filePath) => 
            new LoadResult<T>(true, data, "", filePath);
            
        public static LoadResult<T> Failure(string errorMessage) => 
            new LoadResult<T>(false, default(T), errorMessage);
    }
    
    /// <summary>
    /// Сериализуемая структура для сохранения рисунка в JSON.
    /// Serializable structure for saving drawing to JSON.
    /// </summary>
    [System.Serializable]
    public class DrawingSaveData
    {
        /// <summary>Версия формата сохранения / Save format version</summary>
        public string version = "1.0";
        
        /// <summary>Время создания сохранения / Save creation time</summary>
        public string creationTime;
        
        /// <summary>Название рисунка / Drawing name</summary>
        public string drawingName;
        
        /// <summary>Список линий рисования / List of drawing lines</summary>
        public List<SerializableDrawingLine> lines;
        
        /// <summary>Настройки, использованные при рисовании / Settings used during drawing</summary>
        public DrawingSettings settings;
        
        public DrawingSaveData()
        {
            creationTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lines = new List<SerializableDrawingLine>();
            settings = DrawingSettings.Default;
        }
    }
    
    /// <summary>
    /// Сериализуемая версия DrawingLine для JSON сохранения.
    /// Serializable version of DrawingLine for JSON saving.
    /// </summary>
    [System.Serializable]
    public class SerializableDrawingLine
    {
        /// <summary>Список точек в виде Vector3 / List of points as Vector3</summary>
        public List<SerializableVector3> points;
        
        /// <summary>Цвет линии / Line color</summary>
        public SerializableColor color;
        
        /// <summary>Толщина линии / Line thickness</summary>
        public float thickness;
        
        /// <summary>Время создания / Creation time</summary>
        public float creationTime;
        
        /// <summary>Идентификатор линии / Line identifier</summary>
        public string lineId;
        
        public SerializableDrawingLine() 
        {
            points = new List<SerializableVector3>();
        }
        
        public SerializableDrawingLine(DrawingLine originalLine)
        {
            points = new List<SerializableVector3>();
            foreach (var point in originalLine.points)
            {
                points.Add(new SerializableVector3(point));
            }
            color = new SerializableColor(originalLine.color);
            thickness = originalLine.thickness;
            creationTime = originalLine.creationTime;
            lineId = originalLine.lineId;
        }
        
        public DrawingLine ToDrawingLine()
        {
            var line = new DrawingLine(color.ToColor(), thickness);
            line.creationTime = creationTime;
            line.lineId = lineId;
            line.points.Clear();
            
            foreach (var point in points)
            {
                line.points.Add(point.ToVector3());
            }
            
            return line;
        }
    }
    
    /// <summary>
    /// Сериализуемая версия Vector3.
    /// Serializable version of Vector3.
    /// </summary>
    [System.Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;
        
        public SerializableVector3(UnityEngine.Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
        
        public UnityEngine.Vector3 ToVector3() => new UnityEngine.Vector3(x, y, z);
    }
    
    /// <summary>
    /// Сериализуемая версия Color.
    /// Serializable version of Color.
    /// </summary>
    [System.Serializable]
    public struct SerializableColor
    {
        public float r, g, b, a;
        
        public SerializableColor(UnityEngine.Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }
        
        public UnityEngine.Color ToColor() => new UnityEngine.Color(r, g, b, a);
    }
}