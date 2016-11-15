using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshCollider))]
public class InteractWithMesh : MonoBehaviour {

    MeshCollider meshCollider;

    private bool hitMesh = false;
    // Use this for initialization
    void Start () {
        meshCollider = GetComponent<MeshCollider>();        
	}
	
	// Update is called once per frame
	void Update () {

	}

    void OnCollisionEnter(Collision collision)
    {
        string otherName = collision.transform.name;

        if (otherName.Equals("sclera_right_mesh"))
        {
            hitMesh = true;
        } else if (!hitMesh && otherName.Equals("sclera_right"))
        {
            Physics.IgnoreCollision(meshCollider, collision.collider);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        string otherName = collision.transform.name;

        if (otherName.Equals("sclera_right_mesh"))
        {
            hitMesh = false;
        }
        else if (!hitMesh && otherName.Equals("sclera_right"))
        {
            Physics.IgnoreCollision(meshCollider, collision.collider, false);
        }
    }
}
