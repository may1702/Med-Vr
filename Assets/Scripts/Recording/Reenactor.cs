using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Using prebuilt frame and event timelines, reenact recorded actions
/// </summary>
public class Reenactor : MonoBehaviour {

    public SceneRecorder Recorder;
    private Dictionary<int, List<ObjectFrame>> FrameTimeline;
    private List<int> ActiveFrames;
    private int _frameCount;
    private int _latestFrame;

    void Awake() {
        _frameCount = 0;
    }

    void Update() {
        if (Input.GetKeyDown("3")) {
            Recorder.CollectRecordedData();
            RetrieveFrameTimeline();
            _latestFrame = GetLatestActiveFrame();
        }
        if (Input.GetKeyDown("4")) {
            ResetTrackedObjectStates();
            StartCoroutine(ReenactFrameTimeline());
        }
    }

    /// <summary>
    /// Pull timeline and active frame data from recorder
    /// </summary>
    public void RetrieveFrameTimeline() {
        if (Recorder == null) {
            Debug.Log("Could not find scene recorder instance.");
            return;
        }
        ActiveFrames = Recorder.GetActiveFrames();
        FrameTimeline = Recorder.GetFrameTimeline();
    }

    /// <summary>
    /// Reset tracked objects to their initial states as per the first recorded object frame
    /// </summary>
    private void ResetTrackedObjectStates() {
        foreach (SingleObjectTracker tracker in Recorder.TrackedSingleObjects) {
            ReenactFrame(tracker.GetObjInitialState());
        }
    }

    /// <summary>
    /// Reenact the frames recorded by scenerecorder - handle on object to object/event to event basis
    /// </summary>
    /// <returns></returns>
    public IEnumerator ReenactFrameTimeline() {
        while (_frameCount < _latestFrame) {
            if (FrameTimeline.ContainsKey(_frameCount)) {
                foreach (ObjectFrame frame in FrameTimeline[_frameCount]) {
                    ReenactFrame(frame);
                }
            }         

            _frameCount++;
            yield return new WaitForEndOfFrame();
        }
        _frameCount = 0;
    }

    /// <summary>
    /// Reenact a single object frame
    /// </summary>
    /// <param name="frame">The objectframe to reenact</param>
    private void ReenactFrame(ObjectFrame frame) {
        if (frame.Options.RecordPosition) frame.Object.transform.position = frame.Position;
        if (frame.Options.RecordLocalPosition) frame.Object.transform.localPosition = frame.LocalPosition;
        if (frame.Options.RecordLocalScale) frame.Object.transform.localScale = frame.LocalScale;
        if (frame.Options.RecordEulerAngles) frame.Object.transform.eulerAngles = frame.EulerAngles;
        if (frame.Options.RecordLocalEulerAngles) frame.Object.transform.localEulerAngles = frame.LocalEulerAngles;
        if (frame.Options.RecordRotation) frame.Object.transform.rotation = frame.Rotation;
        if (frame.Options.RecordLocalRotation) frame.Object.transform.localRotation = frame.LocalRotation;
    }

    private int GetLatestActiveFrame() {
        return ActiveFrames[ActiveFrames.Count - 1];
    }

}
