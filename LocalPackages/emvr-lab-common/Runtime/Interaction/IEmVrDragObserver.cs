using UnityEngine;

namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Receives drag lifecycle notifications without coupling reusable drag
    /// behavior to a lab's physics or progression rules.
    /// </summary>
    public interface IEmVrDragObserver
    {
        void OnEmVrDragStarted(Transform target);
        void OnEmVrDragMoved(Transform target, Vector3 worldPosition);
        void OnEmVrDragEnded(Transform target);
    }
}
