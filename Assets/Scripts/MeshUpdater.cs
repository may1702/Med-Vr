using UnityEngine;
using System.Collections;

public class MeshUpdater : MonoBehaviour {
    Mesh mesh;
    MeshCollider meshCollider;
    GameObject child;

	// Use this for initialization
	void Start () {
        child = GameObject.Find("sclera_right");
        mesh = child.GetComponent<MeshFilter>().mesh;
        meshCollider = GetComponent<MeshCollider>();
	}
	
	// Update is called once per frame
	void Update () {
        //this.transform.position = GameObject.Find("sclera_right").transform.position;
        //transform.rotation = GameObject.Find("sclera_right").transform.rotation;
        //transform.localPosition = new Vector3(child.transform.position.x, child.transform.position.y, child.transform.position.z);

        //transform.rotation = child.transform.rotation;
        //transform.localRotation = child.transform.localRotation;
        //transform.localPosition = child.transform.localPosition;
        //transform.position = child.transform.position;
        //transform.rotation = new Quaternion(child.transform.rotation.x, child.transform.rotation.y, child.transform.rotation.z, child.transform.rotation.w);
        meshCollider.sharedMesh = mesh;
    }
}
