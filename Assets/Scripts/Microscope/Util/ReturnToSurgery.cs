using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToSurgery : MonoBehaviour {

	public void ReturnToSurgeryScene() {
        SceneManager.LoadScene("Surgery");
    }
}
