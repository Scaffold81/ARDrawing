using Zenject;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Services;
using ARDrawing.Core.Models;
using UnityEngine;

namespace ARDrawing.Installers
{
    /// <summary>
    /// Основной инсталлер для настройки Dependency Injection в приложении.
    /// Main installer for setting up Dependency Injection in the application.
    /// </summary>
    public class AppInstaller : MonoInstaller
    {
        [Header("Service Settings")]
        [SerializeField] private DrawingSettings defaultDrawingSettings;
        
        [Header("Performance Settings")]  
        [SerializeField] private bool enableDebugLogging = true;
        
        /// <summary>
        /// Регистрация всех сервисов и зависимостей в DI контейнере.
        /// Register all services and dependencies in DI container.
        /// </summary>
        public override void InstallBindings()
        {
            InstallCoreServices();
            InstallConfigurations();
            InstallFactories();
            
            if (enableDebugLogging)
            {
                Debug.Log("AppInstaller: All services installed successfully / Все сервисы успешно установлены");
            }
        }
        
        /// <summary>
        /// Регистрация основных сервисов приложения.
        /// Register core application services.
        /// </summary>
        private void InstallCoreServices()
        {
            // Hand Tracking сервис с OpenXR интеграцией
            // Hand Tracking service with OpenXR integration
            Container
                .Bind<IHandTrackingService>()
                .To<OpenXRHandTrackingService>()
                .AsSingle()
                .NonLazy();
            
            // Сервис рисования с пулингом объектов
            // Drawing service with object pooling
            Container
                .Bind<IDrawingService>()
                .To<DrawingService>()
                .AsSingle()
                .NonLazy();
            
            // Сервис взаимодействия с UI
            // UI interaction service
            Container
                .Bind<IUIInteractionService>()
                .To<UIInteractionService>()
                .AsSingle()
                .NonLazy();
            
            // Сервис сохранения/загрузки через JSON
            // Save/Load service through JSON
            Container
                .Bind<ISaveLoadService>()
                .To<JsonSaveLoadService>()
                .AsSingle()
                .NonLazy();
        }
        
        /// <summary>
        /// Регистрация конфигураций и настроек.
        /// Register configurations and settings.
        /// </summary>
        private void InstallConfigurations()
        {
            // Настройки рисования по умолчанию
            // Default drawing settings
            if (defaultDrawingSettings.Equals(default(DrawingSettings)))
            {
                defaultDrawingSettings = DrawingSettings.Default;
            }
            
            Container
                .Bind<DrawingSettings>()
                .FromInstance(defaultDrawingSettings)
                .AsSingle();
        }
        
        /// <summary>
        /// Регистрация фабрик для создания объектов.
        /// Register factories for object creation.
        /// </summary>
        private void InstallFactories()
        {
            // Фабрика для создания линий рисования
            // Factory for creating drawing lines
            Container
                .Bind<IFactory<DrawingLine>>()
                .To<DrawingLineFactory>()
                .AsSingle();
        }
        
        /// <summary>
        /// Валидация настроек инсталлера (вызывается в Editor).
        /// Validate installer settings (called in Editor).
        /// </summary>
        private void OnValidate()
        {
            // Проверяем что настройки корректны
            // Check that settings are correct
            if (defaultDrawingSettings.lineThickness <= 0)
            {
                defaultDrawingSettings.lineThickness = 0.01f;
            }
            
            if (defaultDrawingSettings.minPointDistance <= 0)
            {
                defaultDrawingSettings.minPointDistance = 0.005f;
            }
        }
    }
}