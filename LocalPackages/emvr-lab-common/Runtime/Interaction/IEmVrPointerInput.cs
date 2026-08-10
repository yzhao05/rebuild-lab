using UnityEngine;

namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Optional input adapter for projects that do not want the shared pointer
    /// to read Unity's legacy Input API directly.
    /// </summary>
    public interface IEmVrPointerInput
    {
        Vector3 PointerScreenPosition { get; }
        bool PrimaryPressedThisFrame { get; }
        bool PrimaryHeld { get; }
        bool PrimaryReleasedThisFrame { get; }
    }
}
