using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EMVR.Assist.Core
{
    public class LabSessionManager : MonoBehaviour
    {
        public static LabSessionManager Instance { get; private set; }

        [SerializeField] private AssistConfig config;
        [SerializeField] private string overrideUserId = "";
        [SerializeField] private string overrideLabId = "";
        [SerializeField] private string overrideSceneId = "";

        public string SessionId { get; private set; }
        public string UserId { get; private set; }
        public string LabId { get; private set; }
        public string SceneId { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SessionId = Guid.NewGuid().ToString();

            UserId = !string.IsNullOrWhiteSpace(overrideUserId)
                ? overrideUserId
                : (config != null && !string.IsNullOrWhiteSpace(config.defaultUserId)
                    ? config.defaultUserId
                    : "local_user_001");

            LabId = !string.IsNullOrWhiteSpace(overrideLabId)
                ? overrideLabId
                : (config != null && !string.IsNullOrWhiteSpace(config.defaultLabId)
                    ? config.defaultLabId
                    : "electrostatics");

            // 优先使用 override > config.defaultSceneId > 当前活动场景名
            string activeSceneName = SceneManager.GetActiveScene().name;
            SceneId = !string.IsNullOrWhiteSpace(overrideSceneId)
                ? overrideSceneId
                : (config != null && !string.IsNullOrWhiteSpace(config.defaultSceneId)
                    ? config.defaultSceneId
                    : (string.IsNullOrEmpty(activeSceneName) ? "default_scene" : activeSceneName));

            Debug.Log($"[LabSessionManager] session_id={SessionId} user_id={UserId} lab_id={LabId} scene_id={SceneId}");
        }

        public void RegenerateSession()
        {
            SessionId = Guid.NewGuid().ToString();
            Debug.Log("[LabSessionManager] new session_id=" + SessionId);
        }
    }
}
