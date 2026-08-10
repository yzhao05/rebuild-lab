using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChargedObjects;
using System;


public class MagneticField : MonoBehaviour
{
     public struct GPU_wire {
        public Vector3 p1;
        public Vector3 p2;
        public float electric_mag;
        public GPU_wire(Vector3 p1_,Vector3 p2_,float mag){
            p1=p1_;
            p2=p2_;
            electric_mag=mag;
        }
    }
    public struct GPU_solenoid {
        public Vector3 p1;
        public Vector3 p2,p3,p4;
        public float electric_mag;
        public GPU_solenoid(Vector3 p1_,Vector3 p2_,Vector3 p3_,Vector3 p4_,float mag){
            p1=p1_;
            p2=p2_;
            p3=p3_;
            p4=p4_;
            electric_mag=mag;
        }
    }
    public bool GPU=false;
    public GameObject Space;
    public List<ChargedObjects.Wire> ChargedWires;
    public List<ChargedObjects.Solenoid> ChargedSolenoids;
    public List<LineRenderer> Mag_field_lines;
    public int Line_density = 20;
    public int Field_Line_Length = 1200;
    public float stepsize = 0.05f;
    public float CollisionBound = 0.06f;
    public int num_subrings = 30;
    int MAX=10;
/* GPU variables*/
    public ComputeShader shader;
    int kernelHandle;

    ComputeBuffer forwardPositionBuffer; // store the position of all lines
    ComputeBuffer backwardPositionBuffer; // store the position of all lines

    ComputeBuffer forwardcountBuffer; // store the count for each line render
    ComputeBuffer backwardcountBuffer; // store the count for each line render

    ComputeBuffer WireBuffer; // store charged objects 
    ComputeBuffer SolenoidBuffer; // store charged objects 
    


    public static float right_field;
    public static float left_field;
    public GameObject Right_grab;
    public GameObject Left_grab;

    public Vector3[] forwardPositionArray;
    public Vector3[] backwardPositionArray;
    public GPU_wire[] wireArray;
    public GPU_solenoid[] solenoidArray;

    public int[] forwardCountArray;
    public int[] backwardCountArray;

