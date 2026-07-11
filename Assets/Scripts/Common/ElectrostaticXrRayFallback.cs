using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticXrRayFallback : MonoBehaviour
    {
        public float rayLength = 8f;
        public float rayWidth = 0.025f;
        public Color rayColor = new Color(0.25f, 0.92f, 1f, 0.95f);

        readonly Dictionary<Transform, LineRenderer> fallbackLines = new Dictionary<Transform, LineRenderer>();
        Material rayMaterial;
        float nextMaintenanceTime;

        void LateUpdate()
        {
            if (Time.unscaledTime >= nextMaintenanceTime)
            {
                nextMaintenanceTime = Time.unscaledTime + 1f;
                DisableInvalidLineVisuals();
            }

            DrawFallbackRays();
        }

        void DisableInvalidLineVisuals()
        {
            XRInteractorLineVisual[] lineVisuals = FindObjectsOfType<XRInteractorLineVisual>(true);
            for (int i = 0; i < lineVisuals.Length; i++)
            {
                XRInteractorLineVisual lineVisual = lineVisuals[i];
                if (lineVisual != null && lineVisual.GetComponent<XRRayInteractor>() == null)
                {
                    lineVisual.enabled = false;
                }
            }
        }

        void DrawFallbackRays()
        {
            XRBaseControllerInteractor[] interactors = FindObjectsOfType<XRBaseControllerInteractor>(true);
            for (int i = 0; i < interactors.Length; i++)
            {
                XRBaseControllerInteractor interactor = interactors[i];
                if (interactor == null || !interactor.isActiveAndEnabled)
                {
                    continue;
                }

                DrawRayFor(interactor.transform);
            }
        }

        void DrawRayFor(Transform rayOrigin)
        {
            LineRenderer lineRenderer = GetLine(rayOrigin);
            Vector3 start = rayOrigin.position;
            Vector3 end = start + rayOrigin.forward * rayLength;

            RaycastHit hit;
            if (Physics.Raycast(start, rayOrigin.forward, out hit, rayLength))
            {
                end = hit.point;
            }

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        LineRenderer GetLine(Transform rayOrigin)
        {
            LineRenderer lineRenderer;
            if (fallbackLines.TryGetValue(rayOrigin, out lineRenderer) && lineRenderer != null)
            {
                return lineRenderer;
            }

            GameObject lineObject = new GameObject("Electrostatics XR Fallback Ray");
            lineObject.transform.SetParent(rayOrigin, false);
            lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.numCapVertices = 4;
            lineRenderer.startWidth = rayWidth;
            lineRenderer.endWidth = rayWidth;
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.sharedMaterial = RayMaterial();

            fallbackLines[rayOrigin] = lineRenderer;
            return lineRenderer;
        }

        Material RayMaterial()
        {
            if (rayMaterial == null)
            {
                rayMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            return rayMaterial;
        }
    }
}
