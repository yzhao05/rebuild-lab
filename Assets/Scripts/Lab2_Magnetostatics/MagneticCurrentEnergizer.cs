using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class MagneticCurrentEnergizer : MonoBehaviour
    {
        [Range(-6, 6)] public int assignedLevel = 1;
        public float effectiveRadius = 0.85f;
        public TextMesh label;

        void Awake()
        {
            RefreshLabel();
        }

        void OnValidate()
        {
            assignedLevel = Mathf.Clamp(assignedLevel, -6, 6);
            RefreshLabel();
        }

        void OnTriggerEnter(Collider other)
        {
            TryApply(other);
        }

        void OnTriggerStay(Collider other)
        {
            TryApply(other);
        }

        void OnMouseDown()
        {
            ApplyToSelectedOrCenteredSource();
        }

        public void ApplyToSelectedOrCenteredSource()
        {
            MagneticCurrentSource source = MagneticDesktopDraggable.FindBestCurrentTarget();
            TryApply(source);
        }

        public bool TryApply(MagneticCurrentSource source)
        {
            if (source == null)
            {
                return false;
            }

            source.SetCurrent(assignedLevel);
            MagneticDesktopDraggable.FindBestCurrentTarget();
            return true;
        }

        void TryApply(Collider other)
        {
            if (other == null)
            {
                return;
            }

            MagneticCurrentSource source = other.GetComponentInParent<MagneticCurrentSource>();
            TryApply(source);
        }

        void RefreshLabel()
        {
            if (label == null)
            {
                return;
            }

            string sign = assignedLevel > 0 ? "+" : string.Empty;
            label.text = "I " + sign + assignedLevel;
            label.color = MagneticCurrentSource.CurrentColor(assignedLevel);
        }
    }
}
