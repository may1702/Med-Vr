using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

/// <summary>
/// Trigger a recording load from the menu buttons
/// </summary>
public class TriggerRecordingLoad : MonoBehaviour {

    private FileInfo _loadTarget;

    /// <summary>
    /// Wait for 5 seconds, then load recording
    /// </summary>
    public void OnLoadClick() {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadCountdown());
    }

    private IEnumerator LoadCountdown() {
        int RemainingSeconds = 5;
        while (RemainingSeconds > 0) {
            GameObject.Find("Canvas/StatusText").GetComponent<Text>().text =
                "Loading recording " + _loadTarget.Name + " in " + RemainingSeconds + "...";

            RemainingSeconds--;
            yield return new WaitForSeconds(1.0f);
        }

        TriggerLoad();
        yield return null;
    }

    /// <summary>
    /// Set up the replay trigger object
    /// </summary>
    private void TriggerLoad() {
        GameObject trigger = GameObject.Find("ReplayTrigger");
        trigger.GetComponent<ReplayTriggerObj>().FullPath = _loadTarget.FullName;

        SceneManager.sceneLoaded += trigger.GetComponent<ReplayTriggerObj>().OnReplaySceneLoaded;
        SceneManager.LoadScene("RecordingDebug");
    }

    public void SetLoadTarget(FileInfo target) {
        _loadTarget = target;
    }

}
