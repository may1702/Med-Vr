//======= Copyright (c) Valve Corporation, All rights reserved. ===============
using UnityEngine;
using System.Collections;

public struct PointerEventArgs
{
    public uint controllerIndex;
    public uint flags;
    public float distance;
    public Transform target;
}

public delegate void PointerEventHandler(object sender, PointerEventArgs e);


public class SteamVR_LaserPointer : MonoBehaviour
{
    public bool active = true;
    public Color color;
    public float thickness = 0.002f;
    public GameObject holder;
    public GameObject pointer;
    bool isActive = false;
    public bool addRigidBody = false;
    public Transform reference;
    public event PointerEventHandler PointerIn;
    public event PointerEventHandler PointerOut;

    Transform previousContact = null;

	// Use this for initialization
	void Start ()
    {
        holder = new GameObject();
        holder.transform.parent = this.transform;
        holder.transform.localPosition = Vector3.zero;

        pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pointer.transform.parent = holder.transform;
        pointer.transform.localScale = new Vector3(thickness, thickness, 100f);
        pointer.transform.localPosition = new Vector3(0f, 0f, 50f);
        BoxCollider collider = pointer.GetComponent<BoxCollider>();
        if (addRigidBody)
        {
            if (collider)
            {
                collider.isTrigger = true;
            }
            Rigidbody rigidBody = pointer.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;
        }
        else
        {
            if(collider)
            {
                Object.Destroy(collider);
            }
        }
        Material newMaterial = new Material(Shader.Find("Unlit/Color"));
        newMaterial.SetColor("_Color", color);
        pointer.GetComponent<MeshRenderer>().material = newMaterial;
	}

    public virtual void OnPointerIn(PointerEventArgs e)
    {
        if (PointerIn != null)
            PointerIn(this, e);
    }

    public virtual void OnPointerOut(PointerEventArgs e)
    {
        if (PointerOut != null)
            PointerOut(this, e);
    }


    // Update is called once per frame
	void Update ()
    {
        if (!isActive)
        {
            isActive = true;
            this.transform.GetChild(0).gameObject.SetActive(true);
        }

        SteamVR_TrackedController controller = GetComponent<SteamVR_TrackedController>();

        Ray raycast = new Ray(transform.position, transform.forward);
        RaycastHit[] hit = Physics.RaycastAll(transform.position, transform.forward, 300);

        //float minDist = hit.Length > 0 ? hit[0].distance : 0;
        float minDist = float.MaxValue;
        string hitName = "";
        for (int i = 0; i < hit.Length; i++)
        {
            hitName = hit[i].transform.name;

                //if (previousContact && previousContact != hit[i].transform)
                //{
                //    PointerEventArgs args = new PointerEventArgs();
                //    if (controller != null)
                //    {
                //        args.controllerIndex = controller.controllerIndex;
                //    }
                //    args.distance = 0f;
                //    args.flags = 0;
                //    args.target = previousContact;
                //    OnPointerOut(args);
                //    previousContact = null;
                //}
                //if (previousContact != hit[i].transform)
                //{
                    PointerEventArgs argsIn = new PointerEventArgs();
                    if (controller != null)
                    {
                        argsIn.controllerIndex = controller.controllerIndex;
                    }

                    if (!hitName.Equals("sclera_right") && hit[i].distance < minDist)
                    {
                        argsIn.distance = hit[i].distance;
                        minDist = hit[i].distance;
                        argsIn.flags = 0;
                        argsIn.target = hit[i].transform;
                        OnPointerIn(argsIn);
                    }
                    //previousContact = hit[i].transform;
                    //exit after this loop
                    //if (!hit[i].transform.name.Equals("sclera_right"))
                    //  i = hit.Length;
                //}
            }

        if (controller != null && controller.triggerPressed)
        {
            pointer.transform.localScale = new Vector3(thickness * 5f, thickness * 5f, minDist);
        }
        else
        {
            pointer.transform.localScale = new Vector3(thickness, thickness, minDist);
        }
        pointer.transform.localPosition = new Vector3(0f, 0f, minDist / 2f);
    }
}
