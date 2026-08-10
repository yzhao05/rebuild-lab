using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EMVR.Assist.Objects;

namespace ChargedObjects{
public class Sphere : MonoBehaviour
{
    public float electric_mag;
    public TextMeshPro chargeDisplayText;
    public ChargedObjects.Sphere sphere;
    public GameObject radius_detect;
    public GameObject one;
    public GameObject two;
    public GameObject three;
    public GameObject four;
    public GameObject five;
    public GameObject six;
    // Start is called before the first frame update
    void Start()
    {
        electric_mag = 0;
        ElectrostaticsAssistBinder.EnsureLab1Object(gameObject, ExperimentObjectType.Sphere, "Point Charge");
    }

    // Update is called once per frame
    void Update()
    {
        RefreshChargeDisplay();
    }
    void RefreshChargeDisplay()
    {
        if (chargeDisplayText != null && sphere != null)
        {
            chargeDisplayText.text = "Charge: " + sphere.electric_mag.ToString();
        }
    }
}
}
