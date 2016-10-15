using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles user input and adjusts scope magnification accordingly
/// </summary>
public class ZoomLevelController : MonoBehaviour {

    //Control keycodes
    public string ZoomInKeycode = "up";
    public string ZoomOutKeycode = "down";

    private Camera _scopeCam;
    private float _initialFOV;

    private enum MagnificationVal {
        _1x, 
        _2x,
        _4x,
        _8x,
        _16x,
        _32x,
        _64x,
        _128x,
        _256x
    }
    private MagnificationVal _magnificiation;

    void Start() {
        _scopeCam = GameObject.FindWithTag("ScopeCam").GetComponent<Camera>();
        _initialFOV = _scopeCam.fieldOfView;
        _magnificiation = MagnificationVal._1x;
    }

    void Update() {

        //Handle zoom controls
        if (Input.GetKeyDown(ZoomInKeycode)) {
            if (_magnificiation < MagnificationVal._256x) _magnificiation++;
            SetMagnification(_magnificiation);
        }
        if (Input.GetKeyDown(ZoomOutKeycode)) {
            if (_magnificiation > MagnificationVal._1x) _magnificiation--;
            SetMagnification(_magnificiation);
        }

    }

    /// <summary>
    /// Set the scope camera's magnification (via FOV)
    /// </summary>
    /// <param name="mag">Desired magnification value</param>
    private void SetMagnification(MagnificationVal mag) {
        float desiredFOV = _initialFOV;

        //TODO - not concise
        switch(mag) {
            case MagnificationVal._1x:
                _scopeCam.fieldOfView = desiredFOV;
                break;
            case MagnificationVal._2x:
                desiredFOV = _initialFOV / 2.0f;
                _scopeCam.fieldOfView = desiredFOV;
                break;
            case MagnificationVal._4x:
                desiredFOV = _initialFOV / 4.0f;
                _scopeCam.fieldOfView = desiredFOV;
                break;
            case MagnificationVal._8x:
                desiredFOV = _initialFOV / 8.0f;
                _scopeCam.fieldOfView = desiredFOV;
                break;
            case MagnificationVal._16x:
                desiredFOV = _initialFOV / 16.0f;
                _scopeCam.fieldOfView = desiredFOV;
                break;
            case MagnificationVal._32x:
                desiredFOV = _initialFOV / 32.0f;
                _scopeCam.fieldOfView = desiredFOV;
                break;
            case MagnificationVal._64x:
                desiredFOV = _initialFOV / 64.0f;
                _scopeCam.fieldOfView = desiredFOV;
                break;
            case MagnificationVal._128x:
                desiredFOV = _initialFOV / 128.0f;
                _scopeCam.fieldOfView = desiredFOV;
                break;
            case MagnificationVal._256x:
                desiredFOV = _initialFOV / 256.0f;
                _scopeCam.fieldOfView = desiredFOV;
                break;
            default:
                _scopeCam.ResetFieldOfView();
                break;
        }

        SetMagnificationText(mag);
    }

    /// <summary>
    /// Temporary - display the magnification value as worldspace text (for test scenes)
    /// </summary>
    /// <param name="mag">Desired magnification value</param>
    private void SetMagnificationText(MagnificationVal mag) {
        GameObject scopeCanvas = GameObject.Find("ScopeCanvas");
        if (scopeCanvas.transform.FindChild("MagnificationText") == null) return;
        scopeCanvas.transform.FindChild("MagnificationText").GetComponent<Text>().text = "Magnification:" + (mag.ToString().Replace('_', ' '));
    }
}
