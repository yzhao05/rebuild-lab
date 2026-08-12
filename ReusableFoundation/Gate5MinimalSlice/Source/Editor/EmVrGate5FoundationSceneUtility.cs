using System;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace EMVR.ReusableFoundation.Gate5.Editor
{
    /// <summary>Neutral scene wiring used by every Gate 5 minimal slice.</summary>
    public static class EmVrGate5FoundationSceneUtility
    {
        public static EventSystem EnsureSingleXrEventSystem(Transform parent)
        {
            EventSystem[] systems = UnityEngine.Object.FindObjectsOfType<EventSystem>(true);
            if (systems.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Gate 5 requires one EventSystem, but the scene contains {systems.Length}.");
            }

            EventSystem system;
            if (systems.Length == 0)
            {
                GameObject instance = new GameObject("EventSystem");
                instance.transform.SetParent(parent, false);
                system = instance.AddComponent<EventSystem>();
            }
            else
            {
                system = systems[0];
                system.gameObject.SetActive(true);
                system.enabled = true;
            }

            if (system.GetComponent<XRUIInputModule>() == null)
            {
                system.gameObject.AddComponent<XRUIInputModule>();
            }

            return system;
        }

        public static Button CreatePersistentButton(
            RectTransform parent,
            Font font,
            string objectName,
            string label,
            Vector2 anchoredPosition,
            UnityAction callback)
        {
            if (parent == null || font == null || callback == null)
            {
                throw new ArgumentNullException("Gate 5 button parent, font, and callback are required.");
            }

            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(210f, 54f);
            buttonRect.anchoredPosition = anchoredPosition;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.42f, 0.58f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, callback);

            GameObject textObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(buttonRect, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 24;

            RequirePersistentButton(button);
            return button;
        }

        public static GameObject CreateActiveOffstageTemplate(Transform parent)
        {
            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = "Gate5 Neutral Spawn Template";
            template.transform.SetParent(parent, false);
            template.transform.position = new Vector3(0f, -1000f, 0f);
            template.transform.localScale = Vector3.one * 0.35f;
            template.SetActive(true);
            return template;
        }

        public static void RequirePersistentButton(Button button)
        {
            if (button == null || !button.interactable || button.onClick.GetPersistentEventCount() < 1)
            {
                throw new InvalidOperationException(
                    "Gate 5 requires an active interactable Button with a persistent onClick listener.");
            }
        }
    }

    /// <summary>Captures build errors so a scene builder can fail before save.</summary>
    public sealed class EmVrGate5BuildGuard : IDisposable
    {
        int capturedErrorCount;
        string firstError;
        bool disposed;

        public EmVrGate5BuildGuard()
        {
            Application.logMessageReceived += OnLog;
        }

        public void ThrowIfCapturedErrors()
        {
            if (capturedErrorCount > 0)
            {
                throw new InvalidOperationException(
                    $"Gate 5 builder captured {capturedErrorCount} error/exception/assert log(s). " +
                    $"First: {firstError}");
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Application.logMessageReceived -= OnLog;
        }

        void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            capturedErrorCount++;
            if (string.IsNullOrEmpty(firstError))
            {
                firstError = condition;
            }
        }
    }
}
