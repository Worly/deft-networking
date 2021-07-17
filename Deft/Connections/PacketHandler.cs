using Deft.Utils;
using System;
using System.Collections.Generic;
using System.Net;

namespace Deft
{
    internal static class PacketHandler
    {
        private static Dictionary<DeftPacket, Action<DeftConnection, ByteBuffer>> handlers = new Dictionary<DeftPacket, Action<DeftConnection, ByteBuffer>>();

        static PacketHandler()
        {
            handlers.Add(DeftPacket.BeginHandshake, ReceivedBeginHandshake);
            handlers.Add(DeftPacket.IdToken, ReceivedIdToken);
            handlers.Add(DeftPacket.ClientIdentified, ReceivedClientIdentified);
            handlers.Add(DeftPacket.Method, ReceivedMethod);
            handlers.Add(DeftPacket.MethodResponse, ReceivedMethodResponse);
        }

        public static void Handle(DeftConnection DeftConnection, DeftPacket DeftPacket, ByteBuffer byteBuffer)
        {
            if (handlers.TryGetValue(DeftPacket, out Action<DeftConnection, ByteBuffer> handler))
                handler(DeftConnection, byteBuffer);
            else
                Logger.LogError("Missing handler for packet type: " + DeftPacket);
        }

        private static void ReceivedBeginHandshake(DeftConnection connection, ByteBuffer byteBuffer)
        {
            Logger.LogDebug($"Received BeginHandshake from {connection}");
            connection.ReceivedBeginHandshake();
        }

        private static void ReceivedIdToken(DeftConnection connection, ByteBuffer byteBuffer)
        {
            var idToken = byteBuffer.ReadString();

            Logger.LogDebug($"Received IdToken from {connection}");

            connection.ReceivedIdToken(idToken);
        }

        private static void ReceivedClientIdentified(DeftConnection connection, ByteBuffer byteBuffer)
        {
            var clientId = byteBuffer.ReadInteger();
            var idToken = byteBuffer.ReadString();

            Logger.LogDebug($"Received ClientIdentified from {connection}");

            connection.ReceivedClientIdenitified(clientId, idToken);
        }

        private static void ReceivedMethod(DeftConnection connection, ByteBuffer byteBuffer)
        {
            var requestDTO = new DeftRequestDTO();
            requestDTO.MethodIndex = byteBuffer.ReadUInteger();
            requestDTO.MethodRoute = byteBuffer.ReadString();
            requestDTO.HeadersJSON = byteBuffer.ReadString();
            requestDTO.BodyJSON = byteBuffer.ReadString();

            Logger.LogDebug($"Received Method (index: {requestDTO.MethodIndex}, route: {requestDTO.MethodRoute}) from {connection}, headers: {requestDTO.HeadersJSON}, body: {requestDTO.BodyJSON}");

            DeftMethods.ReceivedMethod(connection, requestDTO);
        }

        private static void ReceivedMethodResponse(DeftConnection connection, ByteBuffer byteBuffer)
        {
            var responseDTO = new DeftResponseDTO();
            responseDTO.MethodIndex = byteBuffer.ReadUInteger();
            responseDTO.StatusCode = (ResponseStatusCode)byteBuffer.ReadInteger();
            responseDTO.HeadersJSON = byteBuffer.ReadString();
            responseDTO.BodyJSON = byteBuffer.ReadString();

            Logger.LogDebug($"Received MethodResponse (index: {responseDTO.MethodIndex}) from {connection}, status code: {responseDTO.StatusCode}, headers: {responseDTO.HeadersJSON}, body: {responseDTO.BodyJSON}");

            DeftMethods.ReceivedResponse(connection, responseDTO);
        }
    }
}
