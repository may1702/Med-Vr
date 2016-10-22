using UnityEngine;
using System.Collections;

public class PreserveObject : MonoBehaviour {

	void Awake() {
        DontDestroyOnLoad(gameObject);
    }

}
