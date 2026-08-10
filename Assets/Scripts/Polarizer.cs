using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ChargedObjects
{
    public class Polarizer : MonoBehaviour
    {
        // Start is called before the first frame update
        public bool linear = true;
        private float Angle;
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (linear)
            {
                Angle = transform.eulerAngles.z;
                transform.eulerAngles = new Vector3(0, 0, Angle);
            }
            else
            {
                transform.eulerAngles = new Vector3(0, 0, 0);
            }
        }
    }
}