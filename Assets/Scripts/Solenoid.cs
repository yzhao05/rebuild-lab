using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChargedObjects{
public class Solenoid : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject endpt1;
    public GameObject endpt2;
    public GameObject endpt3;
    public GameObject endpt4;
    public float electric_mag;
    void Start()
    {
        electric_mag = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

}
