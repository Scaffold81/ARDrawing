using Zenject;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Services;
using ARDrawing.Core.Models;
using ARDrawing.Core.Config;
using ARDrawing.Testing;
using UnityEngine;

namespace ARDrawing.Installers
{
    /// <summary>
    /// Основной инсталлер для настройки Dependency Injection в приложении.
    /// Main installer for setting up Dependency Injection in the application.
    /// </summary>
    public class AppInstaller : MonoInstaller
    {
        [Header("Debug Settings")]
        [SerializeField] private bool useHandTrackingSimulator = false;
        
        [Header("Service Settings")]
        [SerializeField] private DrawingSettingsConfig drawingSettingsConfig;
        
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
            // Hand Tracking сервис - выбор между реальным и симулятором
            // Hand Tracking service - choice between real and simulator
            if (useHandTrackingSimulator)
            {
                Container
                    .Bind<IHandTrackingService>()
                    .To<HandTrackingSimulator>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                Container
                    .Bind<IHandTrackingService>()
                    .To<OpenXRHandTrackingService>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }
            
            // Сервис рисования с пулингом объектов
            // Drawing service with object pooling
            Container
                .Bind<IDrawingService>()
                .To<DrawingService>()
                .FromNewComponentOnNewGameObject()
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
            // Настройки рисования из ScriptableObject
            // Drawing settings from ScriptableObject
            if (drawingSettingsConfig != null)
            {
                Container
                    .Bind<DrawingSettingsConfig>()
                    .FromInstance(drawingSettingsConfig)
                    .AsSingle();
                    
                Container
                    .Bind<DrawingSettings>()
                    .FromInstance(drawingSettingsConfig.ToDrawingSettings())
                    .AsSingle();
            }
            else
            {
                // Настройки по умолчанию если ScriptableObject не назначен
                Container
                    .Bind<DrawingSettings>()
                    .FromInstance(DrawingSettings.Default)
                    .AsSingle();
            }
        }
        
        /// <summary>
        /// Регистрация фабрик для создания объектов.
        /// Register factories for object creation.
        /// </summary>
        private void InstallFactories()
        {
            // Пока оставляем пустым - фабрики будут добавлены позже
            // Leave empty for now - factories will be added later
        }
        
        /// <summary>
        /// Валидация настроек инсталлера (вызывается в Editor).
        /// Validate installer settings (called in Editor).
        /// </summary>
        private void OnValidate()
        {
            // Проверяем что DrawingSettingsConfig назначен
            // Check that DrawingSettingsConfig is assigned
            if (drawingSettingsConfig != null)
            {
                drawingSettingsConfig.ValidateSettings();
            }
        }
    }
}