using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EMVR.Assist.Objects;

namespace ChargedObjects{
public class Wire : MonoBehaviour
    {
        public GameObject head;
        public GameObject tail;
        public float electric_mag;

        
       public int Wire_density;


    public TextMeshPro chargeDisplayText;
    public ChargedObjects.Wire wire;
    public GameObject radius_detect;
    public GameObject one;
    public GameObject two;
    public GameObject three;
    public GameObject four;
    public GameObject five;
    public GameObject six;

    void Start()
    {
        electric_mag = 0;
        ElectrostaticsAssistBinder.EnsureLab1Object(gameObject, ExperimentObjectType.Wire, "Line Charge");
    }

    // Update is called once per frame
    void Update()
    {
        RefreshChargeDisplay();
    } 
    void RefreshChargeDisplay()
    {
        if (chargeDisplayText != null && wire != null)
        {
            chargeDisplayText.text = "Charge: " + wire.electric_mag.ToString();
        }
    }

}

}
