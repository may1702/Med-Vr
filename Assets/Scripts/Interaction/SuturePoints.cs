using System.Collections;
using System.Collections.Generic;
using Obi;
using UnityEngine;
using System;

public class SuturePoints : MonoBehaviour {

    public VRTK.VRTK_ControllerEvents vrtk;
    public float lerpTimescale = 0.0f;
    public ObiCloth Cloth;

    private Vector3 _sutureMidpoint;
    private Vector3 pointA;
    private Vector3 pointB;

    private bool _sutureActive;
    private Mesh _targetMesh;
    private GameObject needleObj;
    private Vector3[] _verts;

    private SutureData currentSutureData;

    public struct SutureData {
        public int indexA;
        public int indexB;
        public int particleIndexA;
        public int particleIndexB;
        public Vector3 initialA;
        public Vector3 initialB;
        public Vector3 newA;
        public Vector3 newB;
        public Vector3 initialNeedlePos;
    }

    private void Awake()
    {
        vrtk.TriggerPressed += new VRTK.ControllerInteractionEventHandler(EndSuture);
        _targetMesh = GetComponent<MeshFilter>().sharedMesh;
    }

    private void Update()
    {
        //if (_sutureActive) UpdateCloth(GetComponent<MeshFilter>().sharedMesh);
    }


	public void SuturePointsTrigger(int indA, int indB, GameObject needleObj)
    {
        currentSutureData = new SutureData();

        //Cloth.Solver.enabled = false;
        int indexA = _targetMesh.triangles[indA];
        int indexB = _targetMesh.triangles[indB];
        currentSutureData.indexA = indexA;
        currentSutureData.indexB = indexB;

        _sutureActive = true;
        pointA = new Vector3(_targetMesh.vertices[indexA].x, _targetMesh.vertices[indexA].y, _targetMesh.vertices[indexA].z);
        pointB = new Vector3(_targetMesh.vertices[indexB].x, _targetMesh.vertices[indexB].y, _targetMesh.vertices[indexB].z);
        currentSutureData.initialA = transform.TransformPoint(pointA);
        currentSutureData.initialB = transform.TransformPoint(pointB);

        this.needleObj = needleObj;
        _sutureMidpoint = new Vector3((pointA.x + pointB.x) / 2.0f, (pointA.y + pointB.y) / 2.0f, (pointA.z + pointB.z) / 2.0f);
        _verts = _targetMesh.vertices;
        GetComponent<SuturePointRecorder>().RecordPoints = false;
        currentSutureData.initialNeedlePos = needleObj.transform.position;

        GetRelatedIndices();

        StartCoroutine(SuturePointsCo(indexA, indexB));
    }

    public IEnumerator SuturePointsCo(int indexA, int indexB)
    {
        while(_sutureActive)
        {
            //Debug.Log(Vector3.Distance(needleObj.transform.position, transform.TransformPoint(currentSutureData.initialB)));
            //lerpTimescale = Vector3.Distance(needleObj.transform.position, transform.TransformPoint(currentSutureData.initialB));
            //lerpTimescale += (needleObj.GetComponent<Rigidbody>().velocity.magnitude / 10) * Time.deltaTime;
            //lerpTimescale += .15f * Time.deltaTime;
            lerpTimescale = (Vector3.Distance(currentSutureData.initialNeedlePos, needleObj.transform.position)) / Vector3.Magnitude(currentSutureData.initialA - currentSutureData.initialB);
            Vector3 newA = new Vector3(
                Mathf.Lerp(pointA.x, _sutureMidpoint.x, lerpTimescale),
                Mathf.Lerp(pointA.y, _sutureMidpoint.y, lerpTimescale),
                Mathf.Lerp(pointA.z, _sutureMidpoint.z, lerpTimescale));

            Vector3 newB = new Vector3(
                Mathf.Lerp(pointB.x, _sutureMidpoint.x, lerpTimescale),
                Mathf.Lerp(pointB.y, _sutureMidpoint.y, lerpTimescale),
                Mathf.Lerp(pointB.z, _sutureMidpoint.z, lerpTimescale));

            _verts[indexA] = newA;
            _verts[indexB] = newB;
            pointA = newA;
            pointB = newB;
            currentSutureData.newA = newA;
            currentSutureData.newB = newB;

            //Update mesh
            GetComponent<MeshFilter>().sharedMesh.vertices = _verts;
            GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().mesh.vertices = _verts;
            GetComponent<MeshFilter>().mesh.RecalculateBounds();
            GetComponent<MeshCollider>().sharedMesh.vertices = _verts;
            GetComponent<MeshCollider>().sharedMesh.RecalculateBounds();

            UpdateCloth(GetComponent<MeshCollider>().sharedMesh, currentSutureData);

            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    public void EndSuture(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        if (_sutureActive)
        {
            GetComponent<SuturePointRecorder>().RecordPoints = true;
            _sutureActive = false;
            Mesh newMesh = Instantiate(GetComponent<MeshCollider>().sharedMesh);
            //Cloth.Solver.enabled = true;
            //Update cloth
            UpdateCloth(GetComponent<MeshCollider>().sharedMesh, currentSutureData);
            currentSutureData = new SutureData();
        }
     
    }

    public void UpdateCloth(Mesh newMesh, SutureData sData)
    {   
        Cloth.positions[sData.particleIndexA] = sData.newA;
        Cloth.positions[sData.particleIndexB] = sData.newB;
        Cloth.PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.ALL));
        
    }

    private void GetRelatedIndices()
    {
        int particleIndexA = 0;
        int particleIndexB = 0;
        for (int i = 0; i < Cloth.positions.Length; i++)
        {
            Vector3 pPos = Cloth.GetParticlePosition(i);
            //Debug.Log("Position " + i + " = " + pPos);
            if (VectorSimilar(pPos, currentSutureData.initialA, 0.1f)) particleIndexA = i;
            if (VectorSimilar(pPos, currentSutureData.initialB, 0.1f)) particleIndexB = i;
        }
        currentSutureData.particleIndexA = particleIndexA;
        currentSutureData.particleIndexB = particleIndexB;

        Debug.Log("Initial A: " + currentSutureData.initialA);
        Debug.Log("Initial B: " + currentSutureData.initialB);
        Debug.Log("A, B: (" + currentSutureData.particleIndexA + ", " + currentSutureData.particleIndexB + ")");
    }

    private bool VectorSimilar(Vector3 A, Vector3 B, float tolerance)
    {
        if (Math.Abs(A.x - B.x) < tolerance &&
            Math.Abs(A.y - B.y) < tolerance &&
            Math.Abs(A.z - B.z) < tolerance)
        {
            return true;
        }

        return false;
    }
}
