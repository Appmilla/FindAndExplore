﻿using Akavache;
using Autofac;
using ReactiveUI;
using System.Reactive.Concurrency;
using Splat;
using CommonServiceLocator;
using Xamarin.Essentials.Interfaces;
using Xamarin.Essentials.Implementation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using FindAndExplore.Core.Configuration;
using FindAndExplore.Core.Reactive;
using FindAndExplore.Core.Http;
using FindAndExplore.Core.AppEvents;
using FindAndExplore.Core.Queries;
using Appmilla.RestApiClient.Interfaces;
using Appmilla.RestApiClient;
using Appmilla.RestApiClient.Logging;
using Appmilla.RestApiClient.Logging.Interfaces;
using FindAndExplore.Core.Bootstrap;
using FindAndExplore.ViewModels;

namespace FindAndExplore.Bootstrap
{
    public static class Bootstrapper
    {
        public static void Bootstrap(ContainerBuilder containerBuilder)
        {
            SetupXamarinEssentials(containerBuilder);

            SetupServices(containerBuilder);

            SetupViewModels(containerBuilder);
        }

        public static void PostContainerBuild()
        {
            SetupLogging();
        }


        public static void SetupLogging()
        {
        }

        static void SetupXamarinEssentials(ContainerBuilder builder)
        {
            // register the IMainThread but prefer to use the ISchedulerProvider.Dispatcher.Schedule
            builder.RegisterType<MainThreadImplementation>().As<IMainThread>().SingleInstance();
            builder.RegisterType<FlashlightImplementation>().As<IFlashlight>().SingleInstance();
            builder.RegisterType<SecureStorageImplementation>().As<ISecureStorage>().SingleInstance();
            builder.RegisterType<PreferencesImplementation>().As<IPreferences>().SingleInstance();
            builder.RegisterType<ConnectivityImplementation>().As<IConnectivity>().SingleInstance();
            builder.RegisterType<BrowserImplementation>().As<IBrowser>().SingleInstance();
            builder.RegisterType<VersionTrackingImplementation>().As<IVersionTracking>().SingleInstance();
            builder.RegisterType<ClipboardImplementation>().As<IClipboard>().SingleInstance();
            builder.RegisterType<DeviceInfoImplementation>().As<IDeviceInfo>().SingleInstance();
            builder.RegisterType<PhoneDialerImplementation>().As<IPhoneDialer>().SingleInstance();
            builder.RegisterType<LauncherImplementation>().As<ILauncher>().SingleInstance();
            builder.RegisterType<ShareImplementation>().As<IShare>().SingleInstance();
        }

        static void SetupServices(ContainerBuilder builder)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore
            };
            serializerSettings.Converters.Add(new StringEnumConverter());
            builder.RegisterInstance(serializerSettings).As<JsonSerializerSettings>().SingleInstance();

            // Akavache asks the container for this, if not found uses the TaskPoolScheduler.Default
            // so by adding it to the container we reduce the number of first chance exceptions thrown
            // this registration can be removed and the app still works ok
            var taskPoolScheduler = TaskPoolScheduler.Default;
            builder.RegisterInstance<IScheduler>(taskPoolScheduler).Named<IScheduler>("Taskpool");

            builder.RegisterType<AppConfiguration>().As<IAppConfiguration>();
            builder.RegisterType<SchedulerProvider>().As<ISchedulerProvider>().SingleInstance();
            builder.RegisterType<FindAndExploreHttpClientFactory>().As<IFindAndExploreHttpClientFactory>().SingleInstance();

            builder.RegisterType<LoggingService>().As<ILoggingService>().SingleInstance();

            builder.RegisterType<FindAndExploreApiClient>().SingleInstance();

            builder.Register(c =>
            {
                var jsonSerializer = c.Resolve<JsonSerializerSettings>();

                var loggingservice = c.Resolve<ILoggingService>();
                var findAndExploreHttpClientFactory = c.Resolve<IFindAndExploreHttpClientFactory>();
                var httpClient = findAndExploreHttpClientFactory.CreateClient();

                var apiService = new ApiService(loggingservice, httpClient);
                apiService.JsonSerializer = jsonSerializer;

                return apiService;
            }).As<IApiService>().SingleInstance();

            builder.RegisterType<FindAndExploreApiClient>().AsSelf().SingleInstance();
            builder.RegisterType<ConnectivityMonitor>().As<IConnectivityMonitor>().SingleInstance();

            builder.Register(c =>
            {
                var blobCache = c.ResolveKeyed<IBlobCache>(AkavacheConstants.LocalMachine);
                var findAndExploreHttpClientFactory = c.Resolve<IFindAndExploreHttpClientFactory>();
                var findAndExploreApiClient = c.Resolve<FindAndExploreApiClient>();
                var schedulerProvider = c.Resolve<ISchedulerProvider>();

                return new FindAndExploreQuery(blobCache,
                    findAndExploreHttpClientFactory,
                    findAndExploreApiClient,
                    schedulerProvider);

            }).As<IFindAndExploreQuery>().SingleInstance();

            //builder.RegisterType<AlertsCache>().As<IAlertsCache>().SingleInstance();           
            builder.RegisterType<ApplicationLifecycleObserver>().As<IApplicationLifecycleObserver>().SingleInstance();
        }

        static void SetupViewModels(ContainerBuilder builder)
        {
            builder.RegisterType<MapViewModel>().AsSelf().SingleInstance();
        }

        public static void SetupReactiveUI(IContainer container)
        {
            var resolver = new AutofacDependencyResolver(container);
            // These Initialize methods will add ReactiveUI platform registrations to your container
            // They MUST be present if you override the default Locator
            resolver.InitializeSplat();

            resolver.InitializeReactiveUI();

            Locator.SetLocator(resolver);

            // Make sure you set the application name before doing any inserts or gets
            Akavache.Registrations.Start("MapboxForms");
        }
    }
}

