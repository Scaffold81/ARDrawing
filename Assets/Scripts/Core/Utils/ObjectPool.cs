using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARDrawing.Core.Utils
{
    /// <summary>
    /// Базовый интерфейс для объектов в пуле.
    /// Base interface for pooled objects.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Вызывается при извлечении объекта из пула.
        /// Called when object is taken from pool.
        /// </summary>
        void OnTakeFromPool();
        
        /// <summary>
        /// Вызывается при возврате объекта в пул.
        /// Called when object is returned to pool.
        /// </summary>
        void OnReturnToPool();
    }
    
    /// <summary>
    /// Универсальный пул объектов с возможностью создания и управления.
    /// Generic object pool with creation and management capabilities.
    /// </summary>
    /// <typeparam name="T">Тип объектов в пуле / Type of objects in pool</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onTake;
        private readonly Action<T> _onReturn;
        private readonly int _maxSize;
        private int _currentCount;
        
        /// <summary>
        /// Количество доступных объектов в пуле.
        /// Number of available objects in pool.
        /// </summary>
        public int AvailableCount => _pool.Count;
        
        /// <summary>
        /// Общее количество созданных объектов.
        /// Total number of created objects.
        /// </summary>
        public int TotalCount => _currentCount;
        
        /// <summary>
        /// Количество активных (используемых) объектов.
        /// Number of active (in use) objects.
        /// </summary>
        public int ActiveCount => _currentCount - _pool.Count;
        
        public ObjectPool(
            Func<T> createFunc,
            Action<T> onTake = null,
            Action<T> onReturn = null,
            int initialSize = 10,
            int maxSize = 100)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _onTake = onTake;
            _onReturn = onReturn;
            _maxSize = maxSize;
            
            // Предварительное создание объектов
            for (int i = 0; i < initialSize; i++)
            {
                var obj = _createFunc();
                _onReturn?.Invoke(obj);
                _pool.Push(obj);
                _currentCount++;
            }
        }
        
        /// <summary>
        /// Получает объект из пула или создает новый.
        /// Gets an object from pool or creates a new one.
        /// </summary>
        /// <returns>Объект из пула / Object from pool</returns>
        public T Take()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = _createFunc();
                _currentCount++;
            }
            
            _onTake?.Invoke(obj);
            
            if (obj is IPoolable poolable)
            {
                poolable.OnTakeFromPool();
            }
            
            return obj;
        }
        
        /// <summary>
        /// Возвращает объект в пул.
        /// Returns an object to pool.
        /// </summary>
        /// <param name="obj">Объект для возврата / Object to return</param>
        public void Return(T obj)
        {
            if (obj == null) return;
            
            if (obj is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }
            
            _onReturn?.Invoke(obj);
            
            if (_pool.Count < _maxSize)
            {
                _pool.Push(obj);
            }
            else
            {
                // Если пул переполнен, просто уничтожаем объект
                if (obj is Component component)
                {
                    UnityEngine.Object.Destroy(component.gameObject);
                }
                else if (obj is GameObject gameObject)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
                _currentCount--;
            }
        }
        
        /// <summary>
        /// Очищает пул, уничтожая все объекты.
        /// Clears the pool, destroying all objects.
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                if (obj is Component component)
                {
                    UnityEngine.Object.Destroy(component.gameObject);
                }
                else if (obj is GameObject gameObject)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
            _currentCount = 0;
        }
        
        /// <summary>
        /// Предварительно заполняет пул указанным количеством объектов.
        /// Pre-fills the pool with specified number of objects.
        /// </summary>
        /// <param name="count">Количество объектов для создания / Number of objects to create</param>
        public void Prewarm(int count)
        {
            var objectsToCreate = Mathf.Min(count, _maxSize - _currentCount);
            
            for (int i = 0; i < objectsToCreate; i++)
            {
                var obj = _createFunc();
                _onReturn?.Invoke(obj);
                _pool.Push(obj);
                _currentCount++;
            }
        }
    }
    
    /// <summary>
    /// Статистика пула объектов.
    /// Object pool statistics.
    /// </summary>
    [Serializable]
    public struct PoolStats
    {
        public int TotalCreated;
        public int ActiveObjects;
        public int AvailableInPool;
        public int MaxPoolSize;
        public float UtilizationPercent;
        
        public static PoolStats FromPool<T>(ObjectPool<T> pool, int maxSize) where T : class
        {
            return new PoolStats
            {
                TotalCreated = pool.TotalCount,
                ActiveObjects = pool.ActiveCount,
                AvailableInPool = pool.AvailableCount,
                MaxPoolSize = maxSize,
                UtilizationPercent = pool.TotalCount > 0 ? (pool.ActiveCount / (float)pool.TotalCount) * 100f : 0f
            };
        }
    }
}