namespace Deft
{
    public class Server : DeftConnectionOwner
    {
        public int MyClientId { get; private set; }

        internal void Bind(DeftConnection connection, int myClientId)
        {
            base.Bind(connection);
            this.MyClientId = myClientId;
        }
    }
}
