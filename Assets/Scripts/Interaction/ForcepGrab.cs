using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForcepGrab : MonoBehaviour {

    public List<GameObject> GrabbableObjects;
    private ForcepsAnimControl fAnim;
    private Transform _grabbedParentRef;
    private GameObject _grabbedObj;
    private Vector3 _grabbedInitialScale;

    private void Awake()
    {
        fAnim = GetComponent<ForcepsAnimControl>();
        fAnim.vrtk.TriggerReleased += new VRTK.ControllerInteractionEventHandler(DropObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (fAnim.anim.GetBool("close") && GrabbableObjects.Contains(collision.gameObject))
        {
            Debug.Log("grabbing");
            GrabObject(collision);
        }
    }

    private void GrabObject(Collision collision)
    {
        GrabbableObjects.Remove(collision.gameObject);
        _grabbedObj = collision.gameObject;
        _grabbedParentRef = collision.gameObject.transform.parent;
        _grabbedInitialScale = collision.gameObject.transform.localScale;
        collision.gameObject.transform.SetParent(transform, true);
        collision.gameObject.transform.localScale = _grabbedInitialScale / transform.localScale.x; 
    }

    public void DropObject(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        _grabbedObj.transform.SetParent(_grabbedParentRef);
        GrabbableObjects.Add(_grabbedObj);
        _grabbedObj.transform.localScale = _grabbedInitialScale;

    }

}
