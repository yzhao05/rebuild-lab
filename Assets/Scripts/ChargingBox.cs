using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChargedObjects;
using EMVR.Assist.Logging;
using EMVR.Assist.Objects;

public class ChargingBox : MonoBehaviour
{
    public int sign = 1;
    private float timer = 0f;
    private static Material positiveZoneMaterial;
    private static Material negativeZoneMaterial;

    void OnTriggerStay(Collider collider)
    {
        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            GameObject thisobject = collider.gameObject;
            ChargedObjects.Wire wire = thisobject.GetComponent<ChargedObjects.Wire>();
            if (wire != null)
            {
                if ((wire.electric_mag < 5) && (sign > 0))
                {
                    wire.electric_mag += sign;
                    colorChange(thisobject, wire.electric_mag);
                    LogChargeChange(thisobject);
                }
                else if ((wire.electric_mag > -5) && (sign < 0))
                {
                    wire.electric_mag += sign;
                    colorChange(thisobject,wire.electric_mag);
                    LogChargeChange(thisobject);
                }
            }


            ChargedObjects.Plane plane = thisobject.GetComponent<ChargedObjects.Plane>();
            if (plane != null)
            {
                if ((plane.electric_mag < 5) && (sign > 0))
                {
                    plane.electric_mag += sign;
                    colorChange(thisobject, plane.electric_mag);
                    LogChargeChange(thisobject);
                }
                else if ((plane.electric_mag > -5) && (sign < 0))
                {
                    plane.electric_mag += sign;
                    colorChange(thisobject, plane.electric_mag);
                    LogChargeChange(thisobject);
                }
            }

            ChargedObjects.Sphere sphere = thisobject.GetComponent<ChargedObjects.Sphere>();
            if (sphere != null)
            {
                if ((sphere.electric_mag < 5) && (sign > 0))
                {
                    sphere.electric_mag += sign;
                    colorChange(thisobject, sphere.electric_mag);
                    LogChargeChange(thisobject);
                }
                else if ((sphere.electric_mag > -5) && (sign < 0))
                {
                    sphere.electric_mag += sign;
                    colorChange(thisobject, sphere.electric_mag);
                    LogChargeChange(thisobject);
                }
            }

            ChargedObjects.Solenoid solenoid = thisobject.GetComponent<ChargedObjects.Solenoid>();
            if (solenoid != null)
            {
                if ((solenoid.electric_mag < 5) && (sign > 0))
                {
                    solenoid.electric_mag += sign;
                    colorChange(thisobject, solenoid.electric_mag);
                    LogChargeChange(thisobject);
                }
                else if ((solenoid.electric_mag > -5) && (sign < 0))
                {
                    solenoid.electric_mag += sign;
                    colorChange(thisobject, solenoid.electric_mag);
                    LogChargeChange(thisobject);
                }
            }

            timer = 0f; // 重置计时器
        }
    }

    private void LogChargeChange(GameObject target)
    {
        if (target == null || InteractionEventLogger.Instance == null) return;

        ExperimentObject experimentObject = target.GetComponentInParent<ExperimentObject>();
        if (experimentObject == null) return;

        InteractionEventLogger.Instance.LogAction("charge_object", experimentObject);
    }

    void colorChange(GameObject wire, float current_mag)
    {
        var wireRenderer = wire.GetComponent<Renderer>();
        print(current_mag);
        switch (current_mag)
        {
            case 0:
                wireRenderer.material.SetColor("_Color", new Color(1f, 1f, 1f));
                break;
            //light sky blue   
            case 1:
                wireRenderer.material.SetColor("_Color", new Color(135f / 255f, 206f / 255f, 250f / 255f, 0.6f)); 
                break;
            //deep sky blue
            case 2:
                wireRenderer.material.SetColor("_Color", new Color(0f / 255f, 191f / 255f, 255f / 255f, 0.6f));
                break;
            //dodger blue
            case 3:
                wireRenderer.material.SetColor("_Color", new Color(30f / 255f, 144f / 255f, 255f / 255f, 0.6f));
                break;
            //blue
            case 4:
                wireRenderer.material.SetColor("_Color", new Color(0f / 255f, 0f / 255f, 255f / 255f, 0.6f));
                break;
            //navy blue 
            case 5:
                wireRenderer.material.SetColor("_Color", new Color(0f / 255f, 0f / 255f, 128f / 255f, 0.6f));
                break;
            //light sky blue   
            case -1:
                wireRenderer.material.SetColor("_Color", new Color(252f / 255f, 107f / 255f, 3f / 255f, 0.6f));
                break;
            //deep sky blue
            case -2:
                wireRenderer.material.SetColor("_Color", new Color(252f / 255f, 86f / 255f, 3f / 255f, 0.6f));
                break;
            //dodger blue
            case -3:
                wireRenderer.material.SetColor("_Color", new Color(252f / 255f, 65f / 255f, 3f / 255f, 0.6f));
                break;
            //blue
            case -4:
                wireRenderer.material.SetColor("_Color", new Color(252f / 255f, 40f / 255f, 3f / 255f, 0.6f));
                break;
            //navy blue 
            case -5:
                wireRenderer.material.SetColor("_Color", new Color(252f / 255f, 3f / 255f, 3f / 255f, 0.6f));
                break;
        }
    }
    void Start()
    {
        RestoreChargeZonePresentation();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void RestoreChargeZonePresentation()
    {
        gameObject.SetActive(true);

        Collider chargeCollider = GetComponent<Collider>();
        if (chargeCollider == null)
        {
            chargeCollider = gameObject.AddComponent<BoxCollider>();
        }
        chargeCollider.enabled = true;
        chargeCollider.isTrigger = true;

        Renderer chargeRenderer = GetComponent<Renderer>();
        if (chargeRenderer != null)
        {
            chargeRenderer.enabled = true;
            chargeRenderer.sharedMaterial = sign >= 0 ? GetPositiveZoneMaterial() : GetNegativeZoneMaterial();
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }
        body.useGravity = false;
        body.isKinematic = true;
    }

    static Material GetPositiveZoneMaterial()
    {
        if (positiveZoneMaterial == null)
        {
            positiveZoneMaterial = CreateZoneMaterial("EMVR Positive Charge Zone", new Color(0.12f, 0.45f, 1f, 0.42f));
        }

        return positiveZoneMaterial;
    }

    static Material GetNegativeZoneMaterial()
    {
        if (negativeZoneMaterial == null)
        {
            negativeZoneMaterial = CreateZoneMaterial("EMVR Negative Charge Zone", new Color(1f, 0.18f, 0.05f, 0.42f));
        }

        return negativeZoneMaterial;
    }

    static Material CreateZoneMaterial(string materialName, Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = materialName;
        material.color = color;
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }
}
