using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class MagneticFieldVisualizer : MonoBehaviour
    {
        [Header("Rendering")]
        public bool showFieldLines = true;
        public bool showVectorMap = true;
        public int maxFieldLineSteps = 180;
        public float integrationStep = 0.12f;
        public float fieldLineWidth = 0.018f;
        public float vectorWidth = 0.01f;
        public float vectorSpacing = 0.86f;
        public float vectorLength = 0.34f;
        public Color fieldLineColor = new Color(1f, 1f, 1f, 0.9f);
        public Color vectorColor = new Color(0.62f, 0.92f, 1f, 0.7f);

        [Header("Physics Approximation")]
        public float softening = 0.12f;
        public float wireFieldScale = 1.25f;
        public float solenoidFieldScale = 1.55f;
        public float solenoidInternalScale = 0.75f;

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
            if (Time.time < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.time + 0.2f;
            List<MagneticCurrentSource> sources = GetActiveSources();
            string signature = BuildSignature(sources);
            if (signature != lastSignature)
            {
                lastSignature = signature;
                Rebuild(sources);
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

        public Vector3 FieldAt(Vector3 point, IList<MagneticCurrentSource> sources)
        {
            Vector3 field = Vector3.zero;
            for (int i = 0; i < sources.Count; i++)
            {
                MagneticCurrentSource source = sources[i];
                if (source == null || source.currentLevel == 0)
                {
                    continue;
                }

                if (source.sourceType == MagneticSourceType.StraightWire)
                {
                    field += StraightWireField(point, source);
                }
                else
                {
                    field += SolenoidField(point, source);
                }
            }

            return Vector3.ClampMagnitude(field, 12f);
        }

        Vector3 StraightWireField(Vector3 point, MagneticCurrentSource source)
        {
            Vector3 axis = source.SourceAxis;
            Vector3 delta = point - source.transform.position;
            Vector3 radial = delta - Vector3.Dot(delta, axis) * axis;
            if (radial.sqrMagnitude < 0.0001f)
            {
                radial = source.transform.right * 0.05f;
            }

            Vector3 tangent = Vector3.Cross(axis, radial).normalized;
            float magnitude = wireFieldScale * source.SignedStrength / (radial.sqrMagnitude + softening);
            return tangent * magnitude;
        }

        Vector3 SolenoidField(Vector3 point, MagneticCurrentSource source)
        {
            Vector3 axis = source.SourceAxis;
            Vector3 delta = point - source.transform.position;
            float axial = Vector3.Dot(delta, axis);
            Vector3 radial = delta - axial * axis;
            float length = Mathf.Max(0.6f, Mathf.Abs(source.transform.lossyScale.y) * 2.1f);
            float radius = Mathf.Max(0.28f, Mathf.Max(Mathf.Abs(source.transform.lossyScale.x), Mathf.Abs(source.transform.lossyScale.z)));

            Vector3 dipoleMoment = axis * source.SignedStrength * solenoidFieldScale;
            float r2 = delta.sqrMagnitude + softening;
            float r = Mathf.Sqrt(r2);
            Vector3 dipole = (3f * delta * Vector3.Dot(dipoleMoment, delta) / Mathf.Pow(r2, 2.5f)) - (dipoleMoment / Mathf.Pow(r, 3f));

            float radialFactor = Mathf.Clamp01(1f - radial.magnitude / (radius * 1.15f));
            float axialFactor = Mathf.Clamp01(1f - Mathf.Abs(axial) / (length * 0.5f));
            Vector3 internalField = axis * source.SignedStrength * solenoidInternalScale * radialFactor * axialFactor;
            return dipole + internalField;
        }

        List<MagneticCurrentSource> GetActiveSources()
        {
            MagneticCurrentSource[] allSources = FindObjectsOfType<MagneticCurrentSource>();
            Bounds expanded = StageBounds;
            expanded.Expand(3.5f);
            List<MagneticCurrentSource> sources = new List<MagneticCurrentSource>();
            for (int i = 0; i < allSources.Length; i++)
            {
                MagneticCurrentSource source = allSources[i];
                if (source != null && source.currentLevel != 0 && expanded.Contains(source.transform.position))
                {
                    sources.Add(source);
                }
            }

            return sources;
        }

        string BuildSignature(IList<MagneticCurrentSource> sources)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(showFieldLines ? "F1" : "F0");
            builder.Append(showVectorMap ? "V1" : "V0");
            for (int i = 0; i < sources.Count; i++)
            {
                MagneticCurrentSource source = sources[i];
                Vector3 p = source.transform.position;
                Vector3 e = source.transform.eulerAngles;
                builder.Append('|');
                builder.Append(source.GetInstanceID());
                builder.Append(':');
                builder.Append(source.sourceType);
                builder.Append(':');
                builder.Append(source.currentLevel);
                builder.Append(':');
                builder.Append(Mathf.RoundToInt(p.x * 16f));
                builder.Append(',');
                builder.Append(Mathf.RoundToInt(p.y * 16f));
                builder.Append(',');
                builder.Append(Mathf.RoundToInt(p.z * 16f));
                builder.Append(':');
                builder.Append(Mathf.RoundToInt(e.x));
                builder.Append(',');
                builder.Append(Mathf.RoundToInt(e.y));
                builder.Append(',');
                builder.Append(Mathf.RoundToInt(e.z));
            }

            return builder.ToString();
        }

        void Rebuild(IList<MagneticCurrentSource> sources)
        {
            ClearLines();
            if (sources.Count == 0)
            {
                return;
            }

            if (showFieldLines)
            {
                BuildFieldLines(sources);
            }

            if (showVectorMap)
            {
                BuildVectorMap(sources);
            }
        }

        void BuildFieldLines(IList<MagneticCurrentSource> sources)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                MagneticCurrentSource source = sources[i];
                List<Vector3> seeds = GetSeeds(source);
                int lineBudget = Mathf.Clamp(Mathf.Abs(source.currentLevel) * 5, 8, 28);
                int step = Mathf.Max(1, seeds.Count / lineBudget);

                for (int s = 0; s < seeds.Count; s += step)
                {
                    List<Vector3> points = TraceLine(seeds[s], sources);
                    if (points.Count > 8)
                    {
                        CreateLine(points, fieldLineColor, fieldLineWidth, "Magnetic Field Line");
                    }
                }
            }
        }

        void BuildVectorMap(IList<MagneticCurrentSource> sources)
        {
            Bounds bounds = StageBounds;
            float minX = bounds.min.x + 0.45f;
            float maxX = bounds.max.x - 0.45f;
            float minY = bounds.min.y + 0.45f;
            float maxY = bounds.max.y - 0.45f;
            float z = transform.position.z - bounds.extents.z * 0.43f;

            for (float x = minX; x <= maxX; x += vectorSpacing)
            {
                for (float y = minY; y <= maxY; y += vectorSpacing)
                {
                    Vector3 p = new Vector3(x, y, z);
                    Vector3 field = FieldAt(p, sources);
                    if (field.sqrMagnitude < 0.001f)
                    {
                        continue;
                    }

                    Vector3 dir = field.normalized;
                    float length = vectorLength * Mathf.Clamp(field.magnitude * 0.25f, 0.25f, 1f);
                    List<Vector3> points = new List<Vector3>();
                    points.Add(p - dir * length * 0.42f);
                    points.Add(p + dir * length * 0.58f);
                    CreateLine(points, vectorColor, vectorWidth, "Magnetic Vector Sample");
                }
            }
        }

        List<Vector3> TraceLine(Vector3 seed, IList<MagneticCurrentSource> sources)
        {
            List<Vector3> backward = TraceDirection(seed, -1f, sources);
            List<Vector3> forward = TraceDirection(seed, 1f, sources);
            backward.Reverse();
            if (forward.Count > 0)
            {
                forward.RemoveAt(0);
            }

            backward.AddRange(forward);
            return backward;
        }

        List<Vector3> TraceDirection(Vector3 seed, float direction, IList<MagneticCurrentSource> sources)
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
                Vector3 field = FieldAt(current, sources);
                if (field.sqrMagnitude < 0.0004f)
                {
                    break;
                }

                current += direction * field.normalized * integrationStep;
            }

            return points;
        }

        List<Vector3> GetSeeds(MagneticCurrentSource source)
        {
            List<Vector3> seeds = new List<Vector3>();
            Vector3 center = source.transform.position;
            Vector3 axis = source.SourceAxis;
            Vector3 right = source.transform.right.normalized;
            Vector3 forward = source.transform.forward.normalized;

            if (source.sourceType == MagneticSourceType.StraightWire)
            {
                for (int a = 0; a < 16; a++)
                {
                    float angle = Mathf.PI * 2f * a / 16f;
                    Vector3 radial = Mathf.Cos(angle) * right + Mathf.Sin(angle) * forward;
                    for (int h = -3; h <= 3; h++)
                    {
                        seeds.Add(center + axis * (h * 0.5f) + radial * 0.42f);
                    }
                }
            }
            else
            {
                float radius = Mathf.Max(0.38f, Mathf.Max(Mathf.Abs(source.transform.lossyScale.x), Mathf.Abs(source.transform.lossyScale.z)));
                for (int a = 0; a < 18; a++)
                {
                    float angle = Mathf.PI * 2f * a / 18f;
                    Vector3 radial = Mathf.Cos(angle) * right + Mathf.Sin(angle) * forward;
                    seeds.Add(center + radial * radius * 0.95f + axis * 0.35f);
                    seeds.Add(center + radial * radius * 1.45f - axis * 0.35f);
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
            line.numCapVertices = 3;
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
