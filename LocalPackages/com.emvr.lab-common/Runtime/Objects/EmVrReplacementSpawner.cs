using System;
using UnityEngine;

namespace EMVR.Common.Objects
{
    /// <summary>
    /// Spawns a replacement at a captured origin when this object first moves
    /// outside a configured radius.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrReplacementSpawner : MonoBehaviour
    {
        public GameObject replacementPrefab;

        [Tooltip("Optional origin for replacements. This object's initial pose is used when empty.")]
        public Transform spawnAnchor;
        public Transform replacementParent;

        [Min(0f)]
        public float movementThreshold = 0.25f;

        [Tooltip("Zero allows unlimited edge-triggered replacements.")]
        [Min(0)]
        public int maximumReplacementCount = 1;

        public bool captureOriginOnEnable = true;

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        bool originCaptured;
        bool wasOutside;
        int replacementCount;

        public event Action<GameObject> ReplacementSpawned;

        public int ReplacementCount
        {
            get { return replacementCount; }
        }

        void OnEnable()
        {
            if (captureOriginOnEnable)
            {
                CaptureOrigin();
            }

            wasOutside = false;
        }

        void Start()
        {
            if (!originCaptured)
            {
                CaptureOrigin();
            }
        }

        void Update()
        {
            if (!originCaptured || replacementPrefab == null)
            {
                return;
            }

            float threshold = Mathf.Max(0f, movementThreshold);
            bool outside = (transform.position - spawnPosition).sqrMagnitude > threshold * threshold;
            if (outside && !wasOutside && CanSpawnAnother())
            {
                SpawnReplacement();
            }

            wasOutside = outside;
        }

        public void CaptureOrigin()
        {
            Transform source = spawnAnchor != null ? spawnAnchor : transform;
            spawnPosition = source.position;
            spawnRotation = source.rotation;
            originCaptured = true;
            wasOutside = false;
        }

        public GameObject SpawnReplacement()
        {
            if (replacementPrefab == null)
            {
                Debug.LogError("EmVrReplacementSpawner requires a replacement prefab.", this);
                return null;
            }

            if (!originCaptured)
            {
                CaptureOrigin();
            }

            if (!CanSpawnAnother())
            {
                return null;
            }

            GameObject replacement = Instantiate(
                replacementPrefab,
                spawnPosition,
                spawnRotation,
                replacementParent);

            replacementCount++;
            if (ReplacementSpawned != null)
            {
                ReplacementSpawned(replacement);
            }

            return replacement;
        }

        public void ResetSpawner()
        {
            replacementCount = 0;
            CaptureOrigin();
        }

        bool CanSpawnAnother()
        {
            return maximumReplacementCount == 0 || replacementCount < maximumReplacementCount;
        }
    }
}
