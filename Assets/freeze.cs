using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class freeze : MonoBehaviour {
    private GameObject obj;
    private static float x, y, z, ax, ay, az, aw;
	// Use obj for initialization
	void Start () {
        obj = this.gameObject;
        x = obj.transform.position.x;
        y = obj.transform.position.y;
        z = obj.transform.position.z;
        ax = obj.transform.rotation.x;
        ay = obj.transform.rotation.y;
        az = obj.transform.rotation.z;
        aw = obj.transform.rotation.w;
	}
	
	// Update is called once per frame
	void Update () {
        obj.transform.position = new Vector3(x, y, z);
        obj.transform.rotation = new Quaternion(ax, ay, az, aw);
	}
}
