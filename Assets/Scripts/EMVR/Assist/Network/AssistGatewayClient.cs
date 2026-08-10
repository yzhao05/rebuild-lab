using System;
using System.Collections;
using System.Text;
using EMVR.Assist.Core;
using EMVR.Assist.Data;
using EMVR.Assist.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace EMVR.Assist.Network
{
    public class AssistGatewayClient : MonoBehaviour
    {
        public static AssistGatewayClient Instance { get; private set; }

        [SerializeField] private AssistConfig config;

        public bool IsRequesting { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public IEnumerator CallReactiveAssist(string studentInputText, Action onComplete = null)
        {
            if (IsRequesting)
            {
                Debug.LogWarning("[AssistGatewayClient] request already in flight, ignored.");
                onComplete?.Invoke();
                yield break;
            }

            if (AssistPayloadBuilder.Instance == null)
            {
                Debug.LogError("[AssistGatewayClient] AssistPayloadBuilder.Instance missing.");
                onComplete?.Invoke();
                yield break;
            }

            AssistPayload payload = AssistPayloadBuilder.Instance.BuildReactivePayload(studentInputText);
            yield return SendPayload(payload, onComplete);
        }

        public IEnumerator CallProactiveAssist(TriggerReason reason, Action onComplete = null)
        {
            if (IsRequesting)
            {
                Debug.LogWarning("[AssistGatewayClient] request already in flight, ignored.");
                onComplete?.Invoke();
                yield break;
            }

            if (AssistPayloadBuilder.Instance == null)
            {
                Debug.LogError("[AssistGatewayClient] AssistPayloadBuilder.Instance missing.");
                onComplete?.Invoke();
                yield break;
            }

            AssistPayload payload = AssistPayloadBuilder.Instance.BuildProactivePayload(reason);
            yield return SendPayload(payload, onComplete);
        }

        private IEnumerator SendPayload(AssistPayload payload, Action onComplete)
        {
            IsRequesting = true;

            if (config == null || string.IsNullOrWhiteSpace(config.gatewayUrl))
            {
                Debug.LogError("[AssistGatewayClient] AssistConfig.gatewayUrl is not set.");
                if (AssistResponseExecutor.Instance != null)
                {
                    AssistResponseExecutor.Instance.ShowLocalError("AI 助教未配置网关地址。");
                }
                IsRequesting = false;
                onComplete?.Invoke();
                yield break;
            }

            string url = config.gatewayUrl.Trim();
            if (!url.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("[AssistGatewayClient] gatewayUrl must start with http:// or https:// — got: [" + url + "]");
                if (AssistResponseExecutor.Instance != null)
                {
                    AssistResponseExecutor.Instance.ShowLocalError("网关地址格式非法。");
                }
                IsRequesting = false;
                onComplete?.Invoke();
                yield break;
            }

            string bodyJson;
            if (config.useDifyDirectMode)
            {
                var wrapper = new DifyDirectRequest
                {
                    inputs = payload,
                    response_mode = "blocking",
                    user = string.IsNullOrEmpty(payload.user_id) ? "unknown_user" : payload.user_id
                };
                bodyJson = JsonConvert.SerializeObject(wrapper);
            }
            else
            {
                bodyJson = JsonConvert.SerializeObject(payload);
            }

            if (config.logRawRequest) Debug.Log("[AssistGatewayClient] POST " + url + "\n>> " + bodyJson);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (config.useDifyDirectMode && !string.IsNullOrEmpty(config.difyApiKey))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + config.difyApiKey);
                }

                if (config.timeoutSec > 0) request.timeout = config.timeoutSec;

                yield return request.SendWebRequest();

                string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (config.logRawResponse) Debug.Log("[AssistGatewayClient] << " + responseText);

                    if (AssistResponseExecutor.Instance != null)
                    {
                        AssistResponseExecutor.Instance.Execute(responseText);
                    }
                    else
                    {
                        Debug.LogWarning("[AssistGatewayClient] AssistResponseExecutor.Instance missing.");
                    }
                }
                else
                {
                    Debug.LogError($"[AssistGatewayClient] failed url={url} code={request.responseCode}, result={request.result}, error={request.error}, body={responseText}");
                    if (AssistResponseExecutor.Instance != null)
                    {
                        AssistResponseExecutor.Instance.ShowLocalError("AI 助教暂时无法连接，请稍后再试。");
                    }
                }
            }

            IsRequesting = false;
            onComplete?.Invoke();
        }

        public IEnumerator PostOutcome(OutcomeRecord record, Action onComplete = null)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.outcomeUrl))
            {
                Debug.Log("[AssistGatewayClient] outcome (local-only): " + JsonConvert.SerializeObject(record));
                onComplete?.Invoke();
                yield break;
            }

            string body = JsonConvert.SerializeObject(record);
            using (UnityWebRequest request = new UnityWebRequest(config.outcomeUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                if (config.timeoutSec > 0) request.timeout = config.timeoutSec;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[AssistGatewayClient] outcome POST failed: {request.error}");
                }
            }
            onComplete?.Invoke();
        }
    }
}
