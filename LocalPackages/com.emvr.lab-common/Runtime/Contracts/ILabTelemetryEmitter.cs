namespace EMVR.Common.Contracts
{
    /// <summary>
    /// Emits stable event names and JSON payloads without coupling shared
    /// interaction components to a transport or analytics provider.
    /// </summary>
    public interface ILabTelemetryEmitter
    {
        void Emit(string eventType, string payloadJson);
    }
}
