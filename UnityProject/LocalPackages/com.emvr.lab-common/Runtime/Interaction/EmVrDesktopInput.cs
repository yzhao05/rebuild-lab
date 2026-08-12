using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Shared desktop debug input compatible with both Unity input backends.
    /// </summary>
    internal static class EmVrDesktopInput
    {
        public static Vector3 PointerScreenPosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    Vector2 position = Mouse.current.position.ReadValue();
                    return new Vector3(position.x, position.y, 0f);
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector3.zero;
#endif
            }
        }

        public static bool PrimaryPressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    return true;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(0);
#else
                return false;
#endif
            }
        }

        public static bool PrimaryHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    return Mouse.current.leftButton.isPressed;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(0);
#else
                return false;
#endif
            }
        }

        public static bool PrimaryReleasedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    return true;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonUp(0);
#else
                return false;
#endif
            }
        }
    }
}
