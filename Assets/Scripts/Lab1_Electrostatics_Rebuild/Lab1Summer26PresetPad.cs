using UnityEngine;

namespace EMVR.Lab1Rebuild
{
    [RequireComponent(typeof(Collider))]
    public sealed class Lab1Summer26PresetPad : MonoBehaviour
    {
        public int presetIndex = 1;
        public Lab1Summer26PresetController controller;

        private void OnMouseDown()
        {
            if (controller != null) controller.ApplyPreset(presetIndex);
        }
    }
}
