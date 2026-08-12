using EMVR.Common.Objects;
using UnityEngine;

namespace EMVR.Common.Validation
{
    /// <summary>
    /// Identifies generated lifecycle scenes without relying on hierarchy names.
    /// The Lab marker also exposes the object whose authored state must survive a
    /// reset through scene reload.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrSceneLifecycleMarker : MonoBehaviour
    {
        public string sceneRole;
        public EmVrLabObject resetValidationObject;
    }
}
