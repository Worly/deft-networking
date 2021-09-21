using System;
using System.Collections.Generic;
using System.Text;

namespace Deft
{
    /// <summary>
    /// This class is not full dependency injector, it only supports creating an instance of a given class, <br/>
    /// this is used as a default IInjectionContainer to support DeftRouter instantiation
    /// </summary>
    internal class SimpleInjector : IInjectionContainer, IInjectionScope
    {
        public IInjectionScope BeginScope()
        {
            return this;
        }

        public void EndScope()
        {
            // nothing
        }

        public TService GetInstance<TService>() where TService : class
        {
            return Activator.CreateInstance<TService>();
        }

        public object GetInstance(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }

        public void Register<TConcrete>(ServiceLifestyle lifestyle) where TConcrete : class
        {
            // not available for SimpleInjector
            throw new NotImplementedException();
        }

        public void Register(Type concreteType, ServiceLifestyle lifestyle)
        {
            // not available for SimpleInjector
            throw new NotImplementedException();
        }

        public void Register(Type serviceType, Type implementationType, ServiceLifestyle lifestyle)
        {
            // not available for SimpleInjector
            throw new NotImplementedException();
        }

        void IInjectionContainer.Register<TService, TImplementation>(ServiceLifestyle lifestyle)
        {
            // not available for SimpleInjector
            throw new NotImplementedException();
        }
    }
}
