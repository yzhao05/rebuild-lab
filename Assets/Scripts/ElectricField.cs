using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChargedObjects;
using EMVR.Assist.Logging;
using EMVR.Assist.Objects;
public class ElectricField : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Space;
    // public GameObject temp_wire;
    public List<ChargedObjects.Wire> ChargedWires;
    public List<ChargedObjects.Plane> ChargedPlanes;
    public List<ChargedObjects.Sphere> ChargedSpheres;
    public List<LineRenderer> Ele_field_lines;
    public List<GameObject> Lines;
    public int[] Sign = new int[500];
    public int Wire_density = 20;
    public int Plane_density = 20;
    private int Sphere_density = 14;
    public int Field_Line_Length = 1200;
    public float stepsize = 0.003f;
    //used to count the number in the for loop
    private int wire_count;
    private int plane_count;
    private int sphere_count;
    //a count of the indexs in line render belong to wires and planes 
    private int total_wire_line;
    private int total_plane_line;
    //judge if there is a wire or plane, used to initialize the number of line render
    // private int wire_exist = 0;
    // private int plane_exist = 0;
    private void SetSphereDensity(ChargedObjects.Sphere sphere)
    {
        float absElectricMag = Mathf.Abs(sphere.electric_mag);
        switch (absElectricMag)
        {
            case 5:
                Sphere_density = 50;
                break;
            case 4:
                Sphere_density = 40;
                break;
            case 3:
                Sphere_density = 30;
                break;
            case 2:
                Sphere_density = 20;
                break;
            case 1:
                Sphere_density = 10;
                break;
            default:
                Sphere_density = 0; // 默认值
                break;
        }
    }
    private void SetWireCount(ChargedObjects.Wire wire)
    {
        float absElectricMag = Mathf.Abs(wire.electric_mag);
        switch (absElectricMag)
        {
            case 5:
                Wire_density = 50;
                break;
            case 4:
                Wire_density = 40;
                break;
            case 3:
                Wire_density = 30;
                break;
            case 2:
                Wire_density = 20;
                break;
            case 1:
                Wire_density = 10;
                break;
            default:
                Wire_density = 0; // 默认值
                break;
        }
    }

    private void SetPlaneCount(ChargedObjects.Plane plane)
    {
        float absElectricMag = Mathf.Abs(plane.electric_mag);
        switch (absElectricMag)
        {
            case 5:
                Plane_density = 50;
                break;
            case 4:
                Plane_density = 40;
                break;
            case 3:
                Plane_density = 30;
                break;
            case 2:
                Plane_density = 20;
                break;
            case 1:
                Plane_density = 10;
                break;
            default:
                Plane_density = 0; // 默认值
                break;
        }
    }
    void Start()
    {
        EnsureChargeBoxesExist();
    }

    void EnsureChargeBoxesExist()
    {
        ChargingBox[] chargeBoxes = FindObjectsOfType<ChargingBox>(true);
        if (chargeBoxes.Length > 0)
        {
            foreach (ChargingBox chargeBox in chargeBoxes)
            {
                if (chargeBox != null)
                {
                    chargeBox.gameObject.SetActive(true);
                }
            }
            return;
        }

        Bounds bounds = new Bounds(Vector3.zero, new Vector3(8f, 4f, 4f));
        if (Space != null)
        {
            Renderer spaceRenderer = Space.GetComponent<Renderer>();
            if (spaceRenderer != null)
            {
                bounds = spaceRenderer.bounds;
            }
        }

        Vector3 rightSide = new Vector3(bounds.max.x + 1.25f, bounds.center.y, bounds.center.z);
        CreateRuntimeChargeBox("Positive Chargebox", rightSide + Vector3.up * 0.65f, 1);
        CreateRuntimeChargeBox("Negative Chargebox", rightSide + Vector3.down * 0.65f, -1);
    }

    void CreateRuntimeChargeBox(string objectName, Vector3 position, int sign)
    {
        GameObject chargeBoxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chargeBoxObject.name = objectName;
        chargeBoxObject.transform.position = position;
        chargeBoxObject.transform.localScale = new Vector3(1.15f, 0.55f, 1.15f);

        ChargingBox chargingBox = chargeBoxObject.AddComponent<ChargingBox>();
        chargingBox.sign = sign;

        Collider chargeCollider = chargeBoxObject.GetComponent<Collider>();
        if (chargeCollider != null)
        {
            chargeCollider.isTrigger = true;
        }

        Rigidbody body = chargeBoxObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
    }

    //every time move objects into the cube, initialize the line render and add them to the lists. 
    void OnTriggerEnter(Collider collision)
    {
        var wire = collision.gameObject.GetComponent<ChargedObjects.Wire>();
        if (wire != null)
        {
            ChargedWires.Add(wire);
            Debug.Log("wire trigger enter");
            LogObservationEnter(collision.gameObject);
            Initialize_Ele_FieldLines();
        }
        var plane = collision.gameObject.GetComponent<ChargedObjects.Plane>();
        if (plane != null)
        {
            ChargedPlanes.Add(plane);
            Debug.Log("plane trigger enter");
            LogObservationEnter(collision.gameObject);
            Initialize_Ele_FieldLines();
        }
        var sphere = collision.gameObject.GetComponent<ChargedObjects.Sphere>();
        if (sphere != null)
        {
            ChargedSpheres.Add(sphere);
            Debug.Log("sphere trigger enter");
            LogObservationEnter(collision.gameObject);
            Initialize_Ele_FieldLines();
        }
    }
    //every time move objects out of the cube, destroy the line render first, initialize it again. And delete the objects from the lists. 
    void OnTriggerExit(Collider collision)
    {
        var wire = collision.gameObject.GetComponent<Wire>();
        if (wire != null)
        {
            ChargedWires.Remove(wire);
            Debug.Log("wire trigger exit");

            Initialize_Ele_FieldLines();
        }
        var plane = collision.gameObject.GetComponent<ChargedObjects.Plane>();
        if (plane != null)
        {
            ChargedPlanes.Remove(plane);
            Debug.Log("plane trigger exit");

            Initialize_Ele_FieldLines();
        }
        var sphere = collision.gameObject.GetComponent<ChargedObjects.Sphere>();
        if (sphere != null)
        {
            ChargedSpheres.Remove(sphere);
            Debug.Log("sphere trigger exit");

            Initialize_Ele_FieldLines();
        }
    }

    // Update is called once per frame
    void Update()
    {

        Draw_Ele_FieldLines();
        // foreach (ChargedObjects.Plane plane in ChargedPlanes)
        // {
        //     plane.transform.rotation=Quaternion.Euler(new Vector3(0,0,90)); ;
        // }
    }

    private void LogObservationEnter(GameObject target)
    {
        if (target == null || InteractionEventLogger.Instance == null) return;

        ExperimentObject experimentObject = target.GetComponentInParent<ExperimentObject>();
        if (experimentObject == null) return;

        InteractionEventLogger.Instance.LogAction("enter_observation_zone", experimentObject);
    }

    //Initialize the line render
    void Initialize_Ele_FieldLines()
    {   //judge if there is any wire or plane
        DestroyFieldLines();

        //calculate the number of line render needed and initialize them
        //Wire_density and Plane_density will be divided by the number of objects, and for one sphere, we will generate Sphere_density lines.
        for (int i = 0; i < Wire_density * ChargedWires.Count + Plane_density * ChargedPlanes.Count + Sphere_density * ChargedSpheres.Count; i++)
        {
            // print("add lines");
            GameObject Line = new GameObject("Electric Field Line");
            Lines.Add(Line);
            LineRenderer Ele_fieldLineRenderer = Line.AddComponent<LineRenderer>();
            Ele_fieldLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            Ele_fieldLineRenderer.widthMultiplier = 0.01f;
            Ele_fieldLineRenderer.positionCount = Field_Line_Length;
            Ele_field_lines.Add(Ele_fieldLineRenderer);
        }
    }

    //destroy the line renders.
    public void DestroyFieldLines()
    {
        Ele_field_lines.ForEach(x => Destroy(x));
        Ele_field_lines.Clear();
        Lines.ForEach(x => Destroy(x));
        Lines.Clear();


    }

    //use the same number before to draw lines.
    void Draw_Ele_FieldLines()
    {
        for (int i = 0; i < Wire_density * ChargedWires.Count + Plane_density * ChargedPlanes.Count + Sphere_density * ChargedSpheres.Count; i++)
        {
            Calculate_Start_Position();
            Get_Line_Positions(i);
        }
    }

    void Calculate_Start_Position()
    {
        Bounds bound = Space.transform.GetComponent<Renderer>().bounds;
        //calculate start points for wires, show as a circle around the center.
        if (ChargedWires.Count > 0)
        {
            // Debug.Log(Wire_density);
            // Debug.Log(num);
            float angle = 360f / Wire_density;
            float radius = 0.01f;
            Vector3 v;
            Quaternion r;
            Quaternion q;
            wire_count = -1;
            foreach (ChargedObjects.Wire wire in ChargedWires)
            {
                // Debug.Log(ChargedWires.Count);
                wire_count += 1;
                // Debug.Log("wire count"+wire_count);
                v = wire.transform.position + wire.transform.forward * radius;
                r = wire.transform.rotation;
                for (int i = 0; i < Wire_density; i++)  //circulate for each point
                {
                    q = Quaternion.Euler(r.eulerAngles.x, r.eulerAngles.y - (angle * i), r.eulerAngles.z); ///get rotation angles
                    v = wire.transform.position + (q * Vector3.forward) * radius;///get coordinate of that poitns
                    // Debug.Log("i+num*wire count" +(i+ wire_count * num));
                    // print(Ele_field_lines.Count);
                    Ele_field_lines[i + wire_count * Wire_density].SetPosition(0, v);
                    if (wire.electric_mag >= 0)
                    {
                        Sign[i + wire_count * Wire_density] = 1;
                    }
                    else
                    {
                        Sign[i + wire_count * Wire_density] = -1;
                    }
                }
            }
            // print(ChargedWires.Count);

        }
        total_wire_line = Wire_density * ChargedWires.Count;
        //calculate start points for planes, show as a grid evenly split the points in the plane
        int iter = (int)Mathf.Floor(Mathf.Sqrt(Plane_density / 2));
        if (ChargedPlanes.Count > 0)
        {
            // Debug.Log(num);  

            // Debug.Log(iter);
            plane_count = -1;
            Vector3 v1;
            Vector3 v2;
            foreach (ChargedObjects.Plane plane in ChargedPlanes)
            {
                Vector3 p1 = plane.GetComponent<ChargedObjects.Plane>().up.transform.position;
                Vector3 p2 = plane.GetComponent<ChargedObjects.Plane>().down.transform.position;
                Vector3 p3 = plane.GetComponent<ChargedObjects.Plane>().left.transform.position;
                Vector3 p4 = plane.GetComponent<ChargedObjects.Plane>().right.transform.position;
                // Debug.Log(ChargedWires.Count);
                plane_count += 1;
                // Debug.Log("wire count"+wire_count);
                //range is the step size in the plane to choose the next point. 
                Vector3 range = Vector3.Cross(p2 - p1, p4 - p3).normalized / 50;
                // Debug.Log(range);
                for (int i = 0; i < iter; i++)
                {
                    for (int j = 0; j < iter; j++)
                    {
                        // *Mathf.Pow(-1,i+j)
                        v1 = p1 + (p4 - p1) / (iter + 1) * (i + 1) + (p3 - p1) / (iter + 1) * (j + 1) + range;
                        Ele_field_lines[(i * iter + j) * 2 + plane_count * iter * iter * 2 + total_wire_line].SetPosition(0, v1);
                        v2 = p1 + (p4 - p1) / (iter + 1) * (i + 1) + (p3 - p1) / (iter + 1) * (j + 1) - range;
                        Ele_field_lines[(i * iter + j) * 2 + 1 + plane_count * iter * iter * 2 + total_wire_line].SetPosition(0, v2);
                        if (plane.electric_mag >= 0)
                        {
                            Sign[(i * iter + j) * 2 + plane_count * iter * iter * 2 + total_wire_line] = 1;
                            Sign[(i * iter + j) * 2 + 1 + plane_count * iter * iter * 2 + total_wire_line] = 1;
                        }
                        else
                        {
                            Sign[(i * iter + j) * 2 + plane_count * iter * iter * 2 + total_wire_line] = -1;
                            Sign[(i * iter + j) * 2 + 1 + plane_count * iter * iter * 2 + total_wire_line] = -1;
                        }
                    }
                }
            }

        }
        total_plane_line = ChargedPlanes.Count * iter * iter * 2;
        //calculate start points for spheres. Prefer inspector marker points when they exist,
        //but fall back to generated surface points so scene setup does not depend on manual refs.
        if (ChargedSpheres.Count > 0)
        {
            sphere_count = -1;
            foreach (ChargedObjects.Sphere sphere in ChargedSpheres)
            {
                sphere_count += 1;

                Vector3[] startPositions = GetSphereStartPositions(sphere, Sphere_density);
                int sphereLineStart = sphere_count * Sphere_density + total_plane_line + total_wire_line;
                for (int i = 0; i < startPositions.Length; i++)
                {
                    int lineIndex = sphereLineStart + i;
                    if (lineIndex >= Ele_field_lines.Count) break;
                    Ele_field_lines[lineIndex].SetPosition(0, startPositions[i]);
                }

                if (sphere.electric_mag >= 0)
                {
                    for (int i = 0; i < Sphere_density; i++)
                    {
                        Sign[i + sphereLineStart] = 1;
                    }
                }
                else
                {
                    for (int i = 0; i < Sphere_density; i++)
                    {
                        Sign[i + sphereLineStart] = -1;
                    }
                }
            }
        }
    }

    Vector3[] GetSphereStartPositions(ChargedObjects.Sphere sphere, int count)
    {
        count = Mathf.Max(1, count);
        Vector3[] positions = new Vector3[count];

        if (TryGetInspectorSphereStartPositions(sphere, positions))
        {
            return positions;
        }

        Vector3 center = sphere.transform.position;
        float radius = EstimateSphereRadius(sphere);
        const float goldenAngle = Mathf.PI * (3f - 2.2360679f);

        for (int i = 0; i < count; i++)
        {
            float y = 1f - (2f * (i + 0.5f) / count);
            float ringRadius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float theta = goldenAngle * i;
            Vector3 direction = new Vector3(Mathf.Cos(theta) * ringRadius, y, Mathf.Sin(theta) * ringRadius);
            positions[i] = center + sphere.transform.rotation * direction * radius;
        }

        return positions;
    }

    bool TryGetInspectorSphereStartPositions(ChargedObjects.Sphere sphere, Vector3[] positions)
    {
        if (positions.Length < 14) return false;
        if (sphere.one == null || sphere.two == null || sphere.three == null ||
            sphere.four == null || sphere.five == null || sphere.six == null)
        {
            return false;
        }

        Vector3 v1 = sphere.one.transform.position;
        Vector3 v2 = sphere.two.transform.position;
        Vector3 v3 = sphere.three.transform.position;
        Vector3 v4 = sphere.four.transform.position;
        Vector3 v5 = sphere.five.transform.position;
        Vector3 v6 = sphere.six.transform.position;

        Vector3[] markerPositions =
        {
            v1,
            v2,
            v3,
            v4,
            v5,
            v6,
            (v1 + v3 + v5) / 3f,
            (v1 + v3 + v6) / 3f,
            (v1 + v4 + v5) / 3f,
            (v1 + v4 + v6) / 3f,
            (v2 + v3 + v5) / 3f,
            (v2 + v3 + v6) / 3f,
            (v2 + v4 + v5) / 3f,
            (v2 + v4 + v6) / 3f
        };

        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = markerPositions[i % markerPositions.Length];
        }

        return true;
    }

    float EstimateSphereRadius(ChargedObjects.Sphere sphere)
    {
        Collider sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null)
        {
            return Mathf.Max(0.05f, sphereCollider.bounds.extents.magnitude * 0.58f);
        }

        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        if (sphereRenderer != null)
        {
            return Mathf.Max(0.05f, sphereRenderer.bounds.extents.magnitude * 0.58f);
        }

        Vector3 scale = sphere.transform.lossyScale;
        return Mathf.Max(0.05f, Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) * 0.5f);
    }

    void Get_Line_Positions(int i)
    {
        List<Vector3> Line_Positions = FourthRKmethod(Ele_field_lines[i].GetPosition(0), stepsize, i);
        //Debug.Log(Line_Positions.Count);
        Ele_field_lines[i].positionCount = Line_Positions.Count;
        Ele_field_lines[i].SetPositions(Line_Positions.ToArray());
    }

    List<Vector3> FourthRKmethod(Vector3 start, float stepsize, int index)
    {
        Bounds bound = Space.transform.GetComponent<Renderer>().bounds;
        bool Forward = true;
        Vector3 Curr_Forward = start;
        Vector3 Next_Forward = start;
        List<Vector3> Forward_List = new List<Vector3>();

        ///new add part, not sure
        if (GetFieldAtPoint(start) == new Vector3(0, 0, 0))
        {
            Forward_List.Add(Next_Forward);
            return Forward_List;
        }

        //////


        for (int i = 0; i < Field_Line_Length; i++)
        {
            if (Forward)
            {
                Vector3 Integration_Result;
                Curr_Forward = Next_Forward;
                if (Sign[index] == -1)
                {
                    Integration_Result = Integration(Next_Forward, stepsize, -1);
                }
                else
                {
                    Integration_Result = Integration(Next_Forward, stepsize, 1);
                }
                // Integration_Result = Integration(Next_Forward, stepsize, 1);
                if (GetFieldAtPoint(Next_Forward) == new Vector3(0, 0, 0))
                {
                    break;
                }
                //when electric field magnitude is too big/close to object, stop
                // if(stepsize * Integration_Result.magnitude<0.05){
                //     break;
                // }


                if ((Curr_Forward.x <= bound.min.x) || (Curr_Forward.x >= bound.max.x) || (Curr_Forward.y <= bound.min.y) || (Curr_Forward.y >= bound.max.y) || (Curr_Forward.z <= bound.min.z) || (Curr_Forward.z >= bound.max.z))
                    Forward = false;
                if (Sign[index] == 1)
                {
                    Next_Forward = Next_Forward + (stepsize * Integration_Result);
                }
                else
                {
                    Next_Forward = Next_Forward + (stepsize * Integration_Result) * (-1);
                }
                // Next_Forward = Next_Forward + (stepsize * Integration_Result);
                // print(Integration_Result);
                Forward_List.Add(Next_Forward);
                //if movement is too small, stop
                if (stepsize * Integration_Result.magnitude < 0.001)
                {
                    break;
                }
                if (!Forward) break;
            }
            // Forward_List.Reverse();
            // Forward_List.Add(start);
        }

        return Forward_List;
    }
    Vector3 Integration(Vector3 point, float stepsize, int sign)
    {
        Vector3 k_1 = Vector3.zero;
        Vector3 k_2 = Vector3.zero;
        Vector3 k_3 = Vector3.zero;
        Vector3 k_4 = Vector3.zero;
        k_1 += GetFieldAtPoint(point).normalized;
        // print(new Vector3(0,0,0).normalized);
        k_2 += GetFieldAtPoint(point + sign * k_1 * stepsize / 2).normalized;
        k_3 += GetFieldAtPoint(point + sign * k_2 * stepsize / 2).normalized;
        k_4 += GetFieldAtPoint(point + sign * k_3 * stepsize).normalized;
        return (k_1 + 2 * k_2 + 2 * k_3 + k_4) / 6;
    }

    // all three equations are converted by some constant, so the ratio should be correct.
    Vector3 GetFieldAtPoint(Vector3 point)
    {
        Vector3 bfVal = new Vector3(0, 0, 0);
        foreach (ChargedObjects.Wire wire in ChargedWires)
        {
            Vector3 p1 = wire.GetComponent<ChargedObjects.Wire>().head.transform.position;
            Vector3 p2 = wire.GetComponent<ChargedObjects.Wire>().tail.transform.position;
            Vector3 r_vec = GetPerpVec(p1, p2, point);
            float r = r_vec.magnitude;
            // if (r==0){
            //     continue;
            // }
            bfVal += r_vec.normalized * wire.electric_mag / r;
        }
        foreach (ChargedObjects.Plane plane in ChargedPlanes)
        {
            Vector3 p1 = plane.GetComponent<ChargedObjects.Plane>().up.transform.position;
            Vector3 p2 = plane.GetComponent<ChargedObjects.Plane>().down.transform.position;
            Vector3 p3 = plane.GetComponent<ChargedObjects.Plane>().left.transform.position;
            Vector3 p4 = plane.GetComponent<ChargedObjects.Plane>().right.transform.position;
            // Debug.Log(p3);
            // Vector3 r_vec=GetPerpVec(p3,p4,GetPerpVec(p1, p2, point)+p4); //get a line perpendicular to the plane and point out to the point
            Vector3 normal = (Vector3.Cross((p3 - p1), (p4 - p1))).normalized;
            Vector3 p1t2point = point - p1;
            float sign = Vector3.Dot(p1t2point, normal);
            if (-0.01 < sign && sign < 0.01)
            {
                return new Vector3(0, 0, 0);
            }
            if (sign > 0)
            {
                bfVal += 1 * normal * plane.electric_mag;
            }
            if (sign < 0)
            {
                bfVal += -1 * normal * plane.electric_mag;
            }
            // bfVal += r_vec.normalized * plane.electric_mag;  
        }
        foreach (ChargedObjects.Sphere sphere in ChargedSpheres)
        {
            Vector3 p1 = sphere.transform.position;
            // Debug.Log(p1);
            Vector3 r_vec = point - p1; //get a line perpendicular to the plane and point out to the point
            float r = r_vec.magnitude;
            bfVal += r_vec.normalized * sphere.electric_mag / r / r / 2;
        }
        // Debug.Log(bfVal);
        return bfVal;
    }

    Vector3 GetPerpVec(Vector3 p1, Vector3 p2, Vector3 fPt)
    {
        Vector3 d = (p1 - p2).normalized;
        Vector3 v = fPt - p2;
        float t = Vector3.Dot(v, d);
        Vector3 P = p2 + t * d;
        return fPt - P;
    }
}
