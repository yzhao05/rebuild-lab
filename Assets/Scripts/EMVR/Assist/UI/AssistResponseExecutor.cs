using System.Collections;
using EMVR.Assist.Data;
using EMVR.Assist.Objects;
using EMVR.Assist.Outcome;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EMVR.Assist.UI
{
    public class AssistResponseExecutor : MonoBehaviour
    {
        public static AssistResponseExecutor Instance { get; private set; }

        [Header("Reply Surfaces (assign at least one)")]
        [SerializeField] private TMP_Text tmpReplyText;
        [SerializeField] private Text uguiReplyText;

        [Header("Highlight")]
        [SerializeField] private bool useEmissionHighlight = true;
        [SerializeField] private bool flashHighlight = true;
        [SerializeField] private Color defaultHighlightColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField] private float highlightEmissionIntensity = 2f;
        [SerializeField] private float flashIntervalSec = 0.25f;

        private Coroutine _activeSubtitleRoutine;
        private Coroutine _activeHighlightRoutine;
        private System.Collections.Generic.List<HighlightMaterialState> _activeHighlightStates;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDisable()
        {
            StopActiveHighlight();
        }

        public void Execute(string responseJson)
        {
            UnityAssistResponse r = null;

            try
            {
                r = ExtractUnityOutput(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[AssistResponseExecutor] parse error: " + ex.Message + "\nraw=" + responseJson);
                ShowLocalError("AI 返回格式异常，请检查 workflow End 节点输出。");
                return;
            }

            if (r == null)
            {
                Debug.LogError("[AssistResponseExecutor] missing unity_output. raw=" + responseJson);
                ShowLocalError("AI 返回缺少 unity_output。请检查 Dify End 节点。");
                return;
            }

            if (r.action_type == "no_action")
            {
                Debug.Log("[AssistResponseExecutor] no_action received.");
                return;
            }

            if (r.action_type == "teaching_intervention" || r.action_type == "fallback_response")
            {
                string text = ExtractDisplayText(r);
                ShowSubtitle(text, r.display != null ? r.display.display_duration_sec : 0f);
                HighlightObjects(r.scene);
                StartOutcomeIfNeeded(r);
                return;
            }

            Debug.LogWarning("[AssistResponseExecutor] unknown action_type=" + r.action_type);
        }

        // 兼容多种返回结构：
        // 1) { "unity_output": { ... } }
        // 2) { "data": { "outputs": { "unity_output": { ... } } } }              ← Dify Workflow Run 原始响应
        // 3) { "data": { "outputs": { "unity_output": "{\"action_type\":...}" } } } ← unity_output 是字符串
        // 4) 顶层就是 UnityAssistResponse（action_type/display/...）
        // 5) FastAPI 网关再包一层：{ "status_code":200, "dify_response": { "data": { "outputs": { "unity_output": {...} } } } }
        private static UnityAssistResponse ExtractUnityOutput(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson)) return null;

            JToken root = JToken.Parse(responseJson);

            // 兜底：用 SelectToken 递归搜常见路径，先命中先返回
            string[] candidatePaths = new[]
            {
                "dify_response.data.outputs.unity_output",
                "data.outputs.unity_output",
                "outputs.unity_output",
                "unity_output"
            };
            foreach (var p in candidatePaths)
            {
                JToken uo = root.SelectToken(p);
                if (uo == null) continue;
                if (uo.Type == JTokenType.String)
                {
                    return JsonConvert.DeserializeObject<UnityAssistResponse>(uo.ToString());
                }
                if (uo.Type == JTokenType.Object)
                {
                    return uo.ToObject<UnityAssistResponse>();
                }
            }

            // 形式 4：顶层就是 UnityAssistResponse
            if (root["action_type"] != null)
            {
                return root.ToObject<UnityAssistResponse>();
            }

            return null;
        }

        private static string ExtractDisplayText(UnityAssistResponse r)
        {
            if (r == null || r.display == null) return "";
            if (!string.IsNullOrWhiteSpace(r.display.subtitle_text)) return r.display.subtitle_text;
            if (!string.IsNullOrWhiteSpace(r.display.voice_text)) return r.display.voice_text;
            return "";
        }

        public void ShowSubtitle(string text, float durationSec)
        {
            if (tmpReplyText != null) tmpReplyText.text = text ?? "";
            if (uguiReplyText != null) uguiReplyText.text = text ?? "";
            Debug.Log("[AssistResponseExecutor] subtitle: " + text);

            if (_activeSubtitleRoutine != null) StopCoroutine(_activeSubtitleRoutine);

            if (durationSec > 0f && !string.IsNullOrWhiteSpace(text))
            {
                _activeSubtitleRoutine = StartCoroutine(ClearSubtitleAfter(durationSec));
            }
        }

        private IEnumerator ClearSubtitleAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (tmpReplyText != null) tmpReplyText.text = "";
            if (uguiReplyText != null) uguiReplyText.text = "";
        }

        public void ShowLocalError(string text)
        {
            if (tmpReplyText != null) tmpReplyText.text = text ?? "";
            if (uguiReplyText != null) uguiReplyText.text = text ?? "";
            Debug.LogWarning("[AssistResponseExecutor] error: " + text);
        }

        private void HighlightObjects(ScenePayload scene)
        {
            if (scene == null || scene.highlight_objects == null || scene.highlight_objects.Length == 0) return;

            StopActiveHighlight();

            Color color = ParseColor(scene.highlight_color, defaultHighlightColor);
            float duration = scene.highlight_duration_sec > 0 ? scene.highlight_duration_sec : 5f;

            _activeHighlightRoutine = StartCoroutine(HighlightCoroutine(scene.highlight_objects, color, duration));
        }

        private IEnumerator HighlightCoroutine(string[] targets, Color color, float duration)
        {
            var states = new System.Collections.Generic.List<HighlightMaterialState>();

            foreach (var target in targets)
            {
                var objects = ExperimentObjectRegistry.FindHighlightTargets(target);
                if (objects.Count == 0)
                {
                    Debug.LogWarning("[AssistResponseExecutor] highlight target not found: " + target);
                    continue;
                }

                for (int i = 0; i < objects.Count; i++)
                {
                    AddRendererStates(objects[i], states);
                }
            }

            if (states.Count == 0)
            {
                _activeHighlightRoutine = null;
                yield break;
            }

            _activeHighlightStates = states;

            if (!flashHighlight)
            {
                ApplyHighlight(states, color, true);
                yield return new WaitForSeconds(duration);
                RestoreHighlightStates();
                _activeHighlightRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            bool visible = false;
            float interval = Mathf.Max(0.05f, flashIntervalSec);

            while (elapsed < duration)
            {
                visible = !visible;
                ApplyHighlight(states, color, visible);

                float wait = Mathf.Min(interval, duration - elapsed);
                yield return new WaitForSeconds(wait);
                elapsed += wait;
            }

            RestoreHighlightStates();
            _activeHighlightRoutine = null;
        }

        private void AddRendererStates(ExperimentObject obj, System.Collections.Generic.List<HighlightMaterialState> states)
        {
            if (obj == null) return;

            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null) continue;

                    if (useEmissionHighlight && material.HasProperty("_EmissionColor"))
                    {
                        states.Add(new HighlightMaterialState
                        {
                            material = material,
                            useEmission = true,
                            originalColor = material.GetColor("_EmissionColor"),
                            emissionWasEnabled = material.IsKeywordEnabled("_EMISSION")
                        });
                    }
                    else if (material.HasProperty("_Color"))
                    {
                        states.Add(new HighlightMaterialState
                        {
                            material = material,
                            useEmission = false,
                            originalColor = material.color
                        });
                    }
                }
            }
        }

        private void ApplyHighlight(System.Collections.Generic.List<HighlightMaterialState> states, Color color, bool visible)
        {
            Color emissionColor = color * Mathf.Max(1f, highlightEmissionIntensity);
            emissionColor.a = color.a;

            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state == null || state.material == null) continue;

                if (state.useEmission)
                {
                    if (visible)
                    {
                        state.material.EnableKeyword("_EMISSION");
                        state.material.SetColor("_EmissionColor", emissionColor);
                    }
                    else
                    {
                        state.material.SetColor("_EmissionColor", state.originalColor);
                        if (!state.emissionWasEnabled) state.material.DisableKeyword("_EMISSION");
                    }
                }
                else
                {
                    state.material.color = visible ? color : state.originalColor;
                }
            }
        }

        private void StopActiveHighlight()
        {
            if (_activeHighlightRoutine != null)
            {
                StopCoroutine(_activeHighlightRoutine);
                _activeHighlightRoutine = null;
            }

            RestoreHighlightStates();
        }

        private void RestoreHighlightStates()
        {
            if (_activeHighlightStates == null) return;

            for (int i = 0; i < _activeHighlightStates.Count; i++)
            {
                var state = _activeHighlightStates[i];
                if (state == null || state.material == null) continue;

                if (state.useEmission)
                {
                    state.material.SetColor("_EmissionColor", state.originalColor);
                    if (!state.emissionWasEnabled) state.material.DisableKeyword("_EMISSION");
                }
                else
                {
                    state.material.color = state.originalColor;
                }
            }

            _activeHighlightStates = null;
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }

        private static void StartOutcomeIfNeeded(UnityAssistResponse r)
        {
            if (OutcomeTracker.Instance == null) return;
            if (r.callback == null) return;
            float timeout = r.callback.timeout_sec > 0 ? r.callback.timeout_sec : 120f;
            string logId = string.IsNullOrEmpty(r.callback.log_id) ? r.trace_id : r.callback.log_id;
            string traceId = string.IsNullOrEmpty(r.callback.trace_id) ? r.trace_id : r.callback.trace_id;
            string expected = r.meta != null ? r.meta.expected_next_action : "";
            OutcomeTracker.Instance.StartTracking(logId, traceId, expected, timeout);
        }

        private class HighlightMaterialState
        {
            public Material material;
            public bool useEmission;
            public Color originalColor;
            public bool emissionWasEnabled;
        }
    }
}
