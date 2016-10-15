using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// For testing purposes - this class controls the scope "mode" (stationary or locked to view)
/// </summary>
public class ScopeModeController : MonoBehaviour {

    //Control keycodes
    public string StationaryModeKeycode = "1";
    public string LockedModeKeycode = "2";

	public enum ScopeMode {
        Stationary,
        Locked
    }
    private ScopeMode Mode;

    //Gameobject indicating locked position - ideally empty
    public GameObject LockedScopeIndicator;
    private Transform _stationaryScopeTransform;

    void Start() {
        Mode = ScopeMode.Stationary;
        _stationaryScopeTransform = transform;
    }

    void Update() {
        
        //Handle mode controls
        if (Input.GetKeyDown(StationaryModeKeycode)) {
            Mode = ScopeMode.Stationary;
            SetStationaryMode();
        }
        if (Input.GetKeyDown(LockedModeKeycode)) {
            Mode = ScopeMode.Locked;
            SetLockedMode();
        }
    }

    /// <summary>
    /// Set this scope object to "stationary" mode - does not move in world space
    /// </summary>
    private void SetStationaryMode() {
        transform.position = _stationaryScopeTransform.position;
        transform.eulerAngles = _stationaryScopeTransform.eulerAngles;
        transform.SetParent(null);
        SetModeText();
    }

    /// <summary>
    /// Set this scope object to "locked" mode - centered in user's field of view at all times
    /// </summary>
    private void SetLockedMode() {
        transform.position = LockedScopeIndicator.transform.position;
        transform.eulerAngles = LockedScopeIndicator.transform.eulerAngles;
        transform.SetParent(LockedScopeIndicator.transform);
        SetModeText();
    }

    /// <summary>
    /// Temporary - set mode indicator text
    /// </summary>
    private void SetModeText() {
        GameObject scopeCanvas = GameObject.Find("ScopeCanvas");
        scopeCanvas.transform.FindChild("ModeText").GetComponent<Text>().text = "Mode: " + Mode.ToString();
    }
}
