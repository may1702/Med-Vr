using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// This class allows easy conversion of height in inches to meters (Unity scale units)
/// </summary>
public class HeightCompensator : MonoBehaviour {

    public int HeightInInches = 66;

    void Awake() {
        Debug.Log("Setting camera rig height...");
        SetCamRigHeight();
    }

    /// <summary>
    /// Set the camera rig's height to the player's height
    /// </summary>
    public void SetCamRigHeight() {
        GameObject camRig = GameObject.FindWithTag("VRCamRig");
        try {
            Vector3 initialPos = camRig.transform.position;
            camRig.transform.position = new Vector3(initialPos.x,
                                                    HeightInInches * .025f,
                                                    initialPos.z);
            Debug.Log("Camera rig height set to " + camRig.transform.position.y + "m");
        } catch (NullReferenceException) {
            Debug.Log("Could not find camera rig. Height will be unchanged.");
        }
    }

}
