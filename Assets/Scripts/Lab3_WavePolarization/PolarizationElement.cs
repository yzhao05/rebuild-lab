using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class PolarizationElement : MonoBehaviour
    {
        public PolarizationElementType elementType = PolarizationElementType.LinearPolarizer;
        [Range(0f, 180f)]
        public float axisAngleDegrees;
        public string displayName = "Linear polarizer";
        public TextMesh label;
        public TextMesh angleLabel;
        public Transform axisVisual;

        MaterialPropertyBlock propertyBlock;
        Renderer[] renderers;

        public Vector2 Axis
        {
            get
            {
                float radians = axisAngleDegrees * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            }
        }

        public Vector2 SlowAxis
        {
            get
            {
                Vector2 fast = Axis;
                return new Vector2(-fast.y, fast.x);
            }
        }

        void Awake()
        {
            CacheRenderers();
            UpdateVisuals();
        }

        void OnValidate()
        {
            axisAngleDegrees = NormalizeAngle(axisAngleDegrees);
            CacheRenderers();
            UpdateVisuals();
        }

        public void SetAngle(float angleDegrees)
        {
            axisAngleDegrees = NormalizeAngle(angleDegrees);
            UpdateVisuals();
        }

        public void AddAngle(float deltaDegrees)
        {
            SetAngle(axisAngleDegrees + deltaDegrees);
        }

        public void ApplyTo(ref JonesVector wave)
        {
            float radians = axisAngleDegrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);

            Complex ex = wave.x;
            Complex ey = wave.y;

            if (elementType == PolarizationElementType.LinearPolarizer)
            {
                Complex projected = ex * c + ey * s;
                wave.x = projected * c;
                wave.y = projected * s;
                return;
            }

            Complex fastComponent = ex * c + ey * s;
            Complex slowComponent = ex * -s + ey * c;
            fastComponent *= Complex.I;

            wave.x = fastComponent * c + slowComponent * -s;
            wave.y = fastComponent * s + slowComponent * c;
        }

        public static float NormalizeAngle(float angle)
        {
            angle %= 180f;
            if (angle < 0f)
            {
                angle += 180f;
            }

            return angle;
        }

        void CacheRenderers()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        void UpdateVisuals()
        {
            if (axisVisual != null)
            {
                axisVisual.localRotation = Quaternion.Euler(0f, 0f, axisAngleDegrees);
            }

            if (label != null)
            {
                label.text = elementType == PolarizationElementType.LinearPolarizer ? "LP" : "QWP";
            }

            if (angleLabel != null)
            {
                string prefix = elementType == PolarizationElementType.LinearPolarizer ? "axis" : "fast";
                angleLabel.text = prefix + " " + Mathf.RoundToInt(axisAngleDegrees);
            }

            if (renderers == null)
            {
                return;
            }

            Color color = elementType == PolarizationElementType.LinearPolarizer
                ? new Color(0.2f, 0.75f, 1f, 0.74f)
                : new Color(1f, 0.62f, 0.22f, 0.74f);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }

    public struct JonesVector
    {
        public Complex x;
        public Complex y;

        public JonesVector(Complex x, Complex y)
        {
            this.x = x;
            this.y = y;
        }

        public float Intensity
        {
            get { return x.MagnitudeSquared + y.MagnitudeSquared; }
        }

        public static JonesVector Linear45()
        {
            float value = 1f / Mathf.Sqrt(2f);
            return new JonesVector(new Complex(value, 0f), new Complex(value, 0f));
        }
    }

    public struct Complex
    {
        public static readonly Complex I = new Complex(0f, 1f);

        public float real;
        public float imag;

        public Complex(float real, float imag)
        {
            this.real = real;
            this.imag = imag;
        }

        public float MagnitudeSquared
        {
            get { return real * real + imag * imag; }
        }

        public float Phase
        {
            get { return Mathf.Atan2(imag, real); }
        }

        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.real + b.real, a.imag + b.imag);
        }

        public static Complex operator *(Complex a, Complex b)
        {
            return new Complex(a.real * b.real - a.imag * b.imag, a.real * b.imag + a.imag * b.real);
        }

        public static Complex operator *(Complex a, float value)
        {
            return new Complex(a.real * value, a.imag * value);
        }

        public static Complex operator *(float value, Complex a)
        {
            return a * value;
        }
    }
}
