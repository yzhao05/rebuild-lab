using System;
using EMVR.Common.Assist;
using EMVR.Common.Objects;
using EMVR.Common.Telemetry;
using EMVR.Common.UI;
using UnityEngine;

namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Connects the reusable drag lifecycle to visible status, telemetry, and
    /// assist selection without adding lab-specific physics behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrDragFeedbackBridge : MonoBehaviour, IEmVrDragObserver
    {
        public EmVrLabObject labObject;
        public EmVrStatusDisplay statusDisplay;
        public EmVrTelemetryEmitter telemetryEmitter;
        public EmVrAssistGroundingProvider assistGrounding;

        [Header("Status keys")]
        public string selectedStatusKey = "selected";
        public string dragStatusKey = "drag";
        public string positionStatusKey = "position";

        [Header("Telemetry")]
        [Min(0.02f)]
        public float movedEventInterval = 0.15f;

        float nextMovedEventTime;

        void Reset()
        {
            labObject = GetComponent<EmVrLabObject>();
        }

        void Awake()
        {
            if (labObject == null)
            {
                labObject = GetComponent<EmVrLabObject>();
            }
        }

        public void OnEmVrDragStarted(Transform target)
        {
            if (statusDisplay != null)
            {
                statusDisplay.SetStatus(selectedStatusKey, DisplayName());
                statusDisplay.SetStatus(dragStatusKey, "active");
                statusDisplay.SetStatus(positionStatusKey, FormatPosition(target.position));
            }

            if (assistGrounding != null)
            {
                assistGrounding.SetSelectedObject(labObject);
            }

            Emit("object_drag_started", target.position);
            nextMovedEventTime = Time.unscaledTime + Mathf.Max(0.02f, movedEventInterval);
        }

        public void OnEmVrDragMoved(Transform target, Vector3 worldPosition)
        {
            if (statusDisplay != null)
            {
                statusDisplay.SetStatus(positionStatusKey, FormatPosition(worldPosition));
            }

            if (Time.unscaledTime >= nextMovedEventTime)
            {
                Emit("object_drag_moved", worldPosition);
                nextMovedEventTime = Time.unscaledTime + Mathf.Max(0.02f, movedEventInterval);
            }
        }

        public void OnEmVrDragEnded(Transform target)
        {
            if (statusDisplay != null)
            {
                statusDisplay.SetStatus(dragStatusKey, "complete");
                statusDisplay.SetStatus(positionStatusKey, FormatPosition(target.position));
            }

            Emit("object_drag_ended", target.position);
        }

        void Emit(string eventType, Vector3 position)
        {
            if (telemetryEmitter == null)
            {
                return;
            }

            DragEventPayload payload = new DragEventPayload
            {
                object_id = labObject != null ? labObject.ObjectId : gameObject.name,
                object_type = labObject != null ? labObject.ObjectType : "Unknown",
                position = position
            };
            telemetryEmitter.Emit(eventType, JsonUtility.ToJson(payload));
        }

        string DisplayName()
        {
            return labObject != null ? labObject.DisplayName : gameObject.name;
        }

        static string FormatPosition(Vector3 position)
        {
            return string.Format("({0:0.00}, {1:0.00}, {2:0.00})", position.x, position.y, position.z);
        }

        [Serializable]
        sealed class DragEventPayload
        {
            public string object_id;
            public string object_type;
            public Vector3 position;
        }
    }
}
