using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Simple property of scope cameras - should the viewing angle affect the image seen through the lens?
/// For a more "realistic" experience, yes. For usability purposes, maybe not.
/// </summary>
public class ViewAngleCompensator : MonoBehaviour {

    public bool MatchViewAngle = false;
    public GameObject EyeCamRig;

    void Update() {
        if (MatchViewAngle) AdjustScopeCamAngle();
    }

    /// <summary>
    /// Adjust the angles of the scope camera to match those of the HMD camera rig
    /// </summary>
    private void AdjustScopeCamAngle() {
        GameObject scopeCam = GameObject.FindWithTag("ScopeCam");
        try {
            scopeCam.transform.eulerAngles = EyeCamRig.transform.eulerAngles;
        } catch (NullReferenceException) {
            Debug.Log("Either the scope camera or the eye camera rig could not be found.");
        }
    }
	
}
