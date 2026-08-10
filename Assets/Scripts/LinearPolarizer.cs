using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ChargedObjects
{
public class LinearPolarizer : MonoBehaviour
{
    // Start is called before the first frame update

    private float Angle;

    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        Angle = transform.eulerAngles.z;
        transform.eulerAngles = new Vector3(0, 0, Angle);
    }
}

}
