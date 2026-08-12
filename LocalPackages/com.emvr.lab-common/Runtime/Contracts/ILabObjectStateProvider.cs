namespace EMVR.Common.Contracts
{
    /// <summary>
    /// Exposes a stable lab object identity and a JSON snapshot for contracts,
    /// telemetry, validation, and AI-assist grounding.
    /// </summary>
    public interface ILabObjectStateProvider
    {
        string ObjectId { get; }
        string ObjectType { get; }
        string DisplayName { get; }
        string ExportStateJson();
    }
}
