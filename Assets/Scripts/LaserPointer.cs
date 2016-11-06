using UnityEngine;
using System.Collections;

public class LaserPointer : MonoBehaviour {
    private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;
    public bool gripButtonDown = false;
    public bool gripButtonUp = false;
    public bool gripButtonPressed = false;

    private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)trackedObj.index); } }
    private SteamVR_TrackedObject trackedObj;

    // Use this for initialization
    void Start()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (controller == null)
        {
            Debug.Log("Controller not initialized");
            return;
        }

        gripButtonDown = controller.GetPressDown(gripButton);
        
        if (gripButtonDown)
        {
            var direction = transform.TransformDirection(Vector3.forward);
            RaycastHit hit;
            Ray raycast = new Ray(transform.position, transform.forward);

            if (Physics.Raycast(raycast, out hit))
            {
                Debug.Log(hit.collider.name);
            }
        }
    }
}
