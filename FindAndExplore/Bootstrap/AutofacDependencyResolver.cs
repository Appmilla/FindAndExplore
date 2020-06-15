using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using Splat;

namespace FindAndExplore.Bootstrap
{
    public class AutofacDependencyResolver : IDependencyResolver
    {
        private IContainer _container;

        public AutofacDependencyResolver(IContainer container)
        {
            _container = container;
        }

        public object GetService(Type serviceType, string contract = null)
        {
            try
            {
                return string.IsNullOrEmpty(contract)
                    ? _container.Resolve(serviceType)
                    : _container.ResolveNamed(contract, serviceType);
            }
            catch (DependencyResolutionException)
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType, string contract = null)
        {
            try
            {
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
                object instance = string.IsNullOrEmpty(contract)
                    ? _container.Resolve(enumerableType)
                    : _container.ResolveNamed(contract, enumerableType);
                return ((IEnumerable)instance).Cast<object>();
            }
            catch (DependencyResolutionException)
            {
                return null;
            }
        }

        public void Register(Func<object> factory, Type serviceType, string contract = null)
        {
            var builder = new ContainerBuilder();
            if (string.IsNullOrEmpty(contract))
            {
                builder.Register(x => factory()).As(serviceType).AsImplementedInterfaces();
            }
            else
            {
                builder.Register(x => factory()).Named(contract, serviceType).AsImplementedInterfaces();
            }

            builder.Update(_container);
        }

        public void UnregisterCurrent(Type serviceType, string contract = null)
        {
            // this method is not used by RxUI
            throw new NotImplementedException();

            /*
            var builder = new ContainerBuilder();

            IEnumerable<IComponentRegistration> components;
        
            if (!string.IsNullOrEmpty(contract))
                components = _container.ComponentRegistry.RegistrationsFor(new KeyedService(contract, serviceType));
            else
                components = _container.ComponentRegistry.Registrations
                .Where(cr => cr.Activator.LimitType != typeof(LifetimeScope))
                .Where(cr => cr.Activator.LimitType != serviceType);
        
            foreach (var c in components)
            {
                builder.RegisterComponent(c);
            }

            foreach (var source in _container.ComponentRegistry.Sources)
            {
                builder.RegisterSource(source);
            }

            //builder.Update(_container);
            _container = builder.Build();
            */
        }

        public void UnregisterAll(Type serviceType, string contract = null)
        {
            throw new NotImplementedException();
        }

        public IDisposable ServiceRegistrationCallback(Type serviceType, string contract, Action<IDisposable> callback)
        {
            // this method is not used by RxUI
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public bool HasRegistration(Type serviceType, string contract = null)
        {
            return _container.IsRegistered(serviceType);
        }
    }
}
