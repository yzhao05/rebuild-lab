using System.Collections;
using EMVR.Common.Interaction;
using EMVR.Common.UI;
using UnityEngine;

namespace EMVR.Common.Validation
{
    /// <summary>
    /// Keeps the desktop validation view monoscopic while leaving the XR rig
    /// active for simulated controller tracking. F1 selects desktop view and F2
    /// selects headset view.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrMinimalValidationViewMode : MonoBehaviour, IEmVrXrViewState
    {
        public Camera desktopCamera;
        public Camera xrCamera;
        public EmVrMousePointer desktopPointer;
        public EmVrPlanarMouseDraggable desktopDraggable;
        public AudioListener desktopAudioListener;
        public AudioListener xrAudioListener;
        public EmVrStatusDisplay statusDisplay;
        public bool startInDesktopView = true;
        public KeyCode desktopViewKey = KeyCode.F1;
        public KeyCode xrViewKey = KeyCode.F2;

        public bool IsDesktopView { get; private set; }

        public bool IsXrViewActive
        {
            get { return !IsDesktopView; }
        }

        void Awake()
        {
            SetDesktopView(startInDesktopView);
        }

        IEnumerator Start()
        {
            // The simulator creates its UI clone during startup. Rebind for a
            // few frames so it is not left as a VR-invisible Overlay canvas.
            for (int i = 0; i < 30; i++)
            {
                ConfigureDeviceSimulatorCanvas();
                yield return null;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(desktopViewKey))
            {
                SetDesktopView(true);
            }
            else if (Input.GetKeyDown(xrViewKey))
            {
                SetDesktopView(false);
            }
        }

        public void SetDesktopView(bool desktop)
        {
            if (!desktop && desktopDraggable != null)
            {
                desktopDraggable.EndMouseDrag();
            }

            if (desktopCamera != null)
            {
                desktopCamera.stereoTargetEye = StereoTargetEyeMask.None;
                desktopCamera.enabled = desktop;
            }

            if (xrCamera != null)
            {
                xrCamera.enabled = !desktop;
            }

            if (desktopPointer != null)
            {
                desktopPointer.enabled = desktop;
            }

            if (desktopDraggable != null)
            {
                desktopDraggable.handleUnityMouseMessages = desktop;
            }

            if (desktopAudioListener != null)
            {
                desktopAudioListener.enabled = desktop;
            }

            if (xrAudioListener != null)
            {
                xrAudioListener.enabled = !desktop;
            }

            IsDesktopView = desktop;
            if (statusDisplay != null)
            {
                statusDisplay.SetStatus(
                    "view_mode",
                    desktop ? "DESKTOP (F2 for XR)" : "XR (F1 for desktop)");
            }

            ConfigureDeviceSimulatorCanvas();
        }

        void ConfigureDeviceSimulatorCanvas()
        {
            Camera activeCamera = IsDesktopView ? desktopCamera : xrCamera;
            if (activeCamera == null)
            {
                return;
            }

            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null
                    || !canvas.name.StartsWith("XR Device Simulator UI"))
                {
                    continue;
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = activeCamera;
                canvas.planeDistance = Mathf.Max(0.25f, activeCamera.nearClipPlane + 0.1f);
            }
        }
    }
}
