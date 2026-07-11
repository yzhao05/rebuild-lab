using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class PolarizationWaveVisualizer : MonoBehaviour
    {
        public bool showWaveTrace = true;
        public bool showPolarizationOrbit = true;
        public bool animateWaveTrace = true;
        public float animationSpeed = 1.9f;
        public TextMesh statusLabel;
        public TextMesh resultLabel;
        public float waveAmplitudeScale = 0.78f;
        public float waveLineWidth = 0.026f;
        public float orbitLineWidth = 0.032f;
        public float vectorLineWidth = 0.018f;
        public Color inputColor = new Color(0.5f, 0.92f, 1f, 0.75f);
        public Color outputColor = new Color(1f, 0.95f, 0.3f, 0.95f);
        public Color orbitColor = new Color(1f, 0.58f, 0.18f, 0.95f);
        public Color vectorColor = new Color(1f, 1f, 1f, 0.88f);

        readonly List<LineRenderer> renderers = new List<LineRenderer>();
        Material lineMaterial;
        string lastSignature;
        float nextRefreshTime;
        JonesVector lastOutput;

        Bounds StageBounds
        {
            get
            {
                Vector3 size = new Vector3(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
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

            nextRefreshTime = Time.time + 0.08f;
            List<PolarizationElement> elements = GetActiveElements();
            string signature = BuildSignature(elements);
            if (signature != lastSignature)
            {
                lastSignature = signature;
                Rebuild(elements);
            }
        }

        public void ForceRefresh()
        {
            lastSignature = string.Empty;
            nextRefreshTime = 0f;
        }

        public void SetWaveTraceVisible(bool visible)
        {
            showWaveTrace = visible;
            ForceRefresh();
        }

        public void SetOrbitVisible(bool visible)
        {
            showPolarizationOrbit = visible;
            ForceRefresh();
        }

        public bool Contains(Vector3 point)
        {
            Bounds bounds = StageBounds;
            bounds.Expand(new Vector3(0.35f, 0.35f, 0f));
            return point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y;
        }

        public JonesVector CalculateOutput(IList<PolarizationElement> elements)
        {
            JonesVector wave = JonesVector.Linear45();
            for (int i = 0; i < elements.Count; i++)
            {
                PolarizationElement element = elements[i];
                if (element != null)
                {
                    element.ApplyTo(ref wave);
                }
            }

            return wave;
        }

        List<PolarizationElement> GetActiveElements()
        {
            Bounds bounds = StageBounds;
            bounds.Expand(new Vector3(0.6f, 1.2f, 0f));
            PolarizationElement[] allElements = FindObjectsOfType<PolarizationElement>();
            List<PolarizationElement> elements = new List<PolarizationElement>();
            for (int i = 0; i < allElements.Length; i++)
            {
                PolarizationElement element = allElements[i];
                if (element != null && IsInsideStageOnScreenPlane(element.transform.position, bounds))
                {
                    elements.Add(element);
                }
            }

            elements.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            return elements;
        }

        static bool IsInsideStageOnScreenPlane(Vector3 point, Bounds bounds)
        {
            return point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y;
        }

        string BuildSignature(IList<PolarizationElement> elements)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(showWaveTrace ? "W1" : "W0");
            builder.Append(showPolarizationOrbit ? "O1" : "O0");
            if (showWaveTrace && animateWaveTrace)
            {
                builder.Append(":T");
                builder.Append(Mathf.FloorToInt(Time.time * 12f));
            }

            for (int i = 0; i < elements.Count; i++)
            {
                PolarizationElement element = elements[i];
                Vector3 p = element.transform.position;
                builder.Append('|');
                builder.Append(element.GetInstanceID());
                builder.Append(':');
                builder.Append(element.elementType);
                builder.Append(':');
                builder.Append(Mathf.RoundToInt(element.axisAngleDegrees * 10f));
                builder.Append(':');
                builder.Append(Mathf.RoundToInt(p.x * 16f));
                builder.Append(',');
                builder.Append(Mathf.RoundToInt(p.y * 16f));
            }

            return builder.ToString();
        }

        void Rebuild(IList<PolarizationElement> elements)
        {
            ClearLines();
            JonesVector input = JonesVector.Linear45();
            JonesVector output = CalculateOutput(elements);
            lastOutput = output;

            if (showWaveTrace)
            {
                BuildWaveTrace(input, true);
                BuildWaveTrace(output, false);
                BuildOutputVectorSamples(output);
            }

            if (showPolarizationOrbit)
            {
                BuildOrbit(output);
                BuildOrbitInstantVector(output);
            }

            UpdateLabels(elements, output);
        }

        void BuildWaveTrace(JonesVector wave, bool input)
        {
            Bounds bounds = StageBounds;
            float minX = input ? bounds.min.x + 0.35f : transform.position.x + 0.2f;
            float maxX = input ? transform.position.x - 0.25f : bounds.max.x - 0.35f;
            float centerY = input ? transform.position.y + 0.85f : transform.position.y - 0.9f;
            float z = transform.position.z - 0.52f;
            List<Vector3> points = new List<Vector3>();
            const int count = 140;
            for (int i = 0; i < count; i++)
            {
                float u = i / (float)(count - 1);
                float phase = u * Mathf.PI * 2f * 3.2f;
                if (animateWaveTrace)
                {
                    phase -= Time.time * animationSpeed;
                }

                float ex = Sample(wave.x, phase);
                float ey = Sample(wave.y, phase);
                float x = Mathf.Lerp(minX, maxX, u);
                points.Add(new Vector3(x + ex * waveAmplitudeScale * 0.16f, centerY + ey * waveAmplitudeScale, z));
            }

            CreateLine(points, input ? inputColor : outputColor, waveLineWidth, input ? "Incident Linear Wave" : "Output Polarization Wave");
        }

        void BuildOutputVectorSamples(JonesVector wave)
        {
            Bounds bounds = StageBounds;
            float minX = transform.position.x + 0.45f;
            float maxX = bounds.max.x - 0.55f;
            float centerY = transform.position.y - 0.9f;
            float z = transform.position.z - 0.57f;
            const int count = 15;
            for (int i = 0; i < count; i++)
            {
                float u = i / (float)(count - 1);
                float phase = u * Mathf.PI * 2f * 3.2f;
                if (animateWaveTrace)
                {
                    phase -= Time.time * animationSpeed;
                }

                float ex = Sample(wave.x, phase);
                float ey = Sample(wave.y, phase);
                Vector3 center = new Vector3(Mathf.Lerp(minX, maxX, u), centerY, z);
                Vector3 offset = new Vector3(ex * 0.34f, ey * 0.56f, 0f);
                if (offset.sqrMagnitude < 0.001f)
                {
                    continue;
                }

                List<Vector3> points = new List<Vector3>();
                points.Add(center);
                Vector3 tip = center + offset;
                points.Add(tip);
                CreateLine(points, vectorColor, vectorLineWidth, "Output E Vector Sample");
                CreateArrowHead(center, tip, vectorColor, vectorLineWidth, "Output E Vector Arrow Head");
            }
        }

        void BuildOrbit(JonesVector wave)
        {
            Vector3 center = new Vector3(StageBounds.max.x + 1.45f, transform.position.y, transform.position.z - 0.54f);
            List<Vector3> points = new List<Vector3>();
            const int count = 121;
            for (int i = 0; i < count; i++)
            {
                float phase = i / (float)(count - 1) * Mathf.PI * 2f;
                float ex = Sample(wave.x, phase);
                float ey = Sample(wave.y, phase);
                points.Add(center + new Vector3(ex * 0.95f, ey * 0.95f, 0f));
            }

            CreateLine(points, orbitColor, orbitLineWidth, "Output Polarization Orbit");
        }

        void BuildOrbitInstantVector(JonesVector wave)
        {
            Vector3 center = new Vector3(StageBounds.max.x + 1.45f, transform.position.y, transform.position.z - 0.6f);
            float phase = animateWaveTrace ? -Time.time * animationSpeed : 0f;
            Vector3 tip = center + new Vector3(Sample(wave.x, phase) * 0.95f, Sample(wave.y, phase) * 0.95f, 0f);
            List<Vector3> vector = new List<Vector3>();
            vector.Add(center);
            vector.Add(tip);
            CreateLine(vector, vectorColor, vectorLineWidth * 1.4f, "Output Orbit Instant E Vector");
            CreateArrowHead(center, tip, vectorColor, vectorLineWidth * 1.4f, "Output Orbit Instant E Arrow Head");

            List<Vector3> tick = new List<Vector3>();
            tick.Add(tip + new Vector3(-0.06f, 0f, 0f));
            tick.Add(tip + new Vector3(0.06f, 0f, 0f));
            CreateLine(tick, vectorColor, vectorLineWidth * 1.4f, "Output Orbit Instant Marker X");
            tick = new List<Vector3>();
            tick.Add(tip + new Vector3(0f, -0.06f, 0f));
            tick.Add(tip + new Vector3(0f, 0.06f, 0f));
            CreateLine(tick, vectorColor, vectorLineWidth * 1.4f, "Output Orbit Instant Marker Y");
        }

        void CreateArrowHead(Vector3 basePoint, Vector3 tip, Color color, float width, string objectName)
        {
            Vector3 direction = tip - basePoint;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            direction.Normalize();
            Vector3 side = new Vector3(-direction.y, direction.x, 0f);
            float headLength = 0.11f;
            float headWidth = 0.07f;

            List<Vector3> left = new List<Vector3>();
            left.Add(tip);
            left.Add(tip - direction * headLength + side * headWidth);
            CreateLine(left, color, width, objectName + " L");

            List<Vector3> right = new List<Vector3>();
            right.Add(tip);
            right.Add(tip - direction * headLength - side * headWidth);
            CreateLine(right, color, width, objectName + " R");
        }

        static float Sample(Complex phasor, float phase)
        {
            return phasor.real * Mathf.Cos(phase) - phasor.imag * Mathf.Sin(phase);
        }

        void UpdateLabels(IList<PolarizationElement> elements, JonesVector output)
        {
            string classification = Classify(output);
            float intensity = output.Intensity;
            float phase = PhaseDifferenceDegrees(output);
            if (resultLabel != null)
            {
                resultLabel.text = classification + "\nI = " + intensity.ToString("0.00") + "\nphase y-x = " + phase.ToString("0") + " deg";
            }

            if (statusLabel != null)
            {
                statusLabel.text = elements.Count == 0
                    ? "Drag polarizers into the stage. Hold R while dragging to rotate continuously."
                    : "active films: " + ElementSummary(elements) + " -> " + classification;
            }
        }

        static string ElementSummary(IList<PolarizationElement> elements)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < elements.Count; i++)
            {
                PolarizationElement element = elements[i];
                if (element == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(element.elementType == PolarizationElementType.LinearPolarizer ? "LP " : "QWP ");
                builder.Append(Mathf.RoundToInt(element.axisAngleDegrees));
            }

            return builder.ToString();
        }

        public static string Classify(JonesVector wave)
        {
            float ax = Mathf.Sqrt(wave.x.MagnitudeSquared);
            float ay = Mathf.Sqrt(wave.y.MagnitudeSquared);
            float intensity = wave.Intensity;
            if (intensity < 0.015f)
            {
                return "near extinction";
            }

            if (ax < 0.025f || ay < 0.025f)
            {
                return "linear";
            }

            float phase = Mathf.DeltaAngle(wave.x.Phase * Mathf.Rad2Deg, wave.y.Phase * Mathf.Rad2Deg);
            bool circularPhase = Mathf.Abs(Mathf.Abs(phase) - 90f) < 12f;
            bool balanced = Mathf.Abs(ax - ay) / Mathf.Max(ax, ay) < 0.14f;

            if (circularPhase && balanced)
            {
                return phase > 0f ? "RHCP" : "LHCP";
            }

            if (Mathf.Abs(phase) < 10f || Mathf.Abs(Mathf.Abs(phase) - 180f) < 10f)
            {
                return "linear";
            }

            return phase > 0f ? "RHEP" : "LHEP";
        }

        static float PhaseDifferenceDegrees(JonesVector wave)
        {
            return Mathf.DeltaAngle(wave.x.Phase * Mathf.Rad2Deg, wave.y.Phase * Mathf.Rad2Deg);
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
            line.numCapVertices = 5;
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
