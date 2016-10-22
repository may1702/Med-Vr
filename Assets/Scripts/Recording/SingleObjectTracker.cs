using UnityEngine;
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

    void Awake() {
        ResetTracker();
        _initialState = new ObjectFrame(gameObject, -1);
        StartCoroutine(RecordObjectFrames());
    }

    private IEnumerator RecordObjectFrames() {
        for (;;) {
            if (_frameCount % IntervalFrames == 0) {
                _recordedFrames.Add(new ObjectFrame(gameObject, _frameCount));
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
        //StopAllCoroutines();
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
}

/// <summary>
/// This class represents one recorded frame for one object
/// Stores only the bare necessities - transform and frame index (for later reconstruction)
/// </summary>
public class ObjectFrame {

    public int FrameIndex { get; private set; }
    public GameObject Object { get; private set; }

    public Transform TransformData { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 LocalPosition { get; private set; }
    public Vector3 EulerAngles { get; private set; }
    public Vector3 LocalEulerAngles { get; private set; }
    public Vector3 LocalScale { get; private set; }
    public Quaternion Rotation { get; private set; }
    public Quaternion LocalRotation { get; private set; }
    

    public ObjectFrame(GameObject subject, int frameIndex) {
        FrameIndex = frameIndex;
        Object = subject;

        TransformData = subject.transform;
        Position = subject.transform.position;
        LocalPosition = subject.transform.localPosition;
        EulerAngles = subject.transform.eulerAngles;
        LocalEulerAngles = subject.transform.eulerAngles;
        LocalScale = subject.transform.localScale;
        Rotation = subject.transform.rotation;
        LocalRotation = subject.transform.localRotation;
    }

}
