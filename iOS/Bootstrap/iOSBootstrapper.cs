﻿using UIKit;
using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using Splat;
using FindAndExplore.iOS.Http;
using FindAndExplore.Bootstrap;
using FindAndExplore.Http;

namespace FindAndExplore.iOS.Bootstrap
{
    public static class iOSBootstrapper
    {
        public static void Bootstrap(UIApplication application)
        {
            var builder = new ContainerBuilder();

            // Perform registrations 

            var messageHandlerFactory = new MessageHandlerFactory();
            builder.Register(c => messageHandlerFactory).As<IMessageHandlerFactory>().SingleInstance();

            Bootstrapper.Bootstrap(builder);

            //build the container
            var container = builder.Build();
            Bootstrapper.SetupReactiveUI(container);

            // Set the service locator to an AutofacServiceLocator.
            var csl = new AutofacServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => csl);

            //App.Container = container;

            // Make sure Splat and ReactiveUI are already configured in the locator
            // so that our override runs last
            //Locator.CurrentMutable.RegisterLazySingleton(() => viewLocator, typeof(IViewLocator));

            Bootstrapper.PostContainerBuild();
        }
    }
}
