using UnityEngine;
using R3;
using ARDrawing.Core.Models;
using System;

namespace ARDrawing.Core.Services
{
    /// <summary>
    /// Менеджер состояний касания с улучшенной логикой определения pinch жеста.
    /// Touch state manager with improved pinch gesture detection logic.
    /// </summary>
    public class TouchStateManager : IDisposable
    {
        // Настройки определения касания / Touch detection settings
        private TouchDetectionSettings _settings;
        
        // R3 Observable потоки / R3 Observable streams
        private readonly Subject<TouchEventData> _touchEvents = new Subject<TouchEventData>();
        private readonly Subject<TouchState> _touchStateChanged = new Subject<TouchState>();
        
        // Внутреннее состояние / Internal state
        private TouchState _currentState = TouchState.None;
        private float _touchStartTime = 0f;
        private float _lastTouchEndTime = 0f;
        private float _smoothedDistance = 0f;
        
        // История для стабильности / History for stability
        private readonly CircularBuffer<float> _distanceHistory;
        private readonly CircularBuffer<float> _confidenceHistory;
        
        public Observable<TouchEventData> TouchEvents => _touchEvents.AsObservable();
        public Observable<TouchState> TouchStateChanged => _touchStateChanged.AsObservable();
        public TouchState CurrentState => _currentState;
        
        /// <summary>
        /// Инициализация менеджера состояний касания.
        /// Initialize touch state manager.
        /// </summary>
        /// <param name="settings">Настройки определения касания / Touch detection settings</param>
        public TouchStateManager(TouchDetectionSettings settings)
        {
            _settings = settings;
            _distanceHistory = new CircularBuffer<float>(5);
            _confidenceHistory = new CircularBuffer<float>(5);
        }
        
        /// <summary>
        /// Обновление состояния касания на основе данных рук.
        /// Update touch state based on hand data.
        /// </summary>
        /// <param name="indexPosition">Позиция указательного пальца / Index finger position</param>
        /// <param name="thumbPosition">Позиция большого пальца / Thumb position</param>
        /// <param name="confidence">Уверенность отслеживания / Tracking confidence</param>
        /// <returns>Данные события касания / Touch event data</returns>
        public TouchEventData UpdateTouchState(Vector3 indexPosition, Vector3 thumbPosition, float confidence)
        {
            // Рассчитываем расстояние между пальцами
            // Calculate distance between fingers
            float currentDistance = Vector3.Distance(indexPosition, thumbPosition);
            
            // Добавляем в историю для сглаживания
            // Add to history for smoothing
            _distanceHistory.Add(currentDistance);
            _confidenceHistory.Add(confidence);
            
            // Применяем сглаживание
            // Apply smoothing
            _smoothedDistance = Mathf.Lerp(_smoothedDistance, GetAverageDistance(), _settings.smoothingFactor);
            float averageConfidence = GetAverageConfidence();
            
            // Определяем новое состояние
            // Determine new state
            TouchState newState = DetermineTouchState(_smoothedDistance, averageConfidence);
            
            // Обрабатываем переход состояния
            // Handle state transition
            ProcessStateTransition(newState, indexPosition, _smoothedDistance, averageConfidence);
            
            // Создаем данные события
            // Create event data
            var touchEvent = new TouchEventData(
                _currentState,
                indexPosition,
                CalculateTouchStrength(_smoothedDistance),
                GetCurrentTouchDuration(),
                averageConfidence
            );
            
            // Отправляем событие
            // Send event
            _touchEvents.OnNext(touchEvent);
            
            return touchEvent;
        }
        
        /// <summary>
        /// Определение состояния касания на основе расстояния и уверенности.
        /// Determine touch state based on distance and confidence.
        /// </summary>
        /// <param name="distance">Расстояние между пальцами / Distance between fingers</param>
        /// <param name="confidence">Уверенность отслеживания / Tracking confidence</param>
        /// <returns>Новое состояние касания / New touch state</returns>
        private TouchState DetermineTouchState(float distance, float confidence)
        {
            // Проверяем минимальную уверенность
            // Check minimum confidence
            if (confidence < _settings.minConfidence)
                return TouchState.None;
            
            // Применяем гистерезис для стабильности
            // Apply hysteresis for stability
            float thresholdToUse = _currentState == TouchState.None || _currentState == TouchState.Ended
                ? _settings.pinchThreshold
                : _settings.pinchThreshold + _settings.hysteresis;
            
            bool shouldBeTouching = distance <= thresholdToUse;
            
            switch (_currentState)
            {
                case TouchState.None:
                    return shouldBeTouching ? TouchState.Started : TouchState.None;
                    
                case TouchState.Started:
                case TouchState.Active:
                    if (!shouldBeTouching)
                    {
                        // Проверяем минимальную длительность касания
                        // Check minimum touch duration
                        float touchDuration = Time.time - _touchStartTime;
                        return touchDuration >= _settings.minTouchDuration ? TouchState.Ended : TouchState.Active;
                    }
                    return TouchState.Active;
                    
                case TouchState.Ended:
                    // Проверяем промежуток между касаниями
                    // Check gap between touches
                    float timeSinceLastTouch = Time.time - _lastTouchEndTime;
                    if (shouldBeTouching && timeSinceLastTouch >= _settings.maxTouchGap)
                    {
                        return TouchState.Started;
                    }
                    return timeSinceLastTouch < _settings.maxTouchGap ? TouchState.Ended : TouchState.None;
                    
                default:
                    return TouchState.None;
            }
        }
        
