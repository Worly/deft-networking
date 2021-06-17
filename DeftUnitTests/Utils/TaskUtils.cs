using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeftUnitTests.Utils
{
    public static class TaskUtils
    {
        public static async Task WaitFor(Func<bool> check)
        {
            while (!check())
                await Task.Delay(1);
        }
    }
}
