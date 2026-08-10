using UnityEngine;

namespace EMVR.Lab1Rebuild
{
    public sealed class Lab1RebuildStatus : MonoBehaviour
    {
        public static Lab1RebuildStatus Instance { get; private set; }

        public TextMesh statusText;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Set(string message)
        {
            if (Instance != null) Instance.SetMessage(message);
            Debug.Log("[Lab1 Rebuild] " + message);
        }

        public void SetMessage(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