        /// <summary>
        /// Обработка перехода между состояниями касания.
        /// Process transition between touch states.
        /// </summary>
        /// <param name="newState">Новое состояние / New state</param>
        /// <param name="position">Позиция пальца / Finger position</param>
        /// <param name="distance">Расстояние / Distance</param>
        /// <param name="confidence">Уверенность / Confidence</param>
        private void ProcessStateTransition(TouchState newState, Vector3 position, float distance, float confidence)
        {
            if (newState != _currentState)
            {
                TouchState previousState = _currentState;
                _currentState = newState;
                
                // Обрабатываем специальные переходы
                // Handle special transitions
                switch (newState)
                {
                    case TouchState.Started:
                        _touchStartTime = Time.time;
                        break;
                        
                    case TouchState.Ended:
                        _lastTouchEndTime = Time.time;
                        float touchDuration = _lastTouchEndTime - _touchStartTime;
                        break;
                        
                    case TouchState.None:
                        if (previousState == TouchState.Ended)
                        {
                            // Touch sequence completed
                        }
                        break;
                }
                
                // Отправляем событие смены состояния
                // Send state change event
                _touchStateChanged.OnNext(newState);
            }
        }
        
        /// <summary>
        /// Расчет силы касания на основе расстояния.
        /// Calculate touch strength based on distance.
        /// </summary>
        /// <param name="distance">Расстояние между пальцами / Distance between fingers</param>
        /// <returns>Сила касания (0-1) / Touch strength (0-1)</returns>
        private float CalculateTouchStrength(float distance)
        {
            if (distance >= _settings.pinchThreshold)
                return 0f;
                
            // Линейная интерполяция от 0 до 1
            // Linear interpolation from 0 to 1
            return Mathf.Clamp01(1f - (distance / _settings.pinchThreshold));
        }
        
        /// <summary>
        /// Получение текущей длительности касания.
        /// Get current touch duration.
        /// </summary>
        /// <returns>Длительность в секундах / Duration in seconds</returns>
        private float GetCurrentTouchDuration()
        {
            return (_currentState == TouchState.Started || _currentState == TouchState.Active)
                ? Time.time - _touchStartTime
                : 0f;
        }
        
        /// <summary>
        /// Получение среднего расстояния из истории.
        /// Get average distance from history.
        /// </summary>
        /// <returns>Среднее расстояние / Average distance</returns>
        private float GetAverageDistance()
        {
            if (_distanceHistory.Count == 0) return 0f;
            
            float sum = 0f;
            for (int i = 0; i < _distanceHistory.Count; i++)
            {
                sum += _distanceHistory[i];
            }
            return sum / _distanceHistory.Count;
        }
        
        /// <summary>
        /// Получение средней уверенности из истории.
        /// Get average confidence from history.
        /// </summary>
        /// <returns>Средняя уверенность / Average confidence</returns>
        private float GetAverageConfidence()
        {
            if (_confidenceHistory.Count == 0) return 0f;
            
            float sum = 0f;
            for (int i = 0; i < _confidenceHistory.Count; i++)
            {
                sum += _confidenceHistory[i];
            }
            return sum / _confidenceHistory.Count;
        }
        
        /// <summary>
        /// Обновление настроек определения касания.
        /// Update touch detection settings.
        /// </summary>
        /// <param name="newSettings">Новые настройки / New settings</param>
        public void UpdateSettings(TouchDetectionSettings newSettings)
        {
            _settings = newSettings;
        }
        
        /// <summary>
        /// Сброс состояния касания.
        /// Reset touch state.
        /// </summary>
        public void ResetState()
        {
            _currentState = TouchState.None;
            _touchStartTime = 0f;
            _lastTouchEndTime = 0f;
            _smoothedDistance = 0f;
            _distanceHistory.Clear();
            _confidenceHistory.Clear();
        }
        
        /// <summary>
        /// Освобождение ресурсов.
        /// Release resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _touchEvents?.Dispose();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TouchStateManager: Error disposing _touchEvents: {ex.Message}");
            }
            
            try
            {
                _touchStateChanged?.Dispose();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TouchStateManager: Error disposing _touchStateChanged: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Кольцевой буфер для хранения истории значений с IDisposable поддержкой.
    /// Circular buffer for storing value history with IDisposable support.
    /// </summary>
    /// <typeparam name="T">Тип хранимых значений / Type of stored values</typeparam>
    public class CircularBuffer<T> : IDisposable
    {
        private T[] _buffer;
        private int _head = 0;
        private int _count = 0;
        private bool _isDisposed = false;
        
        public int Count => _count;
        
        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
        }
        
        public void Add(T item)
        {
            if (_isDisposed) return;
            
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            
            if (_count < _buffer.Length)
                _count++;
        }
        
        public T this[int index]
        {
            get
            {
                if (_isDisposed || index >= _count)
                    throw new IndexOutOfRangeException();
                    
                int actualIndex = (_head - _count + index + _buffer.Length) % _buffer.Length;
                return _buffer[actualIndex];
            }
        }
        
        public void Clear()
        {
            if (_isDisposed) return;
            
            // Clear references for GC
            for (int i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = default(T);
            }
            
            _head = 0;
            _count = 0;
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            Clear();
            _buffer = null;
        }
    }
}