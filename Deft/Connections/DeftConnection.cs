using Deft.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Deft
{
    public class DeftConnection
    {
        public string ConnectionIdentifier { get; private set; }
        private TcpClient tcpClient;
        private NetworkStream dataStream;

        internal ClientListener OwnerClientListener { get; set; }
        internal DeftConnectionOwner Owner { get; set; }
        internal bool HandshakeSuccessful { get; set; } = false;

        public IPEndPoint RemoteEndPoint { get; set; }

        private byte[] recieveBuffer;

        private SmartByteBuffer currentPacketByteBuffer = new SmartByteBuffer();
        private int? currentPacketLength = null;

        /// <summary>
        /// OnClientIdentified(int myClientId)
        /// </summary>
        internal Action<int> OnClientIdentified;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private bool closingConnection = false;

        internal DeftConnection(string connectionIdentifier)
        {
            this.ConnectionIdentifier = connectionIdentifier;
        }

        internal DeftConnection(ClientListener ownerClientListener)
        {
            this.OwnerClientListener = ownerClientListener;
        }

        internal void Connect(TcpClient connectedClient)
        {
            if (connectedClient == null)
                throw new ArgumentNullException("connectedClient");

            tcpClient = connectedClient;

            recieveBuffer = new byte[connectedClient.ReceiveBufferSize];

            dataStream = tcpClient.GetStream();

            RemoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            Deft.StopEvent += Deft_Stop;

            Logger.LogDebug("Established TCP connection to " + RemoteEndPoint);

            BeginRead();
        }

        public void CloseConnection()
        {
            Deft.StopEvent -= Deft_Stop;

            if (closingConnection)
                return;

            closingConnection = true;

            cancellationTokenSource.Cancel();

            if (tcpClient != null && tcpClient.Connected)
            {
                if (dataStream != null)
                    dataStream.Close();

                if (tcpClient != null)
                    tcpClient.Close();
                tcpClient = null;
            }

            if (Owner != null)
                this.Owner.OnDisconnectedInternal();
        }

        private void Deft_Stop(object sender, EventArgs e)
        {
            CloseConnection();
        }

        public bool IsConnected()
        {
            return tcpClient != null && tcpClient.Client != null && tcpClient.Client.Connected;
        }

        private void BeginRead()
        {
            try
            {
                dataStream.BeginRead(recieveBuffer, 0, recieveBuffer.Length, RecieveData, null);
            }
            catch (Exception e)
            {
                Logger.LogError("Error reading TCP stream, see exception: " + e.ToString());
                CloseConnection();
            }
        }

        protected void RecieveData(IAsyncResult result)
        {
            try
            {
                if (!dataStream.CanRead)
                    return;

                int size = dataStream.EndRead(result);

                if (size == 0)
                {
                    CloseConnection();
                    return;
                }

                ReassembleAndHandle(recieveBuffer, size);

                BeginRead();
            }
            catch (Exception e)
            {
                if (e.InnerException is SocketException socketException && socketException.SocketErrorCode == SocketError.ConnectionReset)
                    CloseConnection();
                else
                    Logger.LogError("Error receiving TCP data, see exception: " + e.ToString());
            }
        }

        void ReassembleAndHandle(byte[] data, int size)
        {
            Logger.LogDebug($"[TCP] Received packet from: {this}, size: {size}, data: " + data.ToHexString());

            var byteBuffer = new ByteBuffer(size);
            byteBuffer.WriteBytes(data, size);
            byteBuffer.SetReadPosition(0);

            while (byteBuffer.Length() > 0)
            {
                if (this.currentPacketLength == null)
                {
                    this.currentPacketLength = byteBuffer.ReadInteger();
                    if (this.currentPacketLength <= 0)
                    {
                        Logger.LogError("[TCP] packet length is less or equal to zero!!");
                        return;
                    }
                }

                var readSize = this.currentPacketLength.Value - this.currentPacketByteBuffer.GetSize();
                if (byteBuffer.Length() < readSize)
                    readSize = byteBuffer.Length();

                this.currentPacketByteBuffer.WriteBytes(byteBuffer.ReadBytes(readSize));

                if (currentPacketByteBuffer.GetSize() == this.currentPacketLength)
                {
                    var packet = currentPacketByteBuffer.GetBytes();

                    Logger.LogDebug("[TCP] Data completed!");
                    DeftThread.ExecuteOnDeftThread(() => HandlePackage(packet));

                    this.currentPacketLength = null;
                    this.currentPacketByteBuffer.Reset();
                }
                else
                    Logger.LogDebug("[TCP] Data is split in multiple packets, waiting for next one...");
            }
        }


        void HandlePackage(byte[] data)
        {
            Logger.LogDebug("[TCP] Handling packet " + data.ToHexString());

            ByteBuffer buffer = new ByteBuffer(data);
            byte toInvoke = buffer.ReadByte();

            if (!Enum.IsDefined(typeof(DeftPacket), (int)toInvoke))
            {
                Logger.LogError($"Received unknown packet {toInvoke} from {RemoteEndPoint}. Ignoring the packet...");
                return;
            }

            var deftPacket = (DeftPacket)toInvoke;

            PacketHandler.Handle(this, deftPacket, buffer);
        }

        /// <summary>
        /// Thread safe
        /// </summary>
        public void SendData(byte[] data)
        {
            DeftThread.ExecuteOnDeftThread(() => SendDataInternal(data));
        }

        private void SendDataInternal(byte[] data)
        {
            if (tcpClient == null || !tcpClient.Connected)
                return;

            ByteBuffer buffer = new ByteBuffer(data.Length + 4);
            buffer.WriteInteger((data.GetUpperBound(0) - data.GetLowerBound(0)) + 1); // packet size
            buffer.WriteBytes(data);

            var array = buffer.ToArray();

            dataStream.BeginWrite(array, 0, array.Length, new AsyncCallback(OnSendDataCompleted), dataStream);
        }

        void OnSendDataCompleted(IAsyncResult asyncResult)
        {
            NetworkStream myNetworkStream = (NetworkStream)asyncResult.AsyncState;
            myNetworkStream.EndWrite(asyncResult);
        }

        public override string ToString()
        {
            return $"[DeftConnection on {RemoteEndPoint}]";
        }

        #region Handshake

        internal void StartHandshake()
        {
            Task.Delay(DeftConfig.HandshakeTimeoutMs, cancellationTokenSource.Token)
                .ContinueWith(t => CheckHandshakeTimeout(), TaskContinuationOptions.NotOnCanceled);
            PacketBuilder.SendBeginHandshake(this);
        }

        internal void ReceivedBeginHandshake()
        {
            PacketBuilder.SendIdToken(this, IdTokenManager.GetIdToken(this.ConnectionIdentifier));
        }

        internal void ReceivedIdToken(string idToken)
        {
            if (OwnerClientListener == null)
            {
                Logger.LogError("Received IdToken but my OwnerClientListener is null, which means i'm not on server application");
                return;
            }

            OwnerClientListener.ReceivedIdToken(this, idToken);
        }

        internal void ReceivedClientIdenitified(int clientId, string idToken)
        {
            HandshakeSuccessful = true;
            IdTokenManager.SaveIdToken(this.ConnectionIdentifier, idToken);
            if (this.OnClientIdentified == null)
                Logger.LogError("ClientIdentified but callback method is null, nothing will happen with this connection");
            else
                this.OnClientIdentified(clientId);
        }

        private void CheckHandshakeTimeout()
        {
            if (HandshakeSuccessful)
                return;

            Logger.LogError($"Handshake timeout on {this}");

            CloseConnection();
        }

        #endregion
    }
}