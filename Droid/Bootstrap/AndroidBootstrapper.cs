using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using FindAndExplore.Bootstrap;
using FindAndExplore.Droid.Http;
using FindAndExplore.Droid.Mapping;
using FindAndExplore.Http;
using FindAndExplore.Mapping;

namespace FindAndExplore.Droid.Bootstrap
{
    public static class AndroidBootstrapper
    {
        public static void Bootstrap()
        {
            var builder = new ContainerBuilder();

            // Perform registrations and build the container.

            var messageHandlerFactory = new MessageHandlerFactory();
            builder.Register(c => messageHandlerFactory).As<IMessageHandlerFactory>().SingleInstance();
            
            builder.RegisterType<MapLayerController>().As<IMapLayerController>().SingleInstance();
            
            Bootstrapper.Bootstrap(builder);

            // build the container
            var container = builder.Build();
            Bootstrapper.SetupReactiveUI(container);

            // Set the service locator to an AutofacServiceLocator.
            var csl = new AutofacServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => csl);

            Bootstrapper.PostContainerBuild();
        }
    }
}
