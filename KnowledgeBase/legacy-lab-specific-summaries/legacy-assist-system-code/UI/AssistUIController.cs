using EMVR.Assist.Logging;
using EMVR.Assist.Network;
using EMVR.Assist.Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EMVR.Assist.UI
{
    public class AssistUIController : MonoBehaviour
    {
        [Header("Input (assign one)")]
        [SerializeField] private TMP_InputField tmpInputField;
        [SerializeField] private InputField uguiInputField;

        [Header("Buttons")]
        [SerializeField] private Button sendButton;

        [Header("Behavior")]
        [SerializeField] private bool submitOnEnter = true;
        [SerializeField] private bool clearInputAfterSend = true;

        private void Start()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(SendQuestion);
            }
        }

        private void Update()
        {
            if (!submitOnEnter) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendQuestion();
            }
        }

        public void SendQuestion()
        {
            string text = ReadInputText();
            if (string.IsNullOrWhiteSpace(text)) return;

            if (AssistGatewayClient.Instance == null)
            {
                Debug.LogError("[AssistUIController] AssistGatewayClient.Instance missing.");
                return;
            }

            if (AssistGatewayClient.Instance.IsRequesting)
            {
                Debug.LogWarning("[AssistUIController] request in flight, ignored.");
                return;
            }

            if (InteractionEventLogger.Instance != null)
            {
                ExperimentObject sel = SelectionTracker.Instance != null
                    ? SelectionTracker.Instance.CurrentSelected
                    : null;
                InteractionEventLogger.Instance.LogAction("ask_for_help", sel);
            }

            if (sendButton != null) sendButton.interactable = false;

            StartCoroutine(AssistGatewayClient.Instance.CallReactiveAssist(text, () =>
            {
                if (sendButton != null) sendButton.interactable = true;
                if (clearInputAfterSend) ClearInput();
            }));
        }

        private string ReadInputText()
        {
            if (tmpInputField != null) return tmpInputField.text;
            if (uguiInputField != null) return uguiInputField.text;
            return "";
        }

        private void ClearInput()
        {
            if (tmpInputField != null)
            {
                tmpInputField.text = "";
                tmpInputField.ActivateInputField();
            }
            if (uguiInputField != null)
            {
                uguiInputField.text = "";
                uguiInputField.ActivateInputField();
            }
        }
    }
}
