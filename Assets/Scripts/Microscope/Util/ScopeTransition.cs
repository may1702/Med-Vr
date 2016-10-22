using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// This script handles transitions from the "hot zones" around the scope object
/// The process is as follows:
///     1) Detection of player camera rig in hot zone
///     2) Indication that a transition is about to occur
///     3) Fade to black (no scene change, but will reduce motion sickness)
///     4) Activate LockedScopePerspective object - set scope camera target to rendertexture
///     5) Enable camera stabilization on lens object(?) - disable positional lens adjustments
///     6) Fade in, resume control
/// This should be added as a component on a gameobject with a hot zone collider
/// </summary>
public class ScopeTransition : MonoBehaviour {

    public float TransitionWaitTime = 2.0f;
    public float FadeDuration = 0.5f;

    public GameObject ScopeObj, HeadCamRig;
    private RawImage _scopeIcon, _eyeIcon; 
    private Image _fullViewCover;

    private bool UITransitionFlag;
    public bool ViewSnapped { private set; get; }

    void Start() {
        //Assign UI icon references
        _scopeIcon = HeadCamRig.transform.FindChild("PerspectiveCanvas/ScopeIcon").GetComponent<RawImage>();
        _eyeIcon = HeadCamRig.transform.FindChild("PerspectiveMicroCanvas/EyeIcon").GetComponent<RawImage>();
        _fullViewCover = HeadCamRig.transform.FindChild("PerspectiveCanvas/FullViewCover").GetComponent<Image>();
    }

    public void OnTriggerEnter(Collider other) {
        //If no transition is occuring and the triggered collider is a hot zone, start appropriate transition
        if (!UITransitionFlag && 
            !ViewSnapped &&
            (other.gameObject == HeadCamRig)) {
            UITransitionFlag = true;
            StartCoroutine(UITransitionIn(other));
        }
    }

    public void OnTriggerExit(Collider other) {
        //If the view is locked to the microscope, start unlocking routines
        if (!UITransitionFlag &&
            ViewSnapped &&
            (other.gameObject == HeadCamRig)) {
            Debug.Log("starting out transition");
            UITransitionFlag = true;
            StartCoroutine(UITransitionOut(other));
        }
    }



    private IEnumerator UITransitionIn(Collider other) {
        System.DateTime tPointOfNoReturn = System.DateTime.Now.AddSeconds(TransitionWaitTime);
        while (System.DateTime.Now < tPointOfNoReturn) {
            //Ensure colliders are still colliding - if not, cancel transition
            if (!other.bounds.Intersects(GetComponent<Collider>().bounds)) {
                BreakTransition();
            }

            //Raise icon alpha
            _scopeIcon.color = new Color(1, 1, 1, Mathf.Lerp(_scopeIcon.color.a, 1.5f, Time.deltaTime));
            yield return new WaitForEndOfFrame();
        }

        //Fade to black
        System.DateTime tFadeBlackFinish = System.DateTime.Now.AddSeconds(FadeDuration);
        while (System.DateTime.Now < tFadeBlackFinish) {
            _fullViewCover.color = new Color(0, 0, 0, Mathf.Lerp(_fullViewCover.color.a, 2.0f, Time.deltaTime * 2));
            yield return new WaitForEndOfFrame();
        }

        //Set collider size larger to prevent accidental unsnap
        GetComponent<BoxCollider>().size = new Vector3(1.5f, 1.5f, 1.5f);

        DisableCullingMaskLayer("Macro", HeadCamRig.GetComponent<Camera>());
        SnapViewToScope();

        //Fade in to micro view
        System.DateTime tFadeInFinish = System.DateTime.Now.AddSeconds(FadeDuration);
        while (System.DateTime.Now < tFadeInFinish) {
            _fullViewCover.color = new Color(0, 0, 0, Mathf.Lerp(_fullViewCover.color.a, -1.0f, Time.deltaTime));
            yield return new WaitForEndOfFrame();
        }

        UITransitionFlag = false;
        yield return null;
    }

