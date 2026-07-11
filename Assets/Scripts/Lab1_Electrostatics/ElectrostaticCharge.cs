using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticCharge : MonoBehaviour
    {
        [Header("Charge")]
        public ChargeDistribution distribution = ChargeDistribution.Point;
        [Range(-8, 8)]
        public int chargeUnits;
        public float sourceStrength = 1f;
        public string displayName = "Charge";

        [Header("Visuals")]
        public TextMesh chargeLabel;
        public Color positiveColor = new Color(1f, 0.22f, 0.12f, 1f);
        public Color negativeColor = new Color(0.12f, 0.42f, 1f, 1f);
        public Color neutralColor = new Color(0.86f, 0.88f, 0.9f, 1f);

        Renderer[] cachedRenderers;
        MaterialPropertyBlock propertyBlock;

        public float SignedStrength
        {
            get { return chargeUnits * sourceStrength; }
        }

        public Vector3 LineAxis
        {
            get { return transform.up.normalized; }
        }

        public Vector3 SurfaceNormal
        {
            get { return transform.right.normalized; }
        }

        void Awake()
        {
            CacheRenderers();
            UpdateVisuals();
        }

        void OnValidate()
        {
            CacheRenderers();
            UpdateVisuals();
        }

        public void AddCharge(int amount, int maxAbsCharge)
        {
            chargeUnits = Mathf.Clamp(chargeUnits + amount, -maxAbsCharge, maxAbsCharge);
            UpdateVisuals();
        }

        public void SetCharge(int amount)
        {
            chargeUnits = amount;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (cachedRenderers == null)
            {
                CacheRenderers();
            }

            Color color = neutralColor;
            if (chargeUnits > 0)
            {
                color = positiveColor;
            }
            else if (chargeUnits < 0)
            {
                color = negativeColor;
            }

            if (cachedRenderers != null)
            {
                if (propertyBlock == null)
                {
                    propertyBlock = new MaterialPropertyBlock();
                }

                for (int i = 0; i < cachedRenderers.Length; i++)
                {
                    if (cachedRenderers[i] != null)
                    {
                        cachedRenderers[i].GetPropertyBlock(propertyBlock);
                        propertyBlock.SetColor("_Color", color);
                        propertyBlock.SetColor("_BaseColor", color);
                        cachedRenderers[i].SetPropertyBlock(propertyBlock);
                    }
                }
            }

            if (chargeLabel != null)
            {
                string sign = chargeUnits > 0 ? "+" : string.Empty;
                chargeLabel.text = displayName + "\nq = " + sign + chargeUnits;
                chargeLabel.color = chargeUnits == 0 ? Color.white : color;
            }
        }

        void CacheRenderers()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }
}
