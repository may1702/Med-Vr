using UnityEngine;
using System.Collections;

public class LinkObject : MonoBehaviour {

    Quaternion rotation;
    public GameObject meshColliderObject;

    void Start()
    {
        
    }

    //void Awake()
    //{
    //    rotation = transform.rotation;
    //}

    //void LateUpdate()
    //{
    //    transform.rotation = rotation;
    //}

    void Update()
    {
        meshColliderObject.transform.position = transform.position;
        meshColliderObject.transform.rotation = transform.rotation;
    }
}
