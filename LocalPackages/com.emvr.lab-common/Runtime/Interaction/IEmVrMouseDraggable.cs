using UnityEngine;

namespace EMVR.Common.Interaction
{
    public interface IEmVrMouseDraggable
    {
        void BeginMouseDrag(Camera cameraToUse, Vector3 screenPosition);
        void ContinueMouseDrag(Vector3 screenPosition);
        void EndMouseDrag();
    }
}
