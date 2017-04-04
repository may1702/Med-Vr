using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuturePointRecorder : MonoBehaviour {

    public Collider NeedleCollider;
    public bool RecordPoints;

    private List<int> _suturePoints; //List of target mesh vertex indices
    public VRTK.VRTK_ControllerEvents vrtk;

    public void Awake()
    {
        RecordPoints = true;
        _suturePoints = new List<int>();
        vrtk.TriggerPressed += new VRTK.ControllerInteractionEventHandler(ClearPoints);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != NeedleCollider) return;
        if (RecordPoints)
        {
            RecordSuturePoint(collision.contacts);
        }
    }

    private void RecordSuturePoint(ContactPoint[] contacts)
    {
        ContactPoint cp = contacts[0];
        RaycastHit hit = new RaycastHit();
        Collider collider = GetComponent<Collider>();

        collider.Raycast(new Ray(cp.point, cp.normal), out hit, cp.point.magnitude);
        if (hit.collider == null)
        {
            collider.Raycast(new Ray(cp.point + NeedleCollider.transform.up, -NeedleCollider.transform.up), out hit, 1.0f);
            //Physics.SphereCast(new Ray(cp.point, cp.normal), 1, out hit, cp.point.magnitude);
            Debug.Log("Internal Hit: " + hit.triangleIndex);
        }
        else
        {
            Debug.Log("External Hit: " + hit.triangleIndex);
        }    

        //Debug.Log("hit collider null: " + (hit.collider == null).ToString());
        //Debug.Log("tri index: " + hit.triangleIndex);
        if (hit.collider != null && hit.triangleIndex != -1)
        {
            _suturePoints.Add(hit.triangleIndex * 3);
            Debug.Log("Count (outside): " + _suturePoints.Count);
            if (_suturePoints.Count == 2)
            {
                Debug.Log("Triggering suture");
                Debug.Log("s.p. 1: " + _suturePoints[0]);
                Debug.Log("s.p. 2: " + _suturePoints[1]);
                if (_suturePoints[0] != _suturePoints[1])
                {
                    GetComponent<SuturePoints>().SuturePointsTrigger(_suturePoints[0], _suturePoints[1], NeedleCollider.gameObject);
                    _suturePoints.Clear();
                }
                else
                {
                    _suturePoints.RemoveAt(1);
                }
                Debug.Log("Count (inside): " + _suturePoints.Count);
            }
        }
    }

    public void ClearPoints(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        _suturePoints.Clear();
    }
}

