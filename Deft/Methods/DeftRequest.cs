using System.Collections.Generic;

namespace Deft
{
    public class DeftRequest<TRequestType>
    {
        public Dictionary<string, string> Headers { get; set; }
        public TRequestType Body { get; set; }
    }
}
