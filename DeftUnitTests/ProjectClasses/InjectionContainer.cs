using Deft;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeftUnitTests.ProjectClasses
{
    class InjectionContainer : IInjectionContainer
    {
        private Container container;

        public InjectionContainer()
        {
            container = new Container();
            container.Options.ResolveUnregisteredConcreteTypes = true;
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
        }

        public IInjectionScope BeginScope()
        {
            return new InjectionScope(container);
        }

        public TService GetInstance<TService>() where TService : class
        {
            return container.GetInstance<TService>();
        }

        public object GetInstance(Type serviceType)
        {
            return container.GetInstance(serviceType);
        }

        public void Register<TConcrete>(ServiceLifestyle lifestyle) where TConcrete : class
        {
            container.Register<TConcrete>(GetLifestyle(lifestyle));
        }

        public void Register(Type concreteType, ServiceLifestyle lifestyle)
        {
            container.Register(concreteType, concreteType, GetLifestyle(lifestyle));
        }

        public void Register(Type serviceType, Type implementationType, ServiceLifestyle lifestyle)
        {
            container.Register(serviceType, implementationType, GetLifestyle(lifestyle));
        }

        void IInjectionContainer.Register<TService, TImplementation>(ServiceLifestyle lifestyle)
        {
            container.Register<TService, TImplementation>(GetLifestyle(lifestyle));
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
                    throw new NotImplementedException("ServiceLifestyle " + serviceLifestyle + " is not implemented");
            }
        }
    }

    class InjectionScope : IInjectionScope
    {
        private Scope scope;

        public InjectionScope(Container container)
        {
            scope = AsyncScopedLifestyle.BeginScope(container);
        }

        public void EndScope()
        {
            scope.Dispose();
        }

        public TService GetInstance<TService>() where TService : class
        {
            return scope.GetInstance<TService>();
        }

        public object GetInstance(Type serviceType)
        {
            return scope.GetInstance(serviceType);
        }
    }
}
