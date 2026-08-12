using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Reusable form of the proven Lab1 XR ray tuner. It applies forgiving
    /// SphereCast settings and optionally bridges editor left-mouse input to
    /// XRI manual selection when the device simulator does not route Trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrXrRayRuntimeTuner : MonoBehaviour
    {
        static readonly List<IXRInteractable> ValidTargets = new List<IXRInteractable>();

        [Header("Ray tuning")]
        [Min(0f)] public float sphereCastRadius = 0.06f;
        [Min(0.1f)] public float minimumRayDistance = 20f;
        public bool controllerRaysOnly = true;

        [Header("Editor simulator select")]
        public bool enableMouseLeftDebugSelect = true;
        [Tooltip("Optional MonoBehaviour implementing IEmVrXrViewState.")]
        public MonoBehaviour xrViewStateSource;

        XRRayInteractor debugInteractor;
        IXRSelectInteractable debugInteractable;
        IEmVrXrViewState xrViewState;
        float nextRayRefreshRealtime;

        void Awake()
        {
            xrViewState = xrViewStateSource as IEmVrXrViewState;
        }

        IEnumerator Start()
        {
            for (int i = 0; i < 8; i++)
            {
                ApplyToExistingRays();
                yield return new WaitForSeconds(0.25f);
            }
        }

        void Update()
        {
            if (Time.realtimeSinceStartup >= nextRayRefreshRealtime)
            {
                ApplyToExistingRays();
                nextRayRefreshRealtime = Time.realtimeSinceStartup + 1f;
            }
        }

        void LateUpdate()
        {
            if (!enableMouseLeftDebugSelect || !XrViewIsActive())
            {
                EndDebugSelect();
                return;
            }

            if (EmVrDesktopInput.PrimaryPressedThisFrame)
            {
                BeginDebugSelect();
            }

            if (EmVrDesktopInput.PrimaryReleasedThisFrame)
            {
                EndDebugSelect();
            }
        }

        void OnDisable()
        {
            EndDebugSelect();
        }

        public void ResolveViewStateSource()
        {
            xrViewState = xrViewStateSource as IEmVrXrViewState;
        }

        public void ApplyToExistingRays()
        {
            XRRayInteractor[] rays = FindObjectsOfType<XRRayInteractor>(true);
            for (int i = 0; i < rays.Length; i++)
            {
                XRRayInteractor ray = rays[i];
                if (!AcceptRay(ray))
                {
                    continue;
                }

                ray.hitDetectionType = XRRayInteractor.HitDetectionType.SphereCast;
                ray.sphereCastRadius = Mathf.Max(ray.sphereCastRadius, sphereCastRadius);
                ray.maxRaycastDistance = Mathf.Max(ray.maxRaycastDistance, minimumRayDistance);
                ray.raycastTriggerInteraction = QueryTriggerInteraction.Collide;
            }
        }

        void BeginDebugSelect()
        {
            if (debugInteractable != null)
            {
                return;
            }

            XRRayInteractor[] rays = FindObjectsOfType<XRRayInteractor>(true);
            for (int i = 0; i < rays.Length; i++)
            {
                XRRayInteractor ray = rays[i];
                if (!AcceptRay(ray) || !ray.isActiveAndEnabled || ray.interactionManager == null)
                {
                    continue;
                }

                if (ray.hasSelection)
                {
                    return;
                }

                ValidTargets.Clear();
                ray.GetValidTargets(ValidTargets);
                for (int targetIndex = 0; targetIndex < ValidTargets.Count; targetIndex++)
                {
                    IXRSelectInteractable selectable =
                        ValidTargets[targetIndex] as IXRSelectInteractable;
                    if (selectable == null)
                    {
                        continue;
                    }

                    ray.StartManualInteraction(selectable);
                    debugInteractor = ray;
                    debugInteractable = selectable;
                    return;
                }
            }
        }

        void EndDebugSelect()
        {
            if (debugInteractor != null && debugInteractable != null)
            {
                debugInteractor.EndManualInteraction();
            }

            debugInteractor = null;
            debugInteractable = null;
        }

        bool AcceptRay(XRRayInteractor ray)
        {
            return ray != null && (!controllerRaysOnly || EmVrXrRayUtility.IsControllerRay(ray));
        }

        bool XrViewIsActive()
        {
            if (xrViewState == null && xrViewStateSource != null)
            {
                ResolveViewStateSource();
            }

            return xrViewState == null || xrViewState.IsXrViewActive;
        }
    }
}
