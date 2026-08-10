using UnityEngine;

namespace EMVR.Common.SceneFlow
{
    /// <summary>
    /// Keeps one application-level service root across single-scene loads and
    /// destroys the authored duplicate when the bootstrap scene is revisited.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class EmVrPersistentServiceRoot : MonoBehaviour
    {
        static EmVrPersistentServiceRoot instance;
        static int duplicateDestroyCount;

        public string serviceId = "emvr_application_services";

        public bool IsPrimary { get; private set; }

        public static EmVrPersistentServiceRoot Instance
        {
            get { return instance; }
        }

        public static int DuplicateDestroyCount
        {
            get { return duplicateDestroyCount; }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                IsPrimary = false;
                duplicateDestroyCount++;
                Destroy(gameObject);
                return;
            }

            instance = this;
            IsPrimary = true;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
