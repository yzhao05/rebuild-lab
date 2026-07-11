using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class MagneticCurrentSource : MonoBehaviour
    {
        public MagneticSourceType sourceType = MagneticSourceType.StraightWire;
        [Range(-6, 6)] public int currentLevel;
        public float sourceStrength = 1f;
        public string displayName = "Current source";
        public TextMesh currentLabel;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        MaterialPropertyBlock propertyBlock;

        public float SignedStrength
        {
            get { return currentLevel * sourceStrength; }
        }

        public Vector3 SourceAxis
        {
            get { return transform.up.normalized; }
        }

        public void SetCurrent(int level)
        {
            currentLevel = Mathf.Clamp(level, -6, 6);
            UpdateVisuals();
        }

        public void AddCurrent(int amount)
        {
            SetCurrent(currentLevel + amount);
        }

        public void FlipCurrent()
        {
            SetCurrent(-currentLevel);
        }

        void Awake()
        {
            UpdateVisuals();
        }

        void OnValidate()
        {
            currentLevel = Mathf.Clamp(currentLevel, -6, 6);
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            Color color = CurrentColor(currentLevel);
            EnsurePropertyBlock();
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer sourceRenderer = renderers[i];
                if (sourceRenderer == null || sourceRenderer is LineRenderer || sourceRenderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                sourceRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorId, color);
                sourceRenderer.SetPropertyBlock(propertyBlock);
            }

            LineRenderer[] lines = GetComponentsInChildren<LineRenderer>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != null)
                {
                    lines[i].startColor = color;
                    lines[i].endColor = color;
                }
            }

            if (currentLabel != null)
            {
                string sign = currentLevel > 0 ? "+" : string.Empty;
                currentLabel.text = displayName + "\nI = " + sign + currentLevel + "\n" + DirectionText();
                currentLabel.color = currentLevel == 0 ? Color.white : color;
            }
        }

        void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        string DirectionText()
        {
            if (currentLevel > 0)
            {
                return "red +I";
            }

            if (currentLevel < 0)
            {
                return "blue -I";
            }

            return "not energized";
        }

        public static Color CurrentColor(int level)
        {
            if (level == 0)
            {
                return new Color(0.82f, 0.84f, 0.86f, 1f);
            }

            float t = Mathf.Clamp01(Mathf.Abs(level) / 6f);
            if (level > 0)
            {
                return Color.Lerp(new Color(0.95f, 0.42f, 0.16f, 1f), new Color(1f, 0.02f, 0.01f, 1f), t);
            }

            return Color.Lerp(new Color(0.1f, 0.62f, 1f, 1f), new Color(0.02f, 0.02f, 0.78f, 1f), t);
        }
    }
}
