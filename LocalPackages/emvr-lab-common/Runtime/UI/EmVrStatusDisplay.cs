using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EMVR.Common.UI
{
    /// <summary>
    /// Maintains ordered, visible key/value feedback such as selected object,
    /// active objects, preset, and result.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrStatusDisplay : MonoBehaviour
    {
        [Serializable]
        public sealed class StatusEntry
        {
            public string key;
            public string value;
        }

        public TextMesh worldText;
        [TextArea] public string emptyMessage = "ready";
        public string separator = ": ";
        public List<StatusEntry> initialEntries = new List<StatusEntry>();

        readonly List<StatusEntry> entries = new List<StatusEntry>();
        readonly StringBuilder builder = new StringBuilder(256);

        public event Action<string> StatusChanged;

        public string CurrentText { get; private set; }

        void Awake()
        {
            entries.Clear();
            for (int i = 0; i < initialEntries.Count; i++)
            {
                StatusEntry initial = initialEntries[i];
                if (initial != null && !string.IsNullOrWhiteSpace(initial.key))
                {
                    SetStatusInternal(initial.key, initial.value, false);
                }
            }

            RefreshDisplay();
        }

        public void SetStatus(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError("EmVrStatusDisplay requires a non-empty status key.", this);
                return;
            }

            SetStatusInternal(key.Trim(), value, true);
        }

        public bool RemoveStatus(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (!string.Equals(entries[i].key, key.Trim(), StringComparison.Ordinal))
                {
                    continue;
                }

                entries.RemoveAt(i);
                RefreshDisplay();
                return true;
            }

            return false;
        }

        public void ClearStatus()
        {
            entries.Clear();
            RefreshDisplay();
        }

        public string GetStatus(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].key, key.Trim(), StringComparison.Ordinal))
                {
                    return entries[i].value;
                }
            }

            return string.Empty;
        }

        void SetStatusInternal(string key, string value, bool refresh)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (!string.Equals(entries[i].key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                entries[i].value = value ?? string.Empty;
                if (refresh)
                {
                    RefreshDisplay();
                }

                return;
            }

            entries.Add(new StatusEntry
            {
                key = key,
                value = value ?? string.Empty
            });

            if (refresh)
            {
                RefreshDisplay();
            }
        }

        void RefreshDisplay()
        {
            builder.Length = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(entries[i].key);
                builder.Append(separator);
                builder.Append(entries[i].value);
            }

            CurrentText = builder.Length == 0 ? emptyMessage ?? string.Empty : builder.ToString();
            if (worldText != null)
            {
                worldText.text = CurrentText;
            }

            if (StatusChanged != null)
            {
                StatusChanged(CurrentText);
            }
        }
    }
}
