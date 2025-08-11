using Zenject;
using ARDrawing.Core.Interfaces;
using ARDrawing.Core.Services;
using ARDrawing.Core.Models;
using ARDrawing.Core.Config;
using ARDrawing.Presentation.Presenters;
using ARDrawing.Presentation.Views;
using ARDrawing.UI.Presenters;
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
            if (enableDebugLogging)
                Debug.Log("[AppInstaller] Installing core services...");
                
            // Hand Tracking сервис - выбор между реальным и симулятором
            // Hand Tracking service - choice between real and simulator
            if (useHandTrackingSimulator)
            {
                if (enableDebugLogging)
                    Debug.Log("[AppInstaller] Installing HandTrackingSimulator...");
                    
                Container
                    .Bind<IHandTrackingService>()
                    .To<HandTrackingSimulator>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                if (enableDebugLogging)
                    Debug.Log("[AppInstaller] Installing OpenXRHandTrackingService...");
                    
                Container
                    .Bind<IHandTrackingService>()
                    .To<OpenXRHandTrackingService>()
                    .FromNewComponentOnNewGameObject()
                    .AsSingle()
                    .NonLazy();
            }
            
            // Сервис рисования с пулингом объектов - только один экземпляр!
            // Drawing service with object pooling - only one instance!
            if (enableDebugLogging)
                Debug.Log("[AppInstaller] Installing DrawingService...");
                
            Container
                .Bind<IDrawingService>()
                .To<DrawingService>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
                
            // Презентер рисования с реактивной логикой
            // Drawing presenter with reactive logic
            if (enableDebugLogging)
                Debug.Log("[AppInstaller] Installing DrawingPresenter...");
                
            Container
                .Bind<DrawingPresenter>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
            
            // AR Drawing View для enhanced визуализации
            // AR Drawing View for enhanced visualization
            if (enableDebugLogging)
                Debug.Log("[AppInstaller] Installing ARDrawingView...");
                
            Container
                .Bind<ARDrawingView>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
            
            // AR Drawing View Tester
            if (enableDebugLogging)
                Debug.Log("[AppInstaller] Installing ARDrawingViewTester...");
                
            Container
                .Bind<ARDrawingViewTester>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
            
            // UI System (Phase 4) - найти существующий UIPresenter в сцене
            if (enableDebugLogging)
                Debug.Log("[AppInstaller] Binding existing UIPresenter from scene...");
                
            Container
                .Bind<UIPresenter>()
                .FromComponentInHierarchy()
                .AsSingle()
                .NonLazy();
            
            // Сервис сохранения/загрузки через JSON
            // Save/Load service through JSON
            if (enableDebugLogging)
                Debug.Log("[AppInstaller] Installing JsonSaveLoadService...");
                
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