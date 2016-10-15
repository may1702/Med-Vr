using UnityEngine;
using System.Collections;

public class MeshDeformerInput : MonoBehaviour {
    public float force = 10f;
    public LayerMask ignoredLayers;
    private SteamVR_TrackedObject trackedObj;

    // Use this for initialization
    void Start () {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
	}
	
	// Update is called once per frame
	void Update () {
        // Change this later
        if (SteamVR_Controller.Input((int)trackedObj.index).GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
        {
            HandleInput();
        }
    }

    public float forceOffset = 0.1f;

    void HandleInput()
    {
        // Change this for sure
        Ray inputRay = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(inputRay, out hit))
        {
            MeshDeformer deformer = hit.collider.GetComponent<MeshDeformer>();

            if (deformer)
            {
                Vector3 point = hit.point;
                point += hit.normal * forceOffset;
                deformer.AddDeformingForce(transform.position, point, force);
            }
        }
    }
}