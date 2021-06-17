using System;
using System.Collections.Generic;
using System.Text;

namespace DeftUnitTests.ProjectClasses
{
    class ThreadArgs
    {
        
    }

    class ThreadResponse
    {
        public bool IsDeftThread { get; set; }
        public bool IsPoolThread { get; set; }
        public bool IsResponseThread { get; set; }
        public bool IsRequestThread { get; set; }
    }
}
