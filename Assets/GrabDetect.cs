using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabDetect : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject hand;
    public GameObject curr_grabbed;
    public bool isright;
    private CharacterController character;
    public XRNode inputsource;
    private float inputAxis;
    private float fieldstrength;
    private void OnTriggerStay(Collider collision)
    {
        if (inputAxis > 0.9f)
        {
            if (collision.gameObject != null)
            {
                curr_grabbed.SetActive(true);
                curr_grabbed.transform.position = hand.transform.position;
            }
        }
        
    }
    void Start()
    {
        character = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputsource);
        device.TryGetFeatureValue(CommonUsages.grip, out inputAxis);
        if (inputAxis < 0.5) curr_grabbed.SetActive(false);
        if (curr_grabbed.activeSelf)
        {
            if (isright) fieldstrength = MagneticField.right_field;
            else fieldstrength = MagneticField.left_field;
            //Debug.Log(fieldstrength);
            //Debug.Log(Mathf.Min(1f, Mathf.Max(Mathf.Log10(Mathf.Abs(fieldstrength)), 0f)));
            device.SendHapticImpulse(0, Mathf.Min(1f,Mathf.Max(Mathf.Log10(Mathf.Abs(fieldstrength)),0f)), 1f);
        }
    }
}
