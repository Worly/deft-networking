using System;

namespace Deft
{
    public class ContainerBuilder<T> where T : IInjectionContainer
    {
        private T container;

        internal ContainerBuilder(T container)
        {
            this.container = container;
        }

        public ContainerBuilder<T> Register<TConcrete>(ServiceLifestyle lifestyle = ServiceLifestyle.Scoped) where TConcrete : class
        {
            container.Register<TConcrete>(lifestyle);
            return this;
        }

        public ContainerBuilder<T> Register<TService, TImplementation>(ServiceLifestyle lifestyle = ServiceLifestyle.Scoped) where TService : class where TImplementation : class, TService
        {
            container.Register<TService, TImplementation>(lifestyle);
            return this;
        }
    }
}
