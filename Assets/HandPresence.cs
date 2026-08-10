using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandPresence : MonoBehaviour
{
    private InputDevice targetDevice;
    public GameObject handprefab;
    public InputDeviceCharacteristics controllerCharacteristics;
    private GameObject spawnHand;
    private Animator handAnimator;
    // Start is called before the first frame update
    void Start()
    {
        List<InputDevice> devices = new List<InputDevice>();
        
        InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);
        foreach (var item in devices) Debug.Log(item.name+item.characteristics);
        if (devices.Count > 0)
        {
            targetDevice = devices[0];
            spawnHand = Instantiate(handprefab, transform);
            handAnimator = spawnHand.GetComponent<Animator>();
        }

    }


    // Update is called once per frame
    void Update()
    {
        if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            handAnimator.SetFloat("Trigger", triggerValue);
        }
        else
        {
            handAnimator.SetFloat("Trigger", 0);
        }
        if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            handAnimator.SetFloat("Grip", gripValue);
        }
        else
        {
            handAnimator.SetFloat("Grip", 0);
        }
    }
}
