using System.Collections;
using System.Collections.Generic;
using Obi;
using UnityEngine;

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

    private void Awake()
    {
        vrtk.TriggerPressed += new VRTK.ControllerInteractionEventHandler(EndSuture);
        _targetMesh = GetComponent<MeshFilter>().sharedMesh;
    }

    private void Update()
    {
        //if (_sutureActive) UpdateCloth();
    }


	public void SuturePointsTrigger(int indA, int indB, GameObject needleObj)
    {
        Cloth.Solver.enabled = false;
        int indexA = _targetMesh.triangles[indA];
        int indexB = _targetMesh.triangles[indB];
        _sutureActive = true;
        pointA = new Vector3(_targetMesh.vertices[indexA].x, _targetMesh.vertices[indexA].y, _targetMesh.vertices[indexA].z);
        pointB = new Vector3(_targetMesh.vertices[indexB].x, _targetMesh.vertices[indexB].y, _targetMesh.vertices[indexB].z);
        this.needleObj = needleObj;
        _sutureMidpoint = new Vector3((pointA.x + pointB.x) / 2.0f, (pointA.y + pointB.y) / 2.0f, (pointA.z + pointB.z) / 2.0f);
        _verts = _targetMesh.vertices;
        GetComponent<SuturePointRecorder>().RecordPoints = false;
        StartCoroutine(SuturePointsCo(indexA, indexB));
    }

    public IEnumerator SuturePointsCo(int indexA, int indexB)
    {
        while(_sutureActive)
        {
            //lerpTimescale = (needleObj.GetComponent<Rigidbody>().velocity.magnitude / 10) * Time.deltaTime;
            lerpTimescale += .15f * Time.deltaTime;
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

            //Update mesh
            GetComponent<MeshFilter>().sharedMesh.vertices = _verts;
            GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().mesh.vertices = _verts;
            GetComponent<MeshFilter>().mesh.RecalculateBounds();
            GetComponent<MeshCollider>().sharedMesh.vertices = _verts;
            GetComponent<MeshCollider>().sharedMesh.RecalculateBounds();
          
            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    public void EndSuture(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        GetComponent<SuturePointRecorder>().RecordPoints = true;
        _sutureActive = false;
        Mesh newMesh = Instantiate(GetComponent<MeshCollider>().sharedMesh);
        Cloth.Solver.enabled = true;
        UpdateCloth(newMesh);
    }

    public void UpdateCloth(Mesh newMesh)
    {
        //ObiMeshTopology updatedTopology = ScriptableObject.CreateInstance(typeof(ObiMeshTopology)) as ObiMeshTopology;
        //updatedTopology.InputMesh = newMesh;
        //updatedTopology.Generate();
        //Cloth.sharedTopology = updatedTopology;
        //Cloth.topology = updatedTopology;
        Cloth.clothMesh = newMesh;
        Cloth.clothMesh.vertices = newMesh.vertices;
        Cloth.clothMesh.RecalculateBounds();
        Cloth.clothMesh.RecalculateNormals();
        Cloth.sharedMesh = newMesh;

        Cloth.CommitResultsToMesh();
        Cloth.GetMeshDataArrays(newMesh);
        Cloth.Solver.FreeParticles(Cloth.particleIndices);
        Cloth.Solver.AllocateParticles(5000);
        Cloth.Solver.UpdateActiveParticles();

        //Cloth.topology.GenerateVisualVertexBuffer();
        //Cloth.sharedTopology.GenerateVisualVertexBuffer();

        //Cloth.sharedTopology.Generate();
        //Cloth.topology.Generate();
        //newCloth.sharedTopology = updatedTopology;
    }
}
