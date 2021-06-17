namespace Deft
{
    public abstract class DeftConnectionOwner
    {
        public DeftConnection Connection { get; private set; }

        public bool IsConnected { get => this.Connection != null && this.Connection.IsConnected(); }

        internal void Bind(DeftConnection connection)
        {
            if (this.Connection != null)
            {
                Logger.LogWarning("Binding client which is already bound to active connection, that shouldn't happen. Diconnecting first...");
                Disconnect();
            }

            this.Connection = connection;
            this.Connection.Owner = this;
            this.OnConnectedInternal();
        }

        public void Disconnect()
        {
            if (this.Connection == null)
                return;
            this.Connection.CloseConnection();
        }

        internal void OnConnectedInternal()
        {
            Logger.LogInfo($"Connected {this.Connection}");
            this.OnConnected();
        }

        public virtual void OnConnected() { }

        internal void OnDisconnectedInternal()
        {
            Logger.LogInfo($"Disconnected {this.Connection}");
            this.OnDisconnected();
            this.Connection = null;
        }

        public virtual void OnDisconnected() { }
    }
}
