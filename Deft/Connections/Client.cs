using Deft.Utils;

namespace Deft
{
    public abstract class Client : DeftConnectionOwner
    {
        private static int NextClientId = 1;

        public int ClientId { get; private set; }


        internal string idToken;

        public Client()
        {
            this.ClientId = NextClientId++;
            idToken = IdTokenManager.GenerateIdToken();
        }
    }
}
