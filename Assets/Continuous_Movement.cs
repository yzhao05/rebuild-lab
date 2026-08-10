using System.Collections;
using System.Collections.Generic;

using Unity.XR.CoreUtils;

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class Continuous_Movement : MonoBehaviour
{
    // Start is called before the first frame update
    public XRNode inputsource;
    public float speed = 1;
    private XROrigin rig;
    private Vector2 inputAxis;
    private CharacterController character;
    void Start()
    {
        character = GetComponent<CharacterController>();
        rig = GetComponent<XROrigin>();
    }

    // Update is called once per frame
    void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputsource);
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out inputAxis);
        
    }
    private void FixedUpdate()
    {
        Quaternion headYaw = Quaternion.Euler(0, rig.Camera.transform.eulerAngles.y, 0);
        Vector3 direction = headYaw* new Vector3(inputAxis.x, 0, inputAxis.y);
        character.Move(direction * Time.fixedDeltaTime * speed);
    }
}
