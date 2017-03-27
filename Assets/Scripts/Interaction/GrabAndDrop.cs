using UnityEngine;
using System.Collections;

public class GrabAndDrop : MonoBehaviour {

	GameObject grabbedObject;
	float grabbedObjectSize;

	GameObject GetMouseHoverObject(float range) {
		Vector3 position = gameObject.transform.position;
		RaycastHit raycastHit;
		Vector3 target = position + Camera.main.transform.forward * range;

		if (Physics.Linecast (position, target, out raycastHit)) {
			return raycastHit.collider.gameObject;
		}
		return null;
	}

	void TryGrabObject(GameObject grabObject) {
		if (grabObject == null || grabObject.name == "BlueTable" || grabObject.name == "Surgery room") {
			return;
		}
		grabbedObject = grabObject;
		grabbedObject.GetComponent<Rigidbody> ().freezeRotation = true;
		grabbedObjectSize = grabObject.GetComponent<Renderer>().bounds.size.magnitude;
	}

	void DropObject() {
		if (grabbedObject == null) {
			return;
		}
		grabbedObject.GetComponent<Rigidbody> ().freezeRotation = false;
		grabbedObject = null;
	}

	void Start() {
		Animator animation = GameObject.Find ("forceps-rigged").GetComponent<Animator> ();
		animation.SetBool ("close", false);
	}

	// Update is called once per frame
	void Update () {
		Debug.Log (GetMouseHoverObject (5));
		if (Input.GetMouseButtonDown (1)) {
			if (grabbedObject == null) {
				TryGrabObject (GetMouseHoverObject (5));
			} else {
				DropObject ();
			}
		}

		if (Input.GetButton ("Fire1")) {
			if (grabbedObject != null) {
				if (grabbedObject.name == "forceps-rigged") {
					Animator animation = GameObject.Find ("forceps-rigged").GetComponent<Animator> ();
					bool closeBool = animation.GetBool ("close");
					Debug.Log ("Close: " + closeBool.ToString());
					animation.SetBool ("close", true);
				}
			}
		} else {
			if (grabbedObject != null) {
				if (grabbedObject.name == "forceps-rigged") {
					Animator animation = GameObject.Find ("forceps-rigged").GetComponent<Animator> ();
					animation.SetBool ("close", false);
				}
			}
		}


		if (grabbedObject != null) {
			Vector3 newPosition = gameObject.transform.position + Camera.main.transform.forward * (grabbedObjectSize);
			newPosition.x = newPosition.x - 0.25f;
			GameObject cursor = GameObject.Find ("Cursor");
			newPosition.y = cursor.transform.position.y;
			newPosition.z = cursor.transform.position.z;
			grabbedObject.transform.position = newPosition;
			cursor.GetComponent<Renderer>().enabled = false;
		}
		if (Input.GetAxis ("Mouse ScrollWheel") > 0) {
			if (grabbedObject != null) {
				grabbedObject.transform.Rotate (0, 0, 5);
			}
		}
		if (Input.GetAxis ("Mouse ScrollWheel") < 0) {
			if (grabbedObject != null) {
				grabbedObject.transform.Rotate (0, 0, -5);
			}
		}
		if(Input.GetKey("q")){
			if (grabbedObject != null) {
				grabbedObject.transform.Rotate (5, 0, 0);
			}
		}
		if(Input.GetKey("e")){
			if (grabbedObject != null) {
				grabbedObject.transform.Rotate (-5, 0, 0);
			}
		}
		if(Input.GetKey("z")){
			if (grabbedObject != null) {
				grabbedObject.transform.Rotate (0, 5, 0);
			}
		}
		if(Input.GetKey("c")){
			if (grabbedObject != null) {
				grabbedObject.transform.Rotate (0, -5, 0);
			}
		}
	}
}
