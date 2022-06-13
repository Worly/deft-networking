using Deft.Utils;
using System.Net;

namespace Deft
{
    internal static class PacketBuilder
    {
        public static void SendBeginHandshake(DeftConnection connection)
        {
            var byteBuffer = new ByteBuffer(sizeof(byte));

            byteBuffer.WriteByte((byte)DeftPacket.BeginHandshake);

            Logger.LogDebug($"Sending BeginHandshake to {connection}");

            connection.SendData(byteBuffer.GetBuffer());
        }

        public static void SendIdToken(DeftConnection connection, string idToken)
        {
            var byteBuffer = new ByteBuffer(sizeof(byte) + ByteBuffer.GetStringSize(idToken));

            byteBuffer.WriteByte((byte)DeftPacket.IdToken);
            byteBuffer.WriteString(idToken);

            Logger.LogDebug($"Sending IdToken to {connection}");

            connection.SendData(byteBuffer.GetBuffer());
        }

        public static void SendClientIdentified(DeftConnection connection, int clientId, string idToken)
        {
            var byteBuffer = new ByteBuffer(sizeof(byte) + sizeof(int) + idToken.Length + 1);

            byteBuffer.WriteByte((byte)DeftPacket.ClientIdentified);
            byteBuffer.WriteInteger(clientId);
            byteBuffer.WriteString(idToken);

            Logger.LogDebug($"Sending ClientIdentified to {connection}");

            connection.SendData(byteBuffer.GetBuffer());
        }

        public static void SendMethod(DeftConnection connection, DeftRequestDTO requestDTO)
        {
            var byteBuffer = new ByteBuffer(
                sizeof(byte) + // (byte)DeftPacket
                sizeof(uint) + // MethodIndex
                ByteBuffer.GetStringSize(requestDTO.MethodRoute) + // MethodRoute
                ByteBuffer.GetStringSize(requestDTO.HeadersJSON) + // HeadersJSON
                ByteBuffer.GetStringSize(requestDTO.BodyJSON) // BodyJSON
                );

            byteBuffer.WriteByte((byte)DeftPacket.Method);
            byteBuffer.WriteUInteger(requestDTO.MethodIndex);
            byteBuffer.WriteString(requestDTO.MethodRoute);
            byteBuffer.WriteString(requestDTO.HeadersJSON);
            byteBuffer.WriteString(requestDTO.BodyJSON);

            Logger.LogDebug($"Sending Method (index: {requestDTO.MethodIndex}, route: {requestDTO.MethodRoute}) to {connection}, headers: {requestDTO.HeadersJSON}, body: {requestDTO.BodyJSON}");

            connection.SendData(byteBuffer.GetBuffer());
        }

        public static void SendMethodResponse(DeftConnection connection, DeftResponseDTO responseDTO)
        {
            var byteBuffer = new ByteBuffer(
                sizeof(byte) + // (byte)DeftPacket
                sizeof(uint) + // MethodIndex
                sizeof(HttpStatusCode) + 1 + // StatusCode
                ByteBuffer.GetStringSize(responseDTO.HeadersJSON) + // HeadersJSON
                ByteBuffer.GetStringSize(responseDTO.BodyJSON) // BodyJSON
                );

            byteBuffer.WriteByte((byte)DeftPacket.MethodResponse);
            byteBuffer.WriteUInteger(responseDTO.MethodIndex);
            byteBuffer.WriteInteger((int)responseDTO.StatusCode);
            byteBuffer.WriteString(responseDTO.HeadersJSON);
            byteBuffer.WriteString(responseDTO.BodyJSON);

            Logger.LogDebug($"Sending MethodResponse (index: {responseDTO.MethodIndex}) to {connection}, status code: {responseDTO.StatusCode}, headers: {responseDTO.HeadersJSON}, body: {responseDTO.BodyJSON}");

            connection.SendData(byteBuffer.GetBuffer());
        }

        public static void SendHealthCheck(DeftConnection connection)
        {
            var byteBuffer = new ByteBuffer(sizeof(byte));

            byteBuffer.WriteByte((byte)DeftPacket.HealthCheck);

            Logger.LogDebug($"Sending HealthCheck to {connection}");

            connection.SendData(byteBuffer.GetBuffer());
        }

        public static void SendHealthCheckResponse(DeftConnection connection)
        {
            var byteBuffer = new ByteBuffer(sizeof(byte));

            byteBuffer.WriteByte((byte)DeftPacket.HealthCheckResponse);

            Logger.LogDebug($"Sending HealthCheckResponse to {connection}");

            connection.SendData(byteBuffer.GetBuffer());
        }
    }
}
