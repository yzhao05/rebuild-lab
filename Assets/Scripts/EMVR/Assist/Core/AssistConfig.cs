using UnityEngine;

namespace EMVR.Assist.Core
{
    [CreateAssetMenu(fileName = "AssistConfig", menuName = "EMVR/Assist Config")]
    public class AssistConfig : ScriptableObject
    {
        [Header("Gateway")]
        [Tooltip("EMVR Gateway 或 Dify 直连地址。Gateway 模式 POST /v1/assist；直连模式 POST /v1/workflows/run。")]
        public string gatewayUrl = "http://10.109.92.119:63800/api/assistant";

        [Tooltip("如果勾选，请求体会包装为 Dify Workflow API 格式 {inputs, response_mode, user}，并附加 Authorization。")]
        public bool useDifyDirectMode = false;

        [Tooltip("仅在 useDifyDirectMode = true 时使用。不要把真实 Key 提交到 Git。")]
        public string difyApiKey = "";

        [Header("Default Context")]
        public string defaultUserId = "local_user_001";
        public string defaultLabId = "electrostatics";
        public string defaultSceneId = "Static_Electric_Field";
        public string defaultStepId = "E1";

        [Header("Outcome Logging")]
        [Tooltip("Outcome 回传地址，留空则只在 Console 输出。Gateway 模式建议指向 /v1/log/outcome。")]
        public string outcomeUrl = "";

        [Header("Networking")]
        [Tooltip("HTTP 请求超时秒数。")]
        public int timeoutSec = 30;

        [Header("Debug")]
        public bool logRawRequest = true;
        public bool logRawResponse = true;
    }
}
