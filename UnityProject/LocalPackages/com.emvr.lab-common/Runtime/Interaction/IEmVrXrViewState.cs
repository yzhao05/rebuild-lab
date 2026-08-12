namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Optional gate used by reusable XR debug helpers to avoid consuming
    /// desktop input while a non-XR spectator view is active.
    /// </summary>
    public interface IEmVrXrViewState
    {
        bool IsXrViewActive { get; }
    }
}
