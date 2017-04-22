using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuturePointRecorder : MonoBehaviour {

    public Collider NeedleCollider;
    public bool RecordPoints;

    private List<int> _suturePoints; //List of target mesh vertex indices

    public void Awake()
    {
        RecordPoints = true;
        _suturePoints = new List<int>();
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
        RaycastHit hit;
        Collider collider = GetComponent<Collider>();
        collider.Raycast(new Ray(cp.point, cp.normal), out hit, cp.point.magnitude);

        if (hit.collider != null && hit.triangleIndex != -1)
        {
            _suturePoints.Add(hit.triangleIndex * 3);
            if (_suturePoints.Count == 2)
            {
                GetComponent<SuturePoints>().SuturePointsTrigger(_suturePoints[0], _suturePoints[1], NeedleCollider.gameObject);
                _suturePoints.Clear();
            }
        }
    }
}

