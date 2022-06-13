namespace Deft
{
    internal enum DeftPacket
    {
        // Handshake packets - (S - Server, C - Client)
        // 1. S -> C - BeginHandshake    (begins handshake)
        // 2. C -> S - IdTokenAndVersion (client responds with IdToken to identify itself, and version)
        // 3. S -> C - ClientIdentified  (server returns if versions are matching, masterReference, and new IdToken (or old) which client saves)
        BeginHandshake = 0,
        IdToken,
        ClientIdentified,
        Method,
        MethodResponse,
        HealthCheck,
        HealthCheckResponse
    }
}
