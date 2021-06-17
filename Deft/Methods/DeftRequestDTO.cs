namespace Deft
{
    internal class DeftRequestDTO
    {
        public uint MethodIndex { get; set; }
        public string MethodRoute { get; set; }
        public string HeadersJSON { get; set; }
        public string BodyJSON { get; set; }
    }
}
