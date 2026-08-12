using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using EMVR.Assist.Logging;
using EMVR.Assist.Objects;

public class GrabDetect_Right : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject hand;
    public static GameObject curr_grabbed;
    public GameObject temp;
    private CharacterController character;
    public XRNode inputsource;
    private float inputAxis;
    private float fieldstrength;
    private GameObject lastReportedGrabbed;
    private void OnTriggerStay(Collider collision)
    {
        if (inputAxis > 0.9f)
        {
            if (collision.gameObject != null)
            {
                curr_grabbed = collision.gameObject;
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
        temp = curr_grabbed;
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputsource);
        device.TryGetFeatureValue(CommonUsages.grip, out inputAxis);
        if (inputAxis < 0.5) curr_grabbed = null;
        ReportSelectionChange(curr_grabbed);
        if (curr_grabbed!=null)
        {
            fieldstrength = MagneticField.right_field;
            //Debug.Log(fieldstrength);
            Debug.Log(Mathf.Min(1f, Mathf.Max(Mathf.Log10(Mathf.Abs(fieldstrength)), 0f)));
            if (fieldstrength!=0)
                device.SendHapticImpulse(0, Mathf.Min(1f,Mathf.Max(Mathf.Log10(Mathf.Abs(fieldstrength))-0.3f,0f)), 0.5f);
        }
    }

    private void ReportSelectionChange(GameObject grabbed)
    {
        if (grabbed == lastReportedGrabbed) return;

        ExperimentObject previous = lastReportedGrabbed != null
            ? lastReportedGrabbed.GetComponentInParent<ExperimentObject>()
            : null;

        ExperimentObject current = grabbed != null
            ? grabbed.GetComponentInParent<ExperimentObject>()
            : null;

        if (previous != null && InteractionEventLogger.Instance != null)
        {
            InteractionEventLogger.Instance.LogAction("release_object", previous);
        }

        if (current != null)
        {
            if (SelectionTracker.Instance != null)
            {
                SelectionTracker.Instance.SetSelected(current);
            }

            if (InteractionEventLogger.Instance != null)
            {
                InteractionEventLogger.Instance.LogAction("grab_object", current);
            }
        }
        else if (SelectionTracker.Instance != null
            && previous != null
            && SelectionTracker.Instance.CurrentSelected == previous)
        {
            SelectionTracker.Instance.ClearSelection();
        }

        lastReportedGrabbed = grabbed;
    }
}
