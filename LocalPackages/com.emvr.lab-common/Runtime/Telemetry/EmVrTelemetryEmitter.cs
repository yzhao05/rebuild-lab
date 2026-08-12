using System;
using System.Collections.Generic;
using EMVR.Common.Contracts;
using UnityEngine;

namespace EMVR.Common.Telemetry
{
    /// <summary>
    /// In-memory telemetry emitter with stable event names and a bounded recent
    /// history. Network or file transports subscribe to EventEmitted.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrTelemetryEmitter : MonoBehaviour, ILabTelemetryEmitter
    {
        [Serializable]
        public sealed class TelemetryEvent
        {
            public string event_type;
            public string payload_json;
            public long timestamp_ms;
            public int frame;
        }

        [Serializable]
        sealed class TelemetryEventCollection
        {
            public List<TelemetryEvent> events = new List<TelemetryEvent>();
        }

        [Min(1)]
        public int historyCapacity = 64;
        public bool logToConsole;

        readonly List<TelemetryEvent> recentEvents = new List<TelemetryEvent>();

        public event Action<TelemetryEvent> EventEmitted;

        public IReadOnlyList<TelemetryEvent> RecentEvents
        {
            get { return recentEvents; }
        }

        public void Emit(string eventType, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                Debug.LogError("EmVrTelemetryEmitter requires a non-empty event type.", this);
                return;
            }

            TelemetryEvent telemetryEvent = new TelemetryEvent
            {
                event_type = eventType.Trim(),
                payload_json = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson,
                timestamp_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                frame = Time.frameCount
            };

            recentEvents.Add(telemetryEvent);
            int capacity = Mathf.Max(1, historyCapacity);
            while (recentEvents.Count > capacity)
            {
                recentEvents.RemoveAt(0);
            }

            if (logToConsole)
            {
                Debug.Log("[EMVR telemetry] " + telemetryEvent.event_type + " " + telemetryEvent.payload_json, this);
            }

            if (EventEmitted != null)
            {
                EventEmitted(telemetryEvent);
            }
        }

        public string ExportRecentEventsJson()
        {
            TelemetryEventCollection collection = new TelemetryEventCollection();
            collection.events.AddRange(recentEvents);
            return JsonUtility.ToJson(collection);
        }

        public void ClearHistory()
        {
            recentEvents.Clear();
        }

        void OnValidate()
        {
            historyCapacity = Mathf.Max(1, historyCapacity);
        }
    }
}
