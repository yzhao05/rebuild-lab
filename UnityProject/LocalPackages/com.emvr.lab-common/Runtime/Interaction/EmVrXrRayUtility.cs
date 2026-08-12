using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.Interaction
{
    internal static class EmVrXrRayUtility
    {
        public static bool IsControllerRay(XRRayInteractor ray)
        {
            return ray != null && ray.GetComponentInParent<XRBaseController>() != null;
        }

        public static Transform Origin(XRRayInteractor ray)
        {
            if (ray == null)
            {
                return null;
            }

            return ray.rayOriginTransform != null ? ray.rayOriginTransform : ray.transform;
        }
    }
}
