namespace EMVR.Common.Contracts
{
    /// <summary>
    /// Extends a lab object's exported state with reversible import and a
    /// scene-authored reset point. Implementations must reject malformed state
    /// rather than silently applying a partial snapshot.
    /// </summary>
    public interface IRestorableLabObjectStateProvider : ILabObjectStateProvider
    {
        bool TryImportStateJson(string stateJson, out string error);
        void CaptureInitialState();
        bool TryRestoreInitialState(out string error);
    }
}
