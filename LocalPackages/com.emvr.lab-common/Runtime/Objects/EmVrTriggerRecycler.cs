using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EMVR.Common.Objects
{
    /// <summary>
    /// Safely disables or destroys accepted objects that enter this trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrTriggerRecycler : MonoBehaviour
    {
        public enum RecycleMode
        {
            Disable,
            Destroy
        }

        public RecycleMode recycleMode = RecycleMode.Disable;
        public LayerMask acceptedLayers = ~0;

        [Tooltip("Leave empty to accept every tag allowed by acceptedLayers.")]
        public string requiredTag;

        [Tooltip("Recycle the Rigidbody root rather than only the child Collider.")]
        public bool useAttachedRigidbodyRoot = true;

        [Tooltip("Prevent this trigger, its children, or its parents from being recycled.")]
        public bool protectRecyclerHierarchy = true;

        [Min(0f)]
        public float destroyDelay;

        readonly HashSet<int> scheduledObjects = new HashSet<int>();

        public event Action<GameObject> ObjectRecycled;

        void OnDisable()
        {
            scheduledObjects.Clear();
        }

        void OnTriggerEnter(Collider other)
        {
            TryRecycle(other);
        }

        public bool TryRecycle(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            GameObject target = ResolveTarget(other);
            if (target == null || !IsAccepted(target))
            {
                return false;
            }

            int instanceId = target.GetInstanceID();
            if (!scheduledObjects.Add(instanceId))
            {
                return false;
            }

            if (ObjectRecycled != null)
            {
                ObjectRecycled(target);
            }

            if (recycleMode == RecycleMode.Disable)
            {
                target.SetActive(false);
                scheduledObjects.Remove(instanceId);
            }
            else
            {
                StartCoroutine(DestroyAndForget(target, instanceId));
            }

            return true;
        }

        IEnumerator DestroyAndForget(GameObject target, int instanceId)
        {
            float delay = Mathf.Max(0f, destroyDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }

            if (target != null)
            {
                Destroy(target);
            }

            scheduledObjects.Remove(instanceId);
        }

        GameObject ResolveTarget(Collider other)
        {
            if (useAttachedRigidbodyRoot && other.attachedRigidbody != null)
            {
                return other.attachedRigidbody.gameObject;
            }

            return other.gameObject;
        }

        bool IsAccepted(GameObject target)
        {
            int layerBit = 1 << target.layer;
            if ((acceptedLayers.value & layerBit) == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requiredTag) && !target.CompareTag(requiredTag))
            {
                return false;
            }

            if (protectRecyclerHierarchy)
            {
                Transform targetTransform = target.transform;
                if (targetTransform == transform
                    || targetTransform.IsChildOf(transform)
                    || transform.IsChildOf(targetTransform))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
