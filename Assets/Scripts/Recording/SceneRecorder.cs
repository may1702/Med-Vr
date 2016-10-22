﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This should be placed on a static object.
/// SceneRecorder acts as the controller for all recording agents in the scene.
/// Once recording is complete, build a timeline of object frames for later replay
/// </summary>
public class SceneRecorder : MonoBehaviour {

    public List<SingleObjectTracker> TrackedSingleObjects;
    private Dictionary<int, List<ObjectFrame>> FrameTimeline;
    private List<int> ActiveFrames;

    void Awake() {
        FrameTimeline = new Dictionary<int, List<ObjectFrame>>();
        ActiveFrames = new List<int>();
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Halt all recording agents in scene
    /// </summary>
    public void StopAllRecordings() {
        foreach (SingleObjectTracker tracker in TrackedSingleObjects) {
            tracker.StopAllCoroutines();
        }
    }

    /// <summary>
    /// Start all recording agents in scene
    /// </summary>
    public void StartAllRecordings() {
        foreach (SingleObjectTracker tracker in TrackedSingleObjects) {
            tracker.StopAllCoroutines();
            tracker.StartTracker();
        }
    }

    /// <summary>
    /// Collect all recorded frames, build timeline
    /// </summary>
    public void CollectRecordedData() {
        foreach (SingleObjectTracker tracker in TrackedSingleObjects) {
            foreach (ObjectFrame frame in tracker.GetRecordedFrames()) {
                if (!FrameTimeline.ContainsKey(frame.FrameIndex)) {
                    FrameTimeline.Add(frame.FrameIndex, new List<ObjectFrame>());
                    ActiveFrames.Add(frame.FrameIndex);
                }
                FrameTimeline[frame.FrameIndex].Add(frame);
            }
        }
    }

    /// <summary>
    /// For debug purposes - stop recording and print status of all recording agents
    /// </summary>
    private void PrintTrackingData() {
        StopAllRecordings();
        foreach (SingleObjectTracker tracker in TrackedSingleObjects) {
            Debug.Log("Tracked object: " + tracker.gameObject.name);
            Debug.Log("Tracked frames: " + tracker.GetRecordedFrames().Count);
            Debug.Log("Interval frames: " + tracker.IntervalFrames);
        }
    }

    /// <summary>
    /// For debug purposes - stop recording and print status of assembled timeline
    /// </summary>
    private void PrintTimelineData() {
        StopAllRecordings();
        CollectRecordedData();
        foreach (int key in FrameTimeline.Keys) {
            Debug.Log("*** FRAME " + key + ":");
            foreach (ObjectFrame frame in FrameTimeline[key]) {
                Debug.Log(frame.Object.name);
            }
        }
    }

    public void ResetFrameTimeline() {
        FrameTimeline.Clear();
        ActiveFrames.Clear();
    }

    public Dictionary<int, List<ObjectFrame>> GetFrameTimeline() {
        return FrameTimeline;
    }
	
    public List<int> GetActiveFrames() {
        return ActiveFrames;
    }
}
