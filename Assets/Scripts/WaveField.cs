using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChargedObjects;
using System;


public class WaveField : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject prefab;
    public int wave_num=40;
    public int belta = 1;
    public int w= 1;
    public List<Polarizer> Polarizers;
    private Vector3 StartPosition;
    private Quaternion StartRotation;
    private List<GameObject> clone1_list;
    private GameObject clone1;
    private int circular_index = 1;
    private bool circular = false;
    private float[,] FieldStrength;
    private float[] magnitude;
    private float[] orientation;
    private float PolAngle;   //Angle of Linear Polarizer
    private float[] phase_count;
    private float[] magx,magy;
    private float mag,orien;
    
    void Start()
    {
        StartPosition = new Vector3(-28, 1, 0);
        StartRotation = prefab.transform.rotation;
        clone1_list =new List<GameObject>();
        FieldStrength = new float[wave_num, 2];
        magnitude = new float[wave_num];
        orientation = new float[wave_num];
        phase_count = new float[wave_num];
        magx= new float[wave_num];
        magy = new float[wave_num];
        for (int i = 1; i <= wave_num; i++)
        {
            magx[i - 1] = 1;
            magy[i - 1] = 1;
        }
            for (int i = 1; i <= wave_num; i++)
        {
            clone1 = Instantiate(prefab, StartPosition + new Vector3(0, 0, (float)i / 6), StartRotation);
            FieldStrength[i - 1, 0] = magx[i-1]*Mathf.Sin(belta * clone1.transform.position.z);   //Calculate strength of wave in x direction
            FieldStrength[i - 1, 1] = magy[i-1]*Mathf.Sin(belta * clone1.transform.position.z);   //Calculate strength of wave in y direction
            magnitude[i - 1] = Mathf.Sqrt((float)Mathf.Pow(FieldStrength[i - 1, 0], 2) + (float)Mathf.Pow(FieldStrength[i - 1, 1], 2));
            orientation[i - 1] = Mathf.Atan(FieldStrength[i - 1, 1] / FieldStrength[i - 1, 0]);
            if (FieldStrength[i - 1, 0] < 0) orientation[i - 1] -= Mathf.PI;   // Atan() range (-90,90)
            clone1.transform.localScale = new Vector3(prefab.transform.localScale.x * magnitude[i - 1], prefab.transform.localScale.y, prefab.transform.localScale.z);
            clone1.transform.eulerAngles = new Vector3(clone1.transform.eulerAngles.x, clone1.transform.eulerAngles.y, -orientation[i - 1] * 180 / Mathf.PI);
            clone1_list.Add(clone1);
        }
    }
    void OnTriggerEnter(Collider collision)
    {
        var Polarizer = collision.gameObject.GetComponent<ChargedObjects.Polarizer>();

        // 修改点：增加了 && !Polarizers.Contains(Polarizer)
        // 只有当列表中还没有这个波片时，才执行添加操作
        if (Polarizer != null && !Polarizers.Contains(Polarizer))
        {
            Polarizers.Add(Polarizer);
            if (Polarizer.linear) Debug.Log("LinearPolarizer enter");
            else Debug.Log("Quarter Plate enter");
        }
    }
    void OnTriggerExit(Collider collision)
    {
        var Polarizer = collision.gameObject.GetComponent<ChargedObjects.Polarizer>();
        if (Polarizer != null)
        {
            Polarizers.Remove(Polarizer);
            if (Polarizer.linear) Debug.Log("LinearPolarizer exit");
            else Debug.Log("Quarter Plate exit");
        }
    }
    // Update is called once per frame
    void Update()
    {
        for (int i = 1; i <= wave_num; i++)
        {
            magx[i - 1] = 1;
            magy[i - 1] = 1;
            phase_count[i - 1] = 0;
            FieldStrength[i - 1, 0] = magx[i-1]*Mathf.Sin(-w * Time.time + belta * clone1_list[i - 1].transform.position.z+phase_count[i - 1] * Mathf.PI * 0.5f);
            FieldStrength[i - 1, 1] = magy[i-1]*Mathf.Sin(-w * Time.time + belta * clone1_list[i - 1].transform.position.z);
            magnitude[i - 1] = Mathf.Sqrt((float)Mathf.Pow(FieldStrength[i - 1, 0], 2) + (float)Mathf.Pow(FieldStrength[i - 1, 1], 2));
            orientation[i - 1] = Mathf.Atan(FieldStrength[i - 1, 1] / FieldStrength[i - 1, 0]);
            if (FieldStrength[i - 1, 0] < 0) orientation[i - 1] -= Mathf.PI;
        }

        for (int i = 1; i <= wave_num; i++) phase_count[i - 1] = 0f;

        Polarizers.Sort(delegate (Polarizer x, Polarizer y)
        {
            return x.transform.position.z.CompareTo(y.transform.position.z);
        });

        foreach (ChargedObjects.Polarizer polarizer in Polarizers)
        {
            if (polarizer.linear) LinearPolarize(polarizer);
            else Circularize(polarizer);
        }

        for (int i = 1; i <= wave_num; i++)
        {
            FieldStrength[i - 1, 0] = magx[i - 1] * Mathf.Sin(-w * Time.time + belta * clone1_list[i - 1].transform.position.z + phase_count[i - 1] * Mathf.PI * 0.5f);
            FieldStrength[i - 1, 1] = magy[i - 1] * Mathf.Sin(-w * Time.time + belta * clone1_list[i - 1].transform.position.z);
            magnitude[i - 1] = Mathf.Sqrt((float)Mathf.Pow(FieldStrength[i - 1, 0], 2) + (float)Mathf.Pow(FieldStrength[i - 1, 1], 2));
            orientation[i - 1] = Mathf.Atan(FieldStrength[i - 1, 1] / FieldStrength[i - 1, 0]);
            if (FieldStrength[i - 1, 0] < 0) orientation[i - 1] -= Mathf.PI;
            clone1_list[i - 1].transform.localScale = new Vector3(prefab.transform.localScale.x * magnitude[i - 1], prefab.transform.localScale.y, prefab.transform.localScale.z);
            clone1_list[i - 1].transform.eulerAngles = new Vector3(clone1.transform.eulerAngles.x, clone1.transform.eulerAngles.y, -orientation[i - 1] * 180 / Mathf.PI);
        }
    }
    public float CheckAngle(float value)
    {
        if (value > 90f) value -= 180f;
        if (value < -90f) value += 180f;
        return value;
    }


    void LinearPolarize(ChargedObjects.Polarizer polarizer)
    {
        PolAngle = CheckAngle(polarizer.transform.eulerAngles.z);

        for (int i = 1; i <= wave_num; i++)
        {

            if (clone1_list[i - 1].transform.position.z > polarizer.transform.position.z)
            {
                mag = Mathf.Abs(magx[i-1] * Mathf.Cos(PolAngle / 180f * Mathf.PI) + magy[i-1]* Mathf.Cos((90f - PolAngle) / 180f * Mathf.PI));
                orien = PolAngle / 180f * Mathf.PI;
                magx[i-1] = mag * Mathf.Cos(orien);
                magy[i-1] = mag * Mathf.Sin(orien);
                magnitude[i - 1] = Mathf.Sqrt((float)Mathf.Pow(FieldStrength[i - 1, 0], 2) + (float)Mathf.Pow(FieldStrength[i - 1, 1], 2));
                orientation[i - 1] = Mathf.Atan(FieldStrength[i - 1, 1] / FieldStrength[i - 1, 0]);
                if (FieldStrength[i - 1, 0] < 0) orientation[i - 1] -= Mathf.PI;
                clone1_list[i - 1].transform.localScale = new Vector3(prefab.transform.localScale.x * magnitude[i - 1], prefab.transform.localScale.y, prefab.transform.localScale.z);
                clone1_list[i - 1].transform.eulerAngles = new Vector3(clone1.transform.eulerAngles.x, clone1.transform.eulerAngles.y, -orientation[i - 1] * 180 / Mathf.PI);
            }           
        }
    }

    void Circularize(ChargedObjects.Polarizer polarizer)
    {
        circular = false;
        for(int i = wave_num; i >0; i--)
        {
            if (clone1_list[i - 1].transform.position.z > polarizer.transform.position.z)
            {
                circular_index = i-1;
            }
        }
        if (magx[circular_index - 1] > 0.01f&& magy[circular_index - 1] > 0.01f)
        {
            circular = true;
        }
        if (circular)
        {
            for (int i = 1; i <= wave_num; i++)
            {
                if (clone1_list[i - 1].transform.position.z > polarizer.transform.position.z) phase_count[i - 1] += 1f;
            }
        }


    }
}