    private IEnumerator UITransitionOut(Collider other) {
        yield return new WaitForSeconds(0.5f); //Allow time for colliders to fully clear each other's bounds

        System.DateTime tPointOfNoReturn = System.DateTime.Now.AddSeconds(TransitionWaitTime);
        while (System.DateTime.Now < tPointOfNoReturn) {
            //Ensure colliders are no longer colliding - if collision detected, cancel transition
            if (other.bounds.Intersects(GetComponent<Collider>().bounds)) {
                BreakTransition();
            }

            //Raise icon alpha
            _eyeIcon.color = new Color(1, 1, 1, Mathf.Lerp(_eyeIcon.color.a, 1.5f, Time.deltaTime));
            yield return new WaitForEndOfFrame();
        }

        //Set collider size larger to prevent accidental cancel
        GetComponent<BoxCollider>().size = new Vector3(0.75f, 0.5f, 0.5f);

        //Fade to black
        System.DateTime tFadeBlackFinish = System.DateTime.Now.AddSeconds(FadeDuration);
        while (System.DateTime.Now < tFadeBlackFinish) {
            _fullViewCover.color = new Color(0, 0, 0, Mathf.Lerp(_fullViewCover.color.a, 2.0f, Time.deltaTime * 2));
            yield return new WaitForEndOfFrame();
        }

        UnsnapViewFromScope();

        //Fade in to macro view
        System.DateTime tFadeInFinish = System.DateTime.Now.AddSeconds(FadeDuration);
        while (System.DateTime.Now < tFadeInFinish) {
            _fullViewCover.color = new Color(0, 0, 0, Mathf.Lerp(_fullViewCover.color.a, -1.0f, Time.deltaTime));
            yield return new WaitForEndOfFrame();
        }

        UITransitionFlag = false;
        yield return null;
    }

    /// <summary>
    /// Cancel any ongoing transitions immediately
    /// </summary>
    private void BreakTransition() {
        Debug.Log("Transition canceled.");
        UITransitionFlag = false;
        HideIcons();
        StopAllCoroutines();
    }

    /// <summary>
    /// Snap the head camera rig to scope view
    /// </summary>
    private void SnapViewToScope() {
        Debug.Log("Snapping to scope view.");
        ViewSnapped = true;
        try {
            HideIcons();

            GameObject lockedPerspectiveObj = HeadCamRig.transform.FindChild("LockedScopePerspective").gameObject;
            Camera scopeCam = GameObject.FindWithTag("ScopeCam").GetComponent<Camera>();

            //Set scope camera to render to locked perspective texture
            scopeCam.targetTexture = lockedPerspectiveObj.GetComponent<MeshRenderer>().material.mainTexture as RenderTexture;

            //Set head cam to render only micro layer (incl. locked perspective object)
            Camera headCam = HeadCamRig.GetComponent<Camera>();
            DisableCullingMaskLayer("Macro", headCam);
            DisableCullingMaskLayer("Environment", headCam);
            DisableCullingMaskLayer("EyeBall", headCam);
            EnableCullingMaskLayer("Micro", headCam);
            EnableCullingMaskLayer("Perspective_Only", headCam);
           
        } catch (NullReferenceException) {
            Debug.Log("Locked scope perspective target or scope camera was not found.");
        }
    }

    /// <summary>
    /// Unsnap the head camera rig from scope view
    /// </summary>
    private void UnsnapViewFromScope() {
        Debug.Log("Unsnapping from scope view.");
        ViewSnapped = false;
        try {
            HideIcons();

            Camera headCam = HeadCamRig.GetComponent<Camera>();
            DisableCullingMaskLayer("Perspective_Only", headCam);
            DisableCullingMaskLayer("Micro", headCam);
            EnableCullingMaskLayer("Macro", headCam);
            EnableCullingMaskLayer("Environment", headCam);
            EnableCullingMaskLayer("EyeBall", headCam);
        } catch (NullReferenceException) {
            Debug.Log("Head camera could not be found.");
        }
    }

    /// <summary>
    /// Directly alter a camera's culling mask over a single layer using a bitwise op
    /// </summary>
    /// <param name="layerName"></param>
    private void EnableCullingMaskLayer(string layerName, Camera cam) {
        cam.cullingMask |= 1 << LayerMask.NameToLayer(layerName);
    }

    /// <summary>
    /// Directly alter a camera's culling mask over a single layer using a bitwise op
    /// </summary>
    /// <param name="layerName"></param>
    private void DisableCullingMaskLayer(string layerName, Camera cam) {
        cam.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
    }

    /// <summary>
    /// Hide all icons used for transitions immediately
    /// </summary>
    private void HideIcons() {
        _scopeIcon.color = new Color(1, 1, 1, 0);
        _eyeIcon.color = new Color(1, 1, 1, 0);
    }

}
