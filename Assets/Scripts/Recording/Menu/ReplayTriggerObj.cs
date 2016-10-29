using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// This should be attached to the ReplayTrigger object
/// Stores path data for replay file while switching between menu scene and replay scene
/// </summary>
public class ReplayTriggerObj : MonoBehaviour {

    public string FullPath;

    void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    public void OnReplaySceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name == "RecordingDebug") {
            GameObject.Find("STATIC").GetComponent<Reenactor>().PrepForReplay(FullPath);
            Destroy(gameObject);
        }
    }
	
}
