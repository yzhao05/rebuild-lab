using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EMVR.Assist.Objects;

namespace ChargedObjects
{
    public class Plane: MonoBehaviour
    {
        // Start is called before the first frame update
        public GameObject  up;
        public TextMeshPro chargeDisplayText;
        public GameObject down;
        public GameObject left;
        public GameObject right;
        public float electric_mag;
        public ChargedObjects.Plane plane;
        void Start()
        {
            electric_mag = 0;
            ElectrostaticsAssistBinder.EnsureLab1Object(gameObject, ExperimentObjectType.Plane, "Plane Charge");
        }

        // Update is called once per frame
        void Update()
        {
            RefreshChargeDisplay();
        }
        void RefreshChargeDisplay()
    {   
        if (chargeDisplayText != null && plane != null)
        {
            chargeDisplayText.text = "Charge: " + plane.electric_mag.ToString();
        }
    }
    }
}
