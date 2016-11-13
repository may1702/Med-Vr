using UnityEngine;
using System.Collections;

public class ManualActorMovement : MonoBehaviour {

    public float Speed = .1f;
    public float RotationDegrees = 1;

    void Update() {
        if (Input.GetKey("w")) {
            transform.position += new Vector3(0, 0, Speed);
        }
        if (Input.GetKey("d")) {
            transform.position += new Vector3(Speed, 0, 0);
        }
        if (Input.GetKey("s")) {
            transform.position += new Vector3(0, 0, -Speed);
        }
        if (Input.GetKey("a")) {
            transform.position += new Vector3(-Speed, 0, 0);
        }
        if (Input.GetKey("e")) {
            transform.position += new Vector3(0, Speed, 0);
        }
        if (Input.GetKey("q")) {
            transform.position += new Vector3(0, -Speed, 0);
        }
        if (Input.GetKey("z")) {
            transform.eulerAngles += new Vector3(0, RotationDegrees);
        }
        if (Input.GetKey("c")) {
            transform.eulerAngles -= new Vector3(0, RotationDegrees);
        }
    }

}
