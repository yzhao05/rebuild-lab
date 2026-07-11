using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticFieldVisualizer : MonoBehaviour
    {
        [Header("Rendering")]
        public bool showFieldLines = true;
        public bool showVectorMap = true;
        public int maxFieldLineSteps = 120;
        public float integrationStep = 0.12f;
        public float fieldLineWidth = 0.018f;
        public float vectorWidth = 0.01f;
        public float vectorSpacing = 0.72f;
        public float vectorLength = 0.32f;
        public Color fieldLineColor = new Color(1f, 1f, 1f, 0.92f);
        public Color vectorColor = new Color(0.72f, 0.92f, 1f, 0.72f);

        [Header("Physics Approximation")]
        public float softening = 0.08f;
        public float surfaceFieldScale = 0.42f;
        public float lineFieldScale = 0.9f;
        public float pointFieldScale = 1.1f;

        readonly List<LineRenderer> renderers = new List<LineRenderer>();
        Material lineMaterial;
        string lastSignature;
        float nextRefreshTime;

        Bounds StageBounds
        {
            get
            {
                Vector3 size = new Vector3(
                    Mathf.Abs(transform.lossyScale.x),
                    Mathf.Abs(transform.lossyScale.y),
                    Mathf.Abs(transform.lossyScale.z));
                return new Bounds(transform.position, size);
            }
        }

        void Awake()
        {
            EnsureLineMaterial();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                SetFieldLinesVisible(!showFieldLines);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                SetVectorMapVisible(!showVectorMap);
            }

            if (Time.time < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.time + 0.25f;

            List<ElectrostaticCharge> charges = GetVisibleCharges();
            string signature = BuildSignature(charges);
            if (signature != lastSignature)
            {
                lastSignature = signature;
                Rebuild(charges);
            }
        }

        public void ForceRefresh()
        {
            lastSignature = string.Empty;
            nextRefreshTime = 0f;
        }

        public void SetFieldLinesVisible(bool visible)
        {
            showFieldLines = visible;
            ForceRefresh();
        }

        public void SetVectorMapVisible(bool visible)
        {
            showVectorMap = visible;
            ForceRefresh();
        }

        public bool Contains(Vector3 point)
        {
            return StageBounds.Contains(point);
        }

        public Vector3 FieldAt(Vector3 point, IList<ElectrostaticCharge> charges)
        {
            Vector3 field = Vector3.zero;

            for (int i = 0; i < charges.Count; i++)
            {
                ElectrostaticCharge charge = charges[i];
                if (charge == null || charge.chargeUnits == 0)
                {
                    continue;
                }

                Vector3 delta = point - charge.transform.position;
                float q = charge.SignedStrength;

                if (charge.distribution == ChargeDistribution.Point)
                {
                    float denom = Mathf.Pow(delta.sqrMagnitude + softening, 1.5f);
                    field += pointFieldScale * q * delta / denom;
                }
                else if (charge.distribution == ChargeDistribution.Line)
                {
                    Vector3 axis = charge.LineAxis;
                    Vector3 radial = delta - Vector3.Dot(delta, axis) * axis;
                    field += lineFieldScale * q * radial / (radial.sqrMagnitude + softening);
                }
                else
                {
                    Vector3 normal = charge.SurfaceNormal;
                    float side = Mathf.Sign(Vector3.Dot(delta, normal));
                    if (Mathf.Abs(side) < 0.1f)
                    {
                        side = 1f;
                    }

                    Vector3 local = charge.transform.InverseTransformPoint(point);
                    float edgeFalloff = Mathf.Clamp01(1.35f - 0.22f * Mathf.Abs(local.y) - 0.18f * Mathf.Abs(local.z));
                    field += surfaceFieldScale * q * side * edgeFalloff * normal;
                }
            }

            return Vector3.ClampMagnitude(field, 10f);
        }

        List<ElectrostaticCharge> GetVisibleCharges()
        {
            ElectrostaticCharge[] allCharges = FindObjectsOfType<ElectrostaticCharge>();
            Bounds bounds = StageBounds;
            List<ElectrostaticCharge> charges = new List<ElectrostaticCharge>();

            for (int i = 0; i < allCharges.Length; i++)
            {
                ElectrostaticCharge charge = allCharges[i];
                if (charge != null && charge.chargeUnits != 0 && bounds.Contains(charge.transform.position))
                {
                    charges.Add(charge);
                }
            }

            return charges;
        }

        string BuildSignature(IList<ElectrostaticCharge> charges)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(showFieldLines ? "F1" : "F0");
            builder.Append(showVectorMap ? "V1" : "V0");

            for (int i = 0; i < charges.Count; i++)
            {
                ElectrostaticCharge charge = charges[i];
                Vector3 p = charge.transform.position;
                builder.Append('|');
                builder.Append(charge.GetInstanceID());
                builder.Append(':');
                builder.Append(charge.distribution);
                builder.Append(':');
                builder.Append(charge.chargeUnits);
                builder.Append(':');
                builder.Append(Mathf.RoundToInt(p.x * 20f));
                builder.Append(',');
                builder.Append(Mathf.RoundToInt(p.y * 20f));
                builder.Append(',');
                builder.Append(Mathf.RoundToInt(p.z * 20f));
            }

            return builder.ToString();
        }

        void Rebuild(IList<ElectrostaticCharge> charges)
        {
            ClearLines();
            if (charges.Count == 0)
            {
                return;
            }

            if (showFieldLines)
            {
                BuildFieldLines(charges);
            }

            if (showVectorMap)
            {
                BuildVectorMap(charges);
            }
        }

        void BuildFieldLines(IList<ElectrostaticCharge> charges)
        {
            bool hasPositive = false;
            for (int i = 0; i < charges.Count; i++)
            {
                if (charges[i].chargeUnits > 0)
                {
                    hasPositive = true;
                    break;
                }
            }

            for (int i = 0; i < charges.Count; i++)
            {
                ElectrostaticCharge charge = charges[i];
                if (hasPositive && charge.chargeUnits <= 0)
                {
                    continue;
                }

                if (!hasPositive && charge.chargeUnits >= 0)
                {
                    continue;
                }

                int direction = charge.chargeUnits > 0 ? 1 : -1;
                List<Vector3> seeds = GetSeeds(charge);
                int lineBudget = Mathf.Clamp(Mathf.Abs(charge.chargeUnits) * 6, 6, 36);
                int step = Mathf.Max(1, seeds.Count / lineBudget);

                for (int s = 0; s < seeds.Count; s += step)
                {
                    List<Vector3> points = TraceLine(seeds[s], direction, charges);
                    if (points.Count > 5)
                    {
                        CreateLine(points, fieldLineColor, fieldLineWidth, "Field Line");
                    }
                }
            }
        }

        void BuildVectorMap(IList<ElectrostaticCharge> charges)
        {
            Bounds bounds = StageBounds;
            float minX = bounds.min.x + 0.35f;
            float maxX = bounds.max.x - 0.35f;
            float minY = bounds.min.y + 0.35f;
            float maxY = bounds.max.y - 0.35f;
            float z = transform.position.z - bounds.extents.z * 0.44f;

            for (float x = minX; x <= maxX; x += vectorSpacing)
            {
                for (float y = minY; y <= maxY; y += vectorSpacing)
                {
                    Vector3 p = new Vector3(x, y, z);
                    Vector3 field = FieldAt(p, charges);
                    if (field.sqrMagnitude < 0.001f)
                    {
                        continue;
                    }

                    Vector3 dir = field.normalized;
                    float length = vectorLength * Mathf.Clamp(field.magnitude * 0.35f, 0.25f, 1f);
                    List<Vector3> points = new List<Vector3>();
                    points.Add(p - dir * length * 0.45f);
                    points.Add(p + dir * length * 0.55f);
                    CreateLine(points, vectorColor, vectorWidth, "Vector Sample");
                }
            }
        }

        List<Vector3> TraceLine(Vector3 seed, int direction, IList<ElectrostaticCharge> charges)
        {
            Bounds bounds = StageBounds;
            List<Vector3> points = new List<Vector3>();
            Vector3 current = seed;

            for (int i = 0; i < maxFieldLineSteps; i++)
            {
                if (!bounds.Contains(current))
                {
                    break;
                }

                points.Add(current);
                Vector3 field = FieldAt(current, charges);
                if (field.sqrMagnitude < 0.0005f)
                {
                    break;
                }

                current += direction * field.normalized * integrationStep;

                for (int c = 0; c < charges.Count; c++)
                {
                    if ((current - charges[c].transform.position).sqrMagnitude < 0.08f)
                    {
                        return points;
                    }
                }
            }

            return points;
        }

        List<Vector3> GetSeeds(ElectrostaticCharge charge)
        {
            List<Vector3> seeds = new List<Vector3>();
            Vector3 center = charge.transform.position;
            int rings = Mathf.Clamp(Mathf.Abs(charge.chargeUnits), 1, 4);

            if (charge.distribution == ChargeDistribution.Point)
            {
                for (int r = 0; r < rings; r++)
                {
                    float radius = 0.3f + r * 0.09f;
                    for (int i = 0; i < 16; i++)
                    {
                        float angle = Mathf.PI * 2f * i / 16f;
                        seeds.Add(center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
                    }
                }
            }
            else if (charge.distribution == ChargeDistribution.Line)
            {
                for (int y = -5; y <= 5; y++)
                {
                    float offsetY = y * 0.22f;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        seeds.Add(center + charge.transform.up * offsetY + charge.transform.right * side * 0.34f);
                    }
                }
            }
            else
            {
                for (int y = -4; y <= 4; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3 tangent = charge.transform.up * (y * 0.24f) + charge.transform.forward * (z * 0.22f);
                        seeds.Add(center + tangent + charge.SurfaceNormal * 0.28f);
                        seeds.Add(center + tangent - charge.SurfaceNormal * 0.28f);
                    }
                }
            }

            return seeds;
        }

        void CreateLine(IList<Vector3> points, Color color, float width, string objectName)
        {
            if (points.Count < 2)
            {
                return;
            }

            EnsureLineMaterial();
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, true);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = lineMaterial;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.positionCount = points.Count;

            for (int i = 0; i < points.Count; i++)
            {
                line.SetPosition(i, points[i]);
            }

            renderers.Add(line);
        }

        void ClearLines()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    Destroy(renderers[i].gameObject);
                }
            }

            renderers.Clear();
        }

        void EnsureLineMaterial()
        {
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                lineMaterial = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit"));
            }
        }
    }
}
