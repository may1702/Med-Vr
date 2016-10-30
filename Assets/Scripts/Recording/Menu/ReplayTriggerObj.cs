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
            
            SteamVR_ControllerManager manager = GameObject.FindWithTag("VRCamRig").GetComponent<SteamVR_ControllerManager>();
            manager.left.GetComponent<SteamVR_TrackedObject>().enabled = false;
            manager.right.GetComponent<SteamVR_TrackedObject>().enabled = false;
            
            GameObject.Find("STATIC").GetComponent<Reenactor>().PrepForReplay(FullPath);

            Destroy(gameObject);
        }
    }
	
}
