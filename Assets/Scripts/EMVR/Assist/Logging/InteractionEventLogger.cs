using System;
using System.Collections.Generic;
using System.Linq;
using EMVR.Assist.Core;
using EMVR.Assist.Data;
using EMVR.Assist.Objects;
using UnityEngine;

namespace EMVR.Assist.Logging
{
    public class InteractionEventLogger : MonoBehaviour
    {
        public static InteractionEventLogger Instance { get; private set; }

        [SerializeField] private int maxRecentEvents = 50;

        private readonly Queue<InteractionEvent> _events = new Queue<InteractionEvent>();

        public long LastActionUnixMs { get; private set; }

        public event Action<InteractionEvent> OnActionLogged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LastActionUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public void LogAction(string actionType, ExperimentObject obj, string result = "success")
        {
            if (string.IsNullOrEmpty(actionType)) return;

            Vector3 p = obj != null ? obj.transform.position : Vector3.zero;
            string stepId = StepProgressManager.Instance != null
                ? StepProgressManager.Instance.CurrentStepId
                : "unknown";

            var e = new InteractionEvent
            {
                event_id = Guid.NewGuid().ToString(),
                timestamp_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                step_id = stepId,
                action_type = actionType,
                object_id = obj != null ? obj.objectId : "",
                object_type = obj != null ? obj.objectType.ToString() : "",
                result = result ?? "unknown",
                position = new[] { p.x, p.y, p.z }
            };

            _events.Enqueue(e);
            LastActionUnixMs = e.timestamp_ms;

            while (_events.Count > maxRecentEvents) _events.Dequeue();

            OnActionLogged?.Invoke(e);
        }

        public List<InteractionEvent> GetRecentEvents() => _events.ToList();

        public InteractionEvent GetLastEvent() => _events.Count == 0 ? null : _events.Last();

        public int CountTrailingRepeats()
        {
            if (_events.Count == 0) return 0;
            var arr = _events.ToArray();
            var last = arr[arr.Length - 1];
            string sig = last.action_type + ":" + last.object_id;
            int count = 0;
            for (int i = arr.Length - 1; i >= 0; i--)
            {
                if (arr[i].action_type + ":" + arr[i].object_id == sig) count++;
                else break;
            }
            return count;
        }
    }
}
