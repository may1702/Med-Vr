using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Using prebuilt frame and event timelines, reenact recorded actions
/// </summary>
public class Reenactor : MonoBehaviour {

    public SceneRecorder Recorder;
    private Dictionary<int, List<ObjectFrame>> ObjectFrameTimeline;
    private List<int> ActiveFrames;
    private int _frameCount;
    private int _latestFrame;
    private bool _replayPaused;

    void Awake() {
        _replayPaused = false;
        _frameCount = 0;
    }

    void Update() {
        /*
        if (Input.GetKeyDown("5")) {
            Recorder.CollectRecordedData();
            RetrieveFrameTimeline();
            _latestFrame = GetLatestActiveFrame();
            ReplayDataHandler.WriteReplayObjectData(ObjectFrameTimeline, "test1");
        }
        if (Input.GetKeyDown("6")) {
            ResetTrackedObjectStates();
            ObjectFrameTimeline = ReplayDataHandler.ReadReplayObjectData("test1", out ActiveFrames);
            _latestFrame = GetLatestActiveFrame();
            StartCoroutine(ReenactFrameTimeline());
        }
        */
    }

    public void PrepForReplay(string fullpath) {
        ResetTrackedObjectStates();
        ObjectFrameTimeline = ReplayDataHandler.ReadReplayObjectDataFromPath(fullpath, out ActiveFrames);
        _latestFrame = GetLatestActiveFrame();
        StartCoroutine(ReenactFrameTimeline());
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
        ObjectFrameTimeline = Recorder.GetObjectFrameTimeline();
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
        _frameCount = 0;
        while (_frameCount < _latestFrame) {
            if (_replayPaused) yield return new WaitForEndOfFrame();
            else
            {
                if (ObjectFrameTimeline.ContainsKey(_frameCount))
                {
                    foreach (ObjectFrame frame in ObjectFrameTimeline[_frameCount])
                    {
                        ReenactFrame(frame);
                    }
                }

                _frameCount++;
            }            
            yield return new WaitForEndOfFrame();
        }
        _frameCount = 0;
    }

    /// <summary>
    /// Reenact a single object frame
    /// </summary>
    /// <param name="frame">The objectframe to reenact</param>
    private void ReenactFrame(ObjectFrame frame) {
        if (frame == null) return; //Occasionally, the first frame in the timeline is null. Fix in the future, this is fine for now

        GameObject target;
        if (frame.Object == null) target = GameObject.Find(frame.ObjectName);
        else target = frame.Object;

        if (target == null || target.GetComponent<SingleObjectTracker>() == null) {
            return;
        }

        if (frame.Options.RecordPosition) target.transform.position = frame.Position;
        if (frame.Options.RecordLocalPosition) target.transform.localPosition = frame.LocalPosition;
        if (frame.Options.RecordLocalScale) target.transform.localScale = frame.LocalScale;
        if (frame.Options.RecordEulerAngles) target.transform.eulerAngles = frame.EulerAngles;
        if (frame.Options.RecordLocalEulerAngles) target.transform.localEulerAngles = frame.LocalEulerAngles;
        if (frame.Options.RecordRotation) target.transform.rotation = frame.Rotation;
        if (frame.Options.RecordLocalRotation) target.transform.localRotation = frame.LocalRotation;
    }

    private int GetLatestActiveFrame() {
        return ActiveFrames[ActiveFrames.Count - 1];
    }

    //public void TogglePauseReenactment(object sender, VRTK.ControllerInteractionEventArgs e) {
    //    _replayPaused = !_replayPaused;
    //}

}
