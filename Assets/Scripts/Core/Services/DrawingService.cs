using UnityEngine;
using R3;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Models;
using System.Collections.Generic;
using System;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Сервис рисования линий в AR пространстве (заглушка для Phase 1).
    /// Drawing lines service in AR space (stub for Phase 1).
    /// </summary>
    public class DrawingService : IDrawingService, IDisposable
    {
        private readonly Subject<List<DrawingLine>> _activeLines = new();
        private readonly Subject<bool> _isDrawing = new();
        private readonly List<DrawingLine> _lines = new();
        private DrawingSettings _settings;
        
        public Observable<List<DrawingLine>> ActiveLines => _activeLines.AsObservable();
        public Observable<bool> IsDrawing => _isDrawing.AsObservable();
        
        /// <summary>
        /// Инициализация сервиса рисования.
        /// Initialize drawing service.
        /// </summary>
        public DrawingService()
        {
            _settings = DrawingSettings.Default;
            Debug.Log("DrawingService: Initialized (stub) / Инициализирован (заглушка)");
            
            // TODO Phase 3: Реальная реализация рисования с пулингом
            // TODO Phase 3: Real drawing implementation with pooling
        }
        
        public void StartLine(Vector3 position)
        {
            // TODO Phase 3: Реализация начала линии
            Debug.Log($"DrawingService: StartLine at {position}");
        }
        
        public void AddPointToLine(Vector3 position)
        {
            // TODO Phase 3: Реализация добавления точки
        }
        
        public void EndLine()
        {
            // TODO Phase 3: Реализация завершения линии
            Debug.Log("DrawingService: EndLine");
        }
        
        public void ClearAllLines()
        {
            _lines.Clear();
            _activeLines.OnNext(new List<DrawingLine>(_lines));
            Debug.Log("DrawingService: ClearAllLines");
        }
        
        public void SetDrawingSettings(DrawingSettings settings)
        {
            _settings = settings;
            Debug.Log($"DrawingService: Settings updated - Color: {settings.lineColor}, Thickness: {settings.lineThickness}");
        }
        
        public void Dispose()
        {
            _activeLines?.Dispose();
            _isDrawing?.Dispose();
        }
    }
}