    void initialshader(){
  
        forwardPositionArray=new Vector3[Field_Line_Length*Line_density];
        backwardPositionArray=new Vector3[Field_Line_Length*Line_density];

        forwardCountArray= new int[Line_density];
        backwardCountArray= new int[Line_density];

        kernelHandle = shader.FindKernel("CSMain");
        //limitation 
        // by doing these the line_density is fixed, cannot be changed in run time
        forwardPositionBuffer= new ComputeBuffer(Field_Line_Length*Line_density,12); // 12=sizeof(float)*3; the size of a vector3
        backwardPositionBuffer= new ComputeBuffer(Field_Line_Length*Line_density,12); // 12=sizeof(float)*3; the size of a vector3

        forwardcountBuffer= new ComputeBuffer(Line_density,sizeof(int)); // 4=sizeof(float) the size of a float 
        backwardcountBuffer= new ComputeBuffer(Line_density,sizeof(int)); // 4=sizeof(float) the size of a float 



        shader.SetBuffer(kernelHandle,"fResult",forwardPositionBuffer);
        shader.SetBuffer(kernelHandle,"bResult",backwardPositionBuffer);

        shader.SetBuffer(kernelHandle,"fCount",forwardcountBuffer);
        shader.SetBuffer(kernelHandle,"bCount",backwardcountBuffer);
        // shader.SetBuffer(kernelHandle,"Wires",WireBuffer);

        WireBuffer = new ComputeBuffer(MAX,sizeof(float)*7);
        shader.SetBuffer(kernelHandle,"Wires",WireBuffer);

        SolenoidBuffer = new ComputeBuffer(MAX,sizeof(float)*13);
        shader.SetBuffer(kernelHandle,"Solenoids",SolenoidBuffer);

        

        
    }
    void Start()
    {
        initialshader();
        Initialize_Mag_FieldLines();
        ChargedWires = new List<ChargedObjects.Wire>();
        ChargedSolenoids = new List<ChargedObjects.Solenoid>();
    }
    private void OnTriggerEnter(Collider other)
    {
        var wire = other.gameObject.GetComponent<ChargedObjects.Wire>();
        if (wire != null)
        {
            ChargedWires.Add(wire);
        }
        var solenoid = other.gameObject.GetComponent<ChargedObjects.Solenoid>();
        if (solenoid != null)
        {
            ChargedSolenoids.Add(solenoid);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        var wire = other.gameObject.GetComponent<ChargedObjects.Wire>();
        if (wire != null)
        {
            ChargedWires.Remove(wire);
        }
        var solenoid = other.gameObject.GetComponent<ChargedObjects.Solenoid>();
        if (solenoid != null)
        {
            ChargedSolenoids.Remove(solenoid);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (ChargedWires.Count==0 && ChargedSolenoids.Count==0)
        {
            for (int i = 0; i<Line_density; i++){

                Mag_field_lines[i].positionCount = 1;
                Mag_field_lines[i].SetPosition(0,Vector3.zero);
            }
            return;
        }
        Draw_Mag_FieldLines();
        Right_grab = GrabDetect_Right.curr_grabbed;
        if (Right_grab!=null)
        {
            Bounds bound = Space.transform.GetComponent<Renderer>().bounds;
            Vector3 Curr_position = Right_grab.transform.position;
            if ((Curr_position.x <= bound.min.x) || (Curr_position.x >= bound.max.x) || (Curr_position.y <= bound.min.y) || (Curr_position.y >= bound.max.y) || (Curr_position.z <= bound.min.z) || (Curr_position.z >= bound.max.z))
                right_field = 0;
            else right_field = GetFieldForVibration(Curr_position, Right_grab).magnitude;
            //Debug.Log(GetFieldAtPoint(Curr_position));
        }
        Left_grab = GrabDetect_Left.curr_grabbed;
        if (Left_grab!=null)
        {
            Bounds bound = Space.transform.GetComponent<Renderer>().bounds;
            Vector3 Curr_position = Left_grab.transform.position;
            if ((Curr_position.x <= bound.min.x) || (Curr_position.x >= bound.max.x) || (Curr_position.y <= bound.min.y) || (Curr_position.y >= bound.max.y) || (Curr_position.z <= bound.min.z) || (Curr_position.z >= bound.max.z))
                left_field = 0;
            else left_field = GetFieldForVibration(Curr_position, Left_grab).magnitude;
            //Debug.Log(GetFieldAtPoint(Curr_position));
        }
    }


    // create #Line_density  Magnetic Field Lines each have a corresponding line_renderer 
    
    void Initialize_Mag_FieldLines()
    {
        for (int i = 0; i<Line_density; i++){
            GameObject Line = new GameObject("Magnetic Field Line");
            LineRenderer Mag_fieldLineRenderer = Line.AddComponent<LineRenderer>();
            Mag_fieldLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            Mag_fieldLineRenderer.widthMultiplier = 0.005f;
            Mag_fieldLineRenderer.positionCount = Field_Line_Length;
            Mag_field_lines.Add(Mag_fieldLineRenderer);
        }
    }



    void Draw_Mag_FieldLines()
    {
        // for each line
        for (int i  = 0; i<Line_density; i ++){
             Calculate_Start_Position(i);
            
        }
        if(GPU){
            Get_Line_Positions_GPU();

        }else{
            for (int i  = 0; i<Line_density; i ++){
            Get_Line_Positions(i);
            
            }
        }

    }
    //FIXME still need polsih make the line more natural 
    void Calculate_Start_Position(int i){
        Bounds bound = Space.transform.GetComponent<Renderer>().bounds;
        float x;
        float y = (bound.min.y+bound.max.y)/2;
        float z;
        if (i % 2 == 0){
            x = bound.min.x + (bound.max.x - bound.min.x)/(Line_density+1)*(i+1);
            z = (bound.min.z + bound.max.z)/2;
        }
        else{
            z = bound.min.z + (bound.max.z - bound.min.z)/(Line_density+1)*(i+1);
            x = (bound.min.x + bound.max.x)/2;
        }
        Mag_field_lines[i].SetPosition(0,new Vector3(x,y,z));
    }

    void Get_Line_Positions_GPU(){
        //initial the buffer 
        
        for (int i = 0; i < Line_density; i++)
        {
            forwardPositionArray[i*Field_Line_Length]=Mag_field_lines[i].GetPosition(0);
            // the array contain the start point 
        }
        wireArray= new GPU_wire[MAX];
        solenoidArray= new GPU_solenoid[MAX];


        for (int i = 0; i < ChargedWires.Count; i++)
        {
            // print( ChargedWires.Count);
            wireArray[i]=new GPU_wire(ChargedWires[i].head.transform.position,ChargedWires[i].tail.transform.position,ChargedWires[i].electric_mag);

        }
        for (int i = 0; i < ChargedSolenoids.Count; i++)
        {
    
            solenoidArray[i]=new GPU_solenoid(ChargedSolenoids[i].endpt1.transform.position,ChargedSolenoids[i].endpt2.transform.position,
                                                ChargedSolenoids[i].endpt3.transform.position,ChargedSolenoids[i].endpt4.transform.position,ChargedSolenoids[i].electric_mag);

        }
        // if (ChargedWires.Count!=0)
        // {
        //     WireBuffer = new ComputeBuffer(ChargedWires.Count,sizeof(float)*7);
        //     shader.SetBuffer(kernelHandle,"Wires",WireBuffer);
            WireBuffer.SetData(wireArray);
        // }
        // if (ChargedSolenoids.Count!=0)
        // {
        //     SolenoidBuffer = new ComputeBuffer(ChargedSolenoids.Count,sizeof(float)*13);
        //     shader.SetBuffer(kernelHandle,"Solenoids",SolenoidBuffer);
            SolenoidBuffer.SetData(solenoidArray);
        // }
       
        Bounds bound = Space.transform.GetComponent<Renderer>().bounds;
        
        //FIXME 如果初始起点不变的话，不需要每帧都传入起始点

        forwardPositionBuffer.SetData(forwardPositionArray);
        
        // pass into the buffer 
        // pass other parameters 
        shader.SetFloat("stepsize",stepsize);
        shader.SetFloat("CollisionBound",CollisionBound);
        shader.SetInt("Line_density",Line_density);
        shader.SetInt("Field_Line_Length",Field_Line_Length);
        shader.SetInt("Wires_size",ChargedWires.Count);
        shader.SetInt("Solenoids_size",ChargedSolenoids.Count);
        shader.SetInt("num_subrings",num_subrings);

        // Vector3 p1=ChargedWires[0].head.transform.position;
        // Vector3 p2=ChargedWires[0].tail.transform.position;

        // limited to only 1 array
        // shader.SetVector("p1",new Vector4(p1.x,p1.y,p1.z,0));
        // shader.SetVector("p2",new Vector4(p2.x,p2.y,p2.z,0));
        shader.SetVector("boundmax",new Vector4(bound.max.x,bound.max.y,bound.max.z,0));
        shader.SetVector("boundmin",new Vector4(bound.min.x,bound.min.y,bound.min.z,0));

        // if (ChargedWires.Count==0 && ChargedSolenoids.Count==0)
        // {
        //     return;
        // }
        shader.Dispatch(kernelHandle, Line_density, 1, 1);
        GetPositionFromGPU();


    }
    // no input
    // no output
    // get the data from GPU after dispatch the kernel 
    void GetPositionFromGPU(){
        // first get the count 
        forwardcountBuffer.GetData(forwardCountArray);
        backwardcountBuffer.GetData(backwardCountArray);

        forwardPositionBuffer.GetData(forwardPositionArray);
        backwardPositionBuffer.GetData(backwardPositionArray);

        for (int i = 0; i < Line_density; i++)
        {
            // Mag_field_lines[i].positionCount=forwardCountArray[i];
            // Vector3[] temp=new Vector3[forwardCountArray[i]];
            // Array.Copy(forwardPositionArray,i*Field_Line_Length,temp,0,forwardCountArray[i]); // copy the postions belong to this line renderer to the temp
            // Mag_field_lines[i].SetPositions(temp);
            List<Vector3> combined_List = new List<Vector3>();
            Vector3[] forwardtemp=new Vector3[forwardCountArray[i]];
            Vector3[] backwardtemp=new Vector3[backwardCountArray[i]];
            Array.Copy(forwardPositionArray,i*Field_Line_Length,forwardtemp,0,forwardCountArray[i]); // copy the postions belong to this line renderer to the temp
            Array.Copy( backwardPositionArray,i*Field_Line_Length, backwardtemp,0,backwardCountArray[i]); // copy the postions belong to this line renderer to the temp
            // start is in the first in the forward
            combined_List.AddRange(backwardtemp);
            combined_List.Reverse();
            combined_List.AddRange(forwardtemp);
            Mag_field_lines[i].positionCount = combined_List.Count;
            Mag_field_lines[i].SetPositions(combined_List.ToArray());


        }
    }

    void Get_Line_Positions(int i){
        List<Vector3> Line_Positions = FourthRKmethod(Mag_field_lines[i].GetPosition(0),stepsize);
        //Debug.Log(Line_Positions.Count);
        Mag_field_lines[i].positionCount = Line_Positions.Count;
        Mag_field_lines[i].SetPositions(Line_Positions.ToArray());
    }

    List<Vector3> FourthRKmethod(Vector3 start, float stepsize){
        Bounds bound = Space.transform.GetComponent<Renderer>().bounds;
        bool Forward = true;
        bool Backward = true;
        Vector3 Curr_Forward = start;
        Vector3 Curr_Backward = start;
        Vector3 Next_Forward = start;
        Vector3 Next_Backward = start;
        List<Vector3> Forward_List = new List<Vector3>();
        List<Vector3> Backward_List = new List<Vector3>();
        for (int i = 0; i <Field_Line_Length; i++){
            if (Forward){
                Curr_Forward = Next_Forward;
                Vector3 Integration_Result = Integration(Next_Forward, stepsize, 1);
                if ((Curr_Forward.x<=bound.min.x)||(Curr_Forward.x>=bound.max.x)||(Curr_Forward.y<=bound.min.y)||(Curr_Forward.y>=bound.max.y)||(Curr_Forward.z<=bound.min.z)||(Curr_Forward.z>=bound.max.z))
                    Forward = false;
                Next_Forward = Next_Forward + (stepsize*Integration_Result);
                Forward_List.Add(Next_Forward);
            }
            if (Backward){
                Curr_Backward = Next_Backward;
                Vector3 Integration_Result = Integration(Next_Backward, stepsize, -1);
                if ((Curr_Backward.x<=bound.min.x)||(Curr_Backward.x>=bound.max.x)||(Curr_Backward.y<=bound.min.y)||(Curr_Backward.y>=bound.max.y)||(Curr_Backward.z<=bound.min.z)||(Curr_Backward.z>=bound.max.z))
                    Backward = false;
                Next_Backward = Next_Backward - (stepsize*Integration_Result);
                Backward_List.Add(Next_Backward);
            }
            //if (i < 30) Debug.Log(Curr_Forward); if (i < 30) Debug.Log(Curr_Backward);
            if ((i>30)&&(Vector3.Distance(Curr_Forward,Curr_Backward)<=CollisionBound))
                break;
            if ((!Forward)&&(!Backward)) break;
        }
        Backward_List.Reverse();
        Backward_List.Add(start);
        Backward_List.AddRange(Forward_List);
        return Backward_List;
    }

    Vector3 Integration(Vector3 point, float stepsize, int sign){
        Vector3 k_1 = Vector3.zero;
        Vector3 k_2 = Vector3.zero;
        Vector3 k_3 = Vector3.zero;
        Vector3 k_4 = Vector3.zero;
        // FIXME 不normalize 可能会更好
        
        // k_1 += GetFieldAtPoint(point);
        // k_2 += GetFieldAtPoint(point + sign * k_1 * stepsize / 2);
        // k_3 += GetFieldAtPoint(point + sign * k_2 * stepsize / 2);
        // k_4 += GetFieldAtPoint(point + sign * k_3 * stepsize);
        k_1 += GetFieldAtPoint(point).normalized;
        k_2 += GetFieldAtPoint(point + sign * k_1 * stepsize / 2).normalized;
        k_3 += GetFieldAtPoint(point + sign * k_2 * stepsize / 2).normalized;
        k_4 += GetFieldAtPoint(point + sign * k_3 * stepsize).normalized;
        return (k_1 + 2 * k_2 + 2 * k_3 + k_4) / 6;
    }

    Vector3 GetFieldAtPoint(Vector3 point){
        Vector3 Field = new Vector3(0,0,0);
        foreach (ChargedObjects.Wire wire in ChargedWires)
        {
            Vector3 p1 = wire.head.transform.position;
            Vector3 p2 = wire.tail.transform.position;
            Vector3 vec = GetPerpVec(p1, p2, point);
            Vector3 direction = Vector3.Cross((p1 - p2).normalized, vec).normalized;
            Field += (p2 - p1).magnitude / vec.magnitude * wire.electric_mag * direction;
        }

        foreach (ChargedObjects.Solenoid solenoid in ChargedSolenoids)
        {

            Vector3 p1 = solenoid.endpt1.transform.position;
            Vector3 p2 = solenoid.endpt2.transform.position;
            Vector3 p3 = solenoid.endpt3.transform.position;
            Vector3 p4 = solenoid.endpt4.transform.position;
            float RE = (p3 - p2).magnitude;
            Vector3 up = (p1 - p2) / (p1 - p2).magnitude;
            Vector3 right = (p3 - p2) / (p3 - p2).magnitude;
            Vector3 front = (p4 - p2) / (p4 - p2).magnitude;

            for (int i = 0; i <= num_subrings; i++)
            {
                Vector3 Center = p2 + (p1 - p2) / num_subrings * i;
                Vector3 r = point - Center;
                float R = r.magnitude;

                //float costheta = Vector3.Dot(r, p1 - p2) / r.magnitude / (p1 - p2).magnitude;
                //float sintheta = math.sqrt(1 - costheta * costheta);
                if (r.magnitude >= RE)
                {
                    float B0 = 1; //To make the plot seem better, I changed the value of B0 to 1

                    //r = solenoid.transform.InverseTransformDirection(r);

                    //float y = r.y;
                    //float x = r.x;
                    //float z = r.z;
                    //Vector3 temp = new Vector3(3 * B0 * x * y / R / R / R / R / R, B0 * (3 * y * y - R * R) / R / R / R / R / R, 3 * B0 * z * y / R / R / R / R / R);
                    //Field -= temp;
                    float y = Vector3.Dot(r, up);
                    float x = Vector3.Dot(r, right);
                    float z = Vector3.Dot(r, front);
                    Field -= right * 3 * B0 * x * y / R / R / R / R / R + up * B0 * (3 * y * y - R * R) / R / R / R / R / R + front * 3 * B0 * z * y / R / R / R / R / R;
                }
                else
                {
                    Vector3 u = (p1 - p2).normalized;
                    Field += (3 * Vector3.Dot(u, r) * r - u) / (2 * R * R * R);
                }
            }
        }
        //foreach (ChargedObjects.Solenoid solenoid in ChargedSolenoids)
        //{

        //    Vector3 p1 = solenoid.endpt1.transform.position;
        //    Vector3 p2 = solenoid.endpt2.transform.position;
        //    Vector3 p3 = solenoid.endpt3.transform.position;
        //    Vector3 p4 = solenoid.endpt4.transform.position;
        //    Vector3 p5 = solenoid.endpt5.transform.position;
        //    Vector3 p6 = solenoid.endpt6.transform.position;

        //    for (int i = 0; i <= num_subrings; i++)
        //    {
        //        Vector3 center = (p1 + p4) / 2;
        //        Vector3 relativeposition = point - center;
        //        Vector3 transferredtarget = new Vector3(Vector3.Dot(relativeposition, (p1 - center).normalized), Vector3.Dot(relativeposition, (p2 - center).normalized), Vector3.Dot(relativeposition, (p3 - center).normalized));
        //        Vector3 projection = new Vector3((float)math.sqrt(transferredtarget[0] * transferredtarget[0] + transferredtarget[1] * transferredtarget[1]), 0, transferredtarget[2]);
        //        float r = 0.4f;
        //        float j = -(float)math.PI;
        //        float stepsize = (float)math.PI / 20;
        //        double bx = 0;
        //        double bz = 0;
        //        while (j < (float)math.PI)
        //        {
        //            bx += r * solenoid.electric_mag / (2) * projection[2] * math.cos(j) / (math.pow(projection[0] * projection[0] + r * r + projection[2] * projection[2] - 2 * projection[0] * r * math.cos(j), 1.5)) * stepsize;
        //            bz += r * solenoid.electric_mag / (2) * (r - projection[0] * math.cos(j)) / (math.pow(projection[0] * projection[0] + r * r + projection[2] * projection[2] - 2 * projection[0] * r * math.cos(j), 1.5)) * stepsize;
        //            j += stepsize;
        //        }
        //        Vector3 result = new Vector3((float)bx * transferredtarget[0] / ((float)math.sqrt(transferredtarget[0] * transferredtarget[0] + transferredtarget[1] * transferredtarget[1])), (float)bx * transferredtarget[1] / ((float)math.sqrt(transferredtarget[0] * transferredtarget[0] + transferredtarget[1] * transferredtarget[1])), (float)bz);
        //        Field += result[0] / num_subrings * (p1 - center).normalized + result[1] * (p2 - center).normalized + result[2] * (p3 - center).normalized;
        //        p1 += (p5 - p6) / num_subrings;
        //        p2 += (p5 - p6) / num_subrings;
        //        p3 += (p5 - p6) / num_subrings;
        //        p4 += (p5 - p6) / num_subrings;
        //    }
        //}
        return Field;
    }
    Vector3 GetPerpVec(Vector3 p1, Vector3 p2, Vector3 fPt)
    {
        Vector3 d = (p1 - p2).normalized;
        Vector3 v = fPt - p2;
        float t = Vector3.Dot(v, d);
        Vector3 P = p2 + t * d;
        return fPt - P;
    }
     private void OnDestroy() {
        backwardcountBuffer.Dispose();
        forwardcountBuffer.Dispose();
        forwardPositionBuffer.Dispose();
        backwardPositionBuffer.Dispose();
        if(WireBuffer!=null){
            WireBuffer.Dispose();
        }

        
    }
    Vector3 GetFieldForVibration(Vector3 point, GameObject curr)
    {
        Vector3 Field = new Vector3(0, 0, 0);
        var thiswire = curr.GetComponent<ChargedObjects.Wire>();
        foreach (ChargedObjects.Wire wire in ChargedWires)
        {
            if (wire != thiswire)
            {
                Vector3 p1 = wire.head.transform.position;
                Vector3 p2 = wire.tail.transform.position;
                Vector3 vec = GetPerpVec(p1, p2, point);
                Vector3 direction = Vector3.Cross((p1 - p2).normalized, vec).normalized;
                Field += (p2 - p1).magnitude / vec.magnitude * wire.electric_mag * direction;
            }

        }
        var thissolenoid = curr.GetComponent<ChargedObjects.Solenoid>();
        foreach (ChargedObjects.Solenoid solenoid in ChargedSolenoids)
        {
            if (solenoid != thissolenoid)
            {
                Vector3 p1 = solenoid.endpt1.transform.position;
                Vector3 p2 = solenoid.endpt2.transform.position;
                Vector3 p3 = solenoid.endpt3.transform.position;
                Vector3 p4 = solenoid.endpt4.transform.position;
                float RE = (p3 - p2).magnitude;
                Vector3 up = (p1 - p2) / (p1 - p2).magnitude;
                Vector3 right = (p3 - p2) / (p3 - p2).magnitude;
                Vector3 front = (p4 - p2) / (p4 - p2).magnitude;

                for (int i = 0; i <= num_subrings; i++)
                {
                    Vector3 Center = p2 + (p1 - p2) / num_subrings * i;
                    Vector3 r = point - Center;
                    float R = r.magnitude;

                    //float costheta = Vector3.Dot(r, p1 - p2) / r.magnitude / (p1 - p2).magnitude;
                    //float sintheta = math.sqrt(1 - costheta * costheta);
                    if (r.magnitude >= RE)
                    {
                        float B0 = 1; //To make the plot seem better, I changed the value of B0 to 1
                        float y = Vector3.Dot(r, up);
                        float x = Vector3.Dot(r, right);
                        float z = Vector3.Dot(r, front);
                        Field -= (right * 3 * B0 * x * y / R / R / R / R / R + up * B0 * (3 * y * y - R * R) / R / R / R / R / R + front * 3 * B0 * z * y / R / R / R / R / R)/ num_subrings;
                    }
                    else
                    {
                        Vector3 u = (p1 - p2).normalized;
                        Field += (3 * Vector3.Dot(u, r) * r - u) / (2 * R * R * R)/ num_subrings;
                    }
                }
            }
            
        }
        return Field;
    }
}