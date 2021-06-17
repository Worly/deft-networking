using System.Net;

namespace Deft
{
    internal class DeftResponseDTO
    {
        public uint MethodIndex { get; set; }
        public ResponseStatusCode StatusCode { get; set; }
        public string HeadersJSON { get; set; }
        public string BodyJSON { get; set; }
    }
}
