using EMVR.Common.Interaction;
using EMVR.Common.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.Validation
{
    /// <summary>
    /// Exposes enough XRI state in the minimal scene to distinguish an aiming
    /// problem from a select-input problem.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrXrValidationDiagnostics : MonoBehaviour
    {
        public EmVrMinimalValidationViewMode viewMode;
        public EmVrStatusDisplay statusDisplay;
        [Min(0.05f)] public float refreshInterval = 0.1f;

        float nextRefreshTime;

        void Update()
        {
            if (statusDisplay == null || Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
            if (viewMode != null && viewMode.IsDesktopView)
            {
                statusDisplay.SetStatus("xr_input", "F2 to inspect");
                return;
            }

            XRRayInteractor[] rays = FindObjectsOfType<XRRayInteractor>(true);
            int activeRayCount = 0;
            bool hasSelection = false;
            string target = "none";
            for (int i = 0; i < rays.Length; i++)
            {
                XRRayInteractor ray = rays[i];
                if (!EmVrXrRayUtility.IsControllerRay(ray) || !ray.isActiveAndEnabled)
                {
                    continue;
                }

                activeRayCount++;
                hasSelection |= ray.hasSelection;

                RaycastHit hit;
                if (!ray.TryGetCurrent3DRaycastHit(out hit) || hit.collider == null)
                {
                    continue;
                }

                XRGrabInteractable grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
                target = grab != null ? "GRABBABLE: " + grab.name : "blocked by: " + hit.collider.name;
                if (grab != null)
                {
                    break;
                }
            }

            statusDisplay.SetStatus("xr_rays", activeRayCount + " active");
            statusDisplay.SetStatus("xr_target", target);
            statusDisplay.SetStatus(
                "xr_select",
                hasSelection ? "ACTIVE" : "LEFT MOUSE trigger");
        }
    }
}
