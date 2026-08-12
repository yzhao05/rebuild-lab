using UnityEngine;

namespace EMVR.Common.SceneFlow
{
    /// <summary>
    /// Requests a scene load after this object moves beyond a configured
    /// distance from its armed position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrMovementSceneTrigger : MonoBehaviour
    {
        public EmVrSceneLoader sceneLoader;

        [Tooltip("Leave empty to use the loader's configured scene.")]
        public string sceneName;

        [Min(0f)]
        public float movementThreshold = 0.05f;

        [Tooltip("Use local position when the trigger moves under a moving parent.")]
        public bool useLocalPosition;

        public bool armOnEnable = true;

        Vector3 armedPosition;
        bool armed;
        bool triggered;

        void Awake()
        {
            if (sceneLoader == null)
            {
                sceneLoader = GetComponent<EmVrSceneLoader>();
            }
        }

        void OnEnable()
        {
            if (armOnEnable)
            {
                Arm();
            }
        }

        void Update()
        {
            if (!armed || triggered)
            {
                return;
            }

            float threshold = Mathf.Max(0f, movementThreshold);
            if ((CurrentPosition() - armedPosition).sqrMagnitude <= threshold * threshold)
            {
                return;
            }

            if (sceneLoader == null)
            {
                Debug.LogError("EmVrMovementSceneTrigger requires an EmVrSceneLoader.", this);
                enabled = false;
                return;
            }

            bool accepted = string.IsNullOrWhiteSpace(sceneName)
                ? sceneLoader.TryLoadConfiguredScene()
                : sceneLoader.TryLoadScene(sceneName);

            if (accepted)
            {
                triggered = true;
            }
            else
            {
                enabled = false;
            }
        }

        public void Arm()
        {
            armedPosition = CurrentPosition();
            armed = true;
            triggered = false;
        }

        public void Disarm()
        {
            armed = false;
        }

        Vector3 CurrentPosition()
        {
            return useLocalPosition ? transform.localPosition : transform.position;
        }
    }
}
