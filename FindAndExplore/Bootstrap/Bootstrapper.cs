using Akavache;
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
using Appmilla.RestApiClient.Interfaces;
using Appmilla.RestApiClient;
using Appmilla.RestApiClient.Logging;
using Appmilla.RestApiClient.Logging.Interfaces;
using FindAndExplore.AppEvents;
using FindAndExplore.Caches;
using FindAndExplore.Configuration;
using FindAndExplore.DatasetProviders;
using FindAndExplore.Http;
using FindAndExplore.Infrastructure;
using FindAndExplore.Mapping;
using FindAndExplore.Queries;
using FindAndExplore.Reactive;
using FindAndExplore.ViewModels;
using FindAndExplore.Services;

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

            builder.RegisterType<ErrorReporter>().As<IErrorReporter>();
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

            builder.RegisterType<DirectionsService>().As<IDirectionsService>().SingleInstance();

            builder.RegisterType<FindAndExploreApiClient>().AsSelf().SingleInstance();
            builder.RegisterType<FoursquareApiClient>().AsSelf().SingleInstance();
            builder.RegisterType<FacebookApiClient>().AsSelf().SingleInstance();
            builder.RegisterType<MapboxApiClient>().AsSelf().SingleInstance();
            builder.RegisterType<ConnectivityMonitor>().As<IConnectivityMonitor>().SingleInstance();
            
            //TODO try AsImplementedInterfaces later
            builder.RegisterType<MapControl>().As<IMapControl>().As<IMapDelegate>().As<IMapCamera>().SingleInstance();

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

            builder.Register(c =>
            {
                var blobCache = c.ResolveKeyed<IBlobCache>(AkavacheConstants.LocalMachine);
                var findAndExploreHttpClientFactory = c.Resolve<IFindAndExploreHttpClientFactory>();
                var foursquareApiClient = c.Resolve<FoursquareApiClient>();
                var schedulerProvider = c.Resolve<ISchedulerProvider>();

                return new FoursquareQuery(blobCache,
                    findAndExploreHttpClientFactory,
                    foursquareApiClient,
                    schedulerProvider);

            }).As<IFoursquareQuery>().SingleInstance();

            builder.Register(c =>
            {
                var blobCache = c.ResolveKeyed<IBlobCache>(AkavacheConstants.LocalMachine);
                var findAndExploreHttpClientFactory = c.Resolve<IFindAndExploreHttpClientFactory>();
                var facebookApiClient = c.Resolve<FacebookApiClient>();
                var schedulerProvider = c.Resolve<ISchedulerProvider>();

                return new FacebookQuery(blobCache,
                    findAndExploreHttpClientFactory,
                    facebookApiClient,
                    schedulerProvider);

            }).As<IFacebookQuery>().SingleInstance();

            builder.RegisterType<FoursquareDatasetProvider>().As<IFoursquareDatasetProvider>().SingleInstance();
            builder.RegisterType<FindAndExploreDatasetProvider>().As<IFindAndExploreDatasetProvider>().SingleInstance();
            builder.RegisterType<FacebookDatasetProvider>().As<IFacebookDatasetProvider>().SingleInstance();

            builder.RegisterType<PlacesCache>().As<IPlacesCache>().SingleInstance();           
            builder.RegisterType<ApplicationLifecycleObserver>().As<IApplicationLifecycleObserver>().SingleInstance();
        }

        static void SetupViewModels(ContainerBuilder builder)
        {
            builder.RegisterType<MapViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<MoreViewModel>().AsSelf().SingleInstance();
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
            Akavache.Registrations.Start("FindAndExplore");
        }
    }
}

