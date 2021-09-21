using System;
using System.Collections.Generic;
using System.Text;

namespace Deft
{
    public enum ServiceLifestyle
    {
        Transient,
        Scoped,
        Singleton
    }

    public interface IInjectionContainer : IServiceResolver
    {
        void Register<TConcrete>(ServiceLifestyle lifestyle) where TConcrete : class;
        void Register<TService, TImplementation>(ServiceLifestyle lifestyle) where TService : class where TImplementation : class, TService;
        void Register(Type concreteType, ServiceLifestyle lifestyle);
        void Register(Type serviceType, Type implementationType, ServiceLifestyle lifestyle);
        IInjectionScope BeginScope();
    }

    public interface IServiceResolver
    {
        TService GetInstance<TService>() where TService : class;
        object GetInstance(Type serviceType);
    }

    public interface IInjectionScope : IServiceResolver
    {
        void EndScope();
    }
}
