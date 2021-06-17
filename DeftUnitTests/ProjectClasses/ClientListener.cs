namespace DeftUnitTests
{
    class ClientListener : Deft.ClientListener<Client>
    {
        public ClientListener(int port) : base(port)
        {

        }

    }


    class AnyListener<T> : Deft.ClientListener<T> where T : Deft.Client, new()
    {
        public AnyListener(int port) : base(port) { }
    }
}
