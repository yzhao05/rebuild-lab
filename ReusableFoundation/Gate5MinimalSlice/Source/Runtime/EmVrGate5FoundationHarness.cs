using EMVR.Common.Objects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EMVR.ReusableFoundation.Gate5
{
    /// <summary>
    /// Cross-Lab Gate 5 wiring probe. Domain code composes around this component;
    /// it must not be copied and edited into a Lab-specific implementation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrGate5FoundationHarness : MonoBehaviour
    {
        public Button spawnButton;
        public EmVrReplacementSpawner replacementSpawner;
        public Camera validationCamera;

        public GameObject SpawnedObject { get; private set; }

        public void SpawnAndValidate()
        {
            SpawnedObject = replacementSpawner != null
                ? replacementSpawner.SpawnReplacement()
                : null;

            if (!TryValidateSpawnedVisibility(out string failure))
            {
                Debug.LogError($"[EMVR Gate 5 Foundation] {failure}", this);
            }
        }

        public bool TryValidateWiring(out string failure)
        {
            EventSystem[] eventSystems = FindObjectsOfType<EventSystem>(true);
            int activeEventSystems = 0;
            for (int i = 0; i < eventSystems.Length; i++)
            {
                if (eventSystems[i].isActiveAndEnabled)
                {
                    activeEventSystems++;
                }
            }

            if (activeEventSystems != 1)
            {
                failure = $"Expected exactly one active EventSystem, found {activeEventSystems}.";
                return false;
            }

            if (spawnButton == null || !spawnButton.isActiveAndEnabled || !spawnButton.interactable)
            {
                failure = "The required spawn button is missing, inactive, disabled, or not interactable.";
                return false;
            }

            if (replacementSpawner == null || replacementSpawner.replacementPrefab == null)
            {
                failure = "Common EmVrReplacementSpawner or its replacement prefab is missing.";
                return false;
            }

            if (!replacementSpawner.replacementPrefab.activeSelf)
            {
                failure = "The replacement prefab is inactive. Unity clones its inactive state, so the spawned object would be invisible.";
                return false;
            }

            if (validationCamera == null || !validationCamera.isActiveAndEnabled)
            {
                failure = "The validation camera is missing or inactive.";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        public bool TryValidateSpawnedVisibility(out string failure)
        {
            if (SpawnedObject == null)
            {
                failure = "The button did not produce a spawned object.";
                return false;
            }

            if (!SpawnedObject.activeInHierarchy)
            {
                failure = "The spawned object exists but is inactive in the hierarchy.";
                return false;
            }

            Renderer[] renderers = SpawnedObject.GetComponentsInChildren<Renderer>(true);
            Renderer visibleRenderer = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled && renderers[i].gameObject.activeInHierarchy)
                {
                    visibleRenderer = renderers[i];
                    break;
                }
            }

            if (visibleRenderer == null)
            {
                failure = "The spawned object has no active enabled Renderer.";
                return false;
            }

            if (SpawnedObject.GetComponentInChildren<Collider>(true) == null)
            {
                failure = "The spawned object has no Collider for desktop/XR interaction.";
                return false;
            }

            if (validationCamera == null)
            {
                failure = "The validation camera is missing.";
                return false;
            }

            Vector3 viewport = validationCamera.WorldToViewportPoint(visibleRenderer.bounds.center);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            {
                failure = $"The spawned object is outside the validation camera (viewport {viewport}).";
                return false;
            }

            failure = string.Empty;
            return true;
        }
    }
}
