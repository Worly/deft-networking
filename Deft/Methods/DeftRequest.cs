using System.Collections.Generic;

namespace Deft
{
    public class DeftRequest
    {
        public IServiceResolver ServiceResolver { get; set; }
        public uint MethodIndex { get; internal set; }
        public DeftConnectionOwner Owner { get; internal set; }
        public Dictionary<string, string> Headers { get; internal set; }
        public DeftRoute FullRoute { get; internal set; }
        public DeftRoute Route { get; internal set; }
        public string BodyJSON { get; internal set; }
    }

    public class DeftRequest<TRequestType>
    {
        public Dictionary<string, string> Headers { get; set; }
        public TRequestType Body { get; set; }
    }
}
