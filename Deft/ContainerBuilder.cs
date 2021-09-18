using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deft
{
    public class ContainerBuilder
    {
        private Container container;

        internal ContainerBuilder(Container container)
        {
            this.container = container;
        }

        public ContainerBuilder Register<TConcrete>(ServiceLifestyle lifestyle = ServiceLifestyle.Scoped) where TConcrete : class
        {
            container.Register<TConcrete>(GetLifestyle(lifestyle));
            return this;
        }

        public ContainerBuilder Register<TService, TImplementation>(ServiceLifestyle lifestyle = ServiceLifestyle.Scoped) where TService : class where TImplementation : class, TService
        {
            container.Register<TService, TImplementation>(GetLifestyle(lifestyle));
            return this;
        }

        public ContainerBuilder Register<TService>(Func<TService> instanceCreator, ServiceLifestyle lifestyle = ServiceLifestyle.Scoped) where TService : class
        {
            container.Register<TService>(instanceCreator, GetLifestyle(lifestyle));
            return this;
        }

        public ContainerBuilder Register(Type serviceType, Type implementationType, ServiceLifestyle lifestyle = ServiceLifestyle.Scoped)
        {
            container.Register(serviceType, implementationType, GetLifestyle(lifestyle));
            return this;
        }

        public ContainerBuilder Register(Type serviceType, Func<object> instanceCreator, ServiceLifestyle lifestyle = ServiceLifestyle.Scoped)
        {
            container.Register(serviceType, instanceCreator, GetLifestyle(lifestyle));
            return this;
        }

        private Lifestyle GetLifestyle(ServiceLifestyle serviceLifestyle)
        {
            switch(serviceLifestyle)
            {
                case ServiceLifestyle.Scoped:
                    return Lifestyle.Scoped;
                case ServiceLifestyle.Singleton:
                    return Lifestyle.Singleton;
                case ServiceLifestyle.Transient:
                    return Lifestyle.Transient;
                default:
                    throw new NotImplementedException("Given ServiceLifestyle is not implemented.");
            }
        }
    }

    public enum ServiceLifestyle
    {
        Transient,
        Scoped,
        Singleton
    }
}
