using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuturePoints : MonoBehaviour {

    public VRTK.VRTK_ControllerEvents vrtk;
    public float lerpTimescale = 0.1f;

    private Vector3 _sutureMidpoint;
    private Vector3 pointA;
    private Vector3 pointB;

    private bool _sutureActive;
    private Mesh _targetMesh;
    private GameObject needleObj;

    private void Awake()
    {
        vrtk.TriggerPressed += new VRTK.ControllerInteractionEventHandler(EndSuture);
        _targetMesh = GetComponent<MeshFilter>().sharedMesh;
    }

	public void SuturePointsTrigger(int indexA, int indexB, GameObject needleObj)
    {
        _sutureActive = true;
        pointA = _targetMesh.vertices[indexA];
        pointB = _targetMesh.vertices[indexB];
        this.needleObj = needleObj;
        _sutureMidpoint = new Vector3(pointA.x + pointB.x / 2.0f, pointA.y + pointB.y / 2.0f, pointA.z + pointB.z / 2.0f);
        StartCoroutine(SuturePointsCo(indexA, indexB));
    }

    public IEnumerator SuturePointsCo(int indexA, int indexB)
    {
        while(_sutureActive)
        {
            lerpTimescale = needleObj.GetComponent<Rigidbody>().velocity.magnitude / 10;
            _targetMesh.vertices[indexA] = new Vector3(
                Mathf.Lerp(_targetMesh.vertices[indexA].x, _sutureMidpoint.x, lerpTimescale),
                Mathf.Lerp(_targetMesh.vertices[indexA].y, _sutureMidpoint.y, lerpTimescale),
                Mathf.Lerp(_targetMesh.vertices[indexA].z, _sutureMidpoint.z, lerpTimescale));

            _targetMesh.vertices[indexB] = new Vector3(
                Mathf.Lerp(_targetMesh.vertices[indexB].x, _sutureMidpoint.x, lerpTimescale),
                Mathf.Lerp(_targetMesh.vertices[indexB].y, _sutureMidpoint.y, lerpTimescale),
                Mathf.Lerp(_targetMesh.vertices[indexB].z, _sutureMidpoint.z, lerpTimescale));

            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    public void EndSuture(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        _sutureActive = false;
    }
}
