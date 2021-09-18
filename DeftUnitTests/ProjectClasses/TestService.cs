using System;
using System.Collections.Generic;
using System.Text;

namespace DeftUnitTests.ProjectClasses
{
    public interface ITestService
    {
        void IncrementCounter();
        int GetCounter();
    }

    public class TestService : ITestService
    {
        private int counter = 0;
        public void IncrementCounter()
        {
            counter++;
        }

        public int GetCounter()
        {
            return this.counter;
        }
    }
}
