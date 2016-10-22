using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This component records the tranform and state of a single object to be assembled by the central SceneRecorder script
/// </summary>
public class SingleObjectTracker : MonoBehaviour {

    //Number of engine frames to ignore before recording next frame
    //Higher interval frames = lower performance cost
    public int IntervalFrames = 1;

    private int _frameCount;
    private List<ObjectFrame> _recordedFrames;
    private ObjectFrame _initialState;

    public ObjectTrackerOptions options;

    //Recorded property selectors
    public bool RecordPosition, RecordLocalPosition,
                RecordEulerAngles, RecordLocalEulerAngles,
                RecordRotation, RecordLocalRotation,
                RecordLocalScale, RecordTransformReference;

    void Awake() {
        SetOptions();
        ResetTracker();
        _initialState = new ObjectFrame(gameObject, -1, options);
        StartCoroutine(RecordObjectFrames());
    }

    private IEnumerator RecordObjectFrames() {
        for (;;) {
            if (_frameCount % IntervalFrames == 0) {
                _recordedFrames.Add(new ObjectFrame(gameObject, _frameCount, options));
            }
            _frameCount++;
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Retrieve the list of recorded frames - this will stop the recording process
    /// </summary>
    /// <returns>A list of recorded ObjectFrames</returns>
    public List<ObjectFrame> GetRecordedFrames() {
        StopAllCoroutines();
        return _recordedFrames;
    }

    public void ResetTracker() {
        StopAllCoroutines();
        _frameCount = 0;
        _recordedFrames = new List<ObjectFrame>();
    }

    public void StartTracker() {
        StopAllCoroutines();
        StartCoroutine(RecordObjectFrames());
    }

    public ObjectFrame GetObjInitialState() {
        return _initialState;
    }

    /// <summary>
    /// Set the options based on editor ticks
    /// </summary>
    public void SetOptions() {
        options = new ObjectTrackerOptions(RecordPosition, RecordLocalPosition,
                RecordEulerAngles, RecordLocalEulerAngles,
                RecordRotation, RecordLocalRotation,
                RecordLocalScale, RecordTransformReference);
    }
}

/// <summary>
/// This class represents one recorded frame for one object
/// Stores only the bare necessities - transform and frame index (for later reconstruction)
/// </summary>
public sealed class ObjectFrame {

    public int FrameIndex { get; private set; }
    public GameObject Object { get; private set; }
    public ObjectTrackerOptions Options { get; private set;}

    public Transform TransformData { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 LocalPosition { get; private set; }
    public Vector3 EulerAngles { get; private set; }
    public Vector3 LocalEulerAngles { get; private set; }
    public Vector3 LocalScale { get; private set; }
    public Quaternion Rotation { get; private set; }
    public Quaternion LocalRotation { get; private set; }

    public ObjectFrame(GameObject subject, int frameIndex, ObjectTrackerOptions frameOptions) {
        FrameIndex = frameIndex;
        Object = subject;
        Options = frameOptions;

        //For initial state, record everything
        if (frameIndex == -1) {
            Options = new ObjectTrackerOptions();
            Options.SetAllTrue();
        }

        //Selectively record based on options
        if (Options.RecordPosition) Position = subject.transform.position;
        if (Options.RecordLocalPosition) LocalPosition = subject.transform.localPosition;
        if (Options.RecordEulerAngles) EulerAngles = subject.transform.eulerAngles;
        if (Options.RecordLocalEulerAngles) LocalEulerAngles = subject.transform.eulerAngles;
        if (Options.RecordLocalScale) LocalScale = subject.transform.localScale;
        if (Options.RecordRotation) Rotation = subject.transform.rotation;
        if (Options.RecordLocalRotation) LocalRotation = subject.transform.localRotation;
        if (Options.RecordTransformReference) TransformData = subject.transform;
    }

}

public sealed class ObjectTrackerOptions {
    //Recorded property selectors
    public bool RecordPosition, RecordLocalPosition,
                RecordEulerAngles, RecordLocalEulerAngles,
                RecordRotation, RecordLocalRotation,
                RecordLocalScale, RecordTransformReference;

    public ObjectTrackerOptions(params bool[] options) {
        RecordPosition = options[0];
        RecordLocalPosition = options[1];
        RecordEulerAngles = options[2];
        RecordLocalEulerAngles = options[3];
        RecordRotation = options[4];
        RecordLocalRotation = options[5];
        RecordLocalScale = options[6];
        RecordTransformReference = options[7];
    }

    public ObjectTrackerOptions() {
        SetAllFalse();
    }

    /// <summary>
    /// For debug only - print the state of the options
    /// </summary>
    public void PrintOptions() {
        Debug.Log("Record position: " + RecordPosition);
        Debug.Log("Record local pos: " + RecordLocalPosition);
        Debug.Log("Record euler angles: " + RecordEulerAngles);
        Debug.Log("Record local euler angles: " + RecordLocalEulerAngles);
        Debug.Log("Record rotation: " + RecordRotation);
        Debug.Log("Record local rotation: " + RecordLocalRotation);
        Debug.Log("Record transform reference: " + RecordTransformReference);
    }

    public void SetAllTrue() {
        RecordPosition = true;
        RecordLocalPosition = true;
        RecordEulerAngles = true;
        RecordLocalEulerAngles = true;
        RecordRotation = true;
        RecordLocalRotation = true;
        RecordLocalScale = true;
        RecordTransformReference = true;
    }

    public void SetAllFalse() {
        RecordPosition = false;
        RecordLocalPosition = false;
        RecordEulerAngles = false;
        RecordLocalEulerAngles = false;
        RecordRotation = false;
        RecordLocalRotation = false;
        RecordLocalScale = false;
        RecordTransformReference = false;
    }
}