using Autofac;

namespace FindAndExplore.Core.Bootstrap
{
    public class AutofacDependencyRegistrar
    {
        protected IContainer Container;

        public AutofacDependencyRegistrar(IContainer container)
        {
            Container = container;
            RegisterViews();
            RegisterViewModels();
            RegisterServices();
            RegisterScreen();
        }

        public void RegisterViews() { }

        public void RegisterViewModels() { }

        public void RegisterServices() { }

        public void RegisterScreen() { }
    }
}
