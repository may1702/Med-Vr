using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Obi;

/// <summary>
/// Takes note of all triangles encountered by a specific collider on a deformable mesh.
/// Place on target mesh with collider.
/// </summary>
public class TriangleCollisionTracker : MonoBehaviour {

    public Collider ActorCollider;
    public float TriangleDistTolerance;
    public int TriangleRemovalInterval;
    public List<int> CollidedTris;
    public ObiCloth TargetCloth;

    private int _intervalCounter;
    private List<int> triBuffer;
    private bool _meshModifiedFlag;

    //Two possible methods for triangle removal - performance advantages in different scenarios
    public enum TriRemovalMethod {
        VectorRange,
        Raycast
    }

    public TriRemovalMethod Method;

    void Awake() {
        triBuffer = new List<int>(GetComponent<MeshFilter>().mesh.triangles);
        CollidedTris = new List<int>();
        _meshModifiedFlag = false;
    }

    void Update() {
        //Update mesh as needed on a fixed interval (updating every frame is too costly)
        if (_meshModifiedFlag) {
            if (_intervalCounter == TriangleRemovalInterval) {
                UpdateMesh();
                UpdateCloth(TargetCloth, GetComponent<MeshCollider>().sharedMesh);
                _intervalCounter = 0;
                _meshModifiedFlag = false;
            }
            else _intervalCounter++;
        }
    }

    void OnCollisionEnter(Collision collision) {
        if (collision.collider != ActorCollider) return;

        //Determine contact points of collision
        ContactPoint[] contactPoints = collision.contacts;

        if (Method == TriRemovalMethod.VectorRange) {
            RemoveTris_VectorRange(contactPoints);
        }
        else if (Method == TriRemovalMethod.Raycast) {
            RemoveTris_Raycast(contactPoints);
        }

        _intervalCounter = 0;
        _meshModifiedFlag = true;
    }

    void OnCollisionStay(Collision collision) {
        if (collision.collider != ActorCollider) return;

        //Determine contact points of collision
        ContactPoint[] contactPoints = collision.contacts;

        if (Method == TriRemovalMethod.VectorRange) {
            RemoveTris_VectorRange(contactPoints);
        }
        else if (Method == TriRemovalMethod.Raycast) {
            RemoveTris_Raycast(contactPoints);
        }

        _meshModifiedFlag = true;
    }

    /// <summary>
    /// Mark triangles containing contact points for removal
    /// </summary>
    /// <param name="contactPoints">All contact points for the current collision</param>
    private void RemoveTris_VectorRange(ContactPoint[] contactPoints) {
        List<Vector3> verts = new List<Vector3>(GetComponent<MeshFilter>().mesh.vertices);
        List<int> tris = new List<int>(triBuffer);
        int count = tris.Count / 3;

        //Determine search radius limits
        List<ContactPoint> cpList = new List<ContactPoint>(contactPoints);
        cpList.OrderBy(point => point.point.x);
        Vector2 xBounds = new Vector2(cpList[0].point.x, cpList[cpList.Count - 1].point.x);
        cpList.OrderBy(point => point.point.y);
        Vector2 yBounds = new Vector2(cpList[0].point.y, cpList[cpList.Count - 1].point.y);
        cpList.OrderBy(point => point.point.z);
        Vector2 zBounds = new Vector2(cpList[0].point.z, cpList[cpList.Count - 1].point.z);

        //Iterate, strip any triangles that contain only contact vertices
        for (int i = count - 1; i >= 0; i--) {
            Vector3 vA = verts[tris[i * 3]];
            if (!VectorIsInRange(vA, xBounds, yBounds, zBounds)) continue; //disqualify triangles with vertices past tolerance val  

            Vector3 vB = verts[tris[i * 3 + 1]];
            if (!VectorIsInRange(vB, xBounds, yBounds, zBounds)) continue;

            Vector3 vC = verts[tris[i * 3 + 2]];
            if (!VectorIsInRange(vC, xBounds, yBounds, zBounds)) continue;

            bool rangeA = false, rangeB = false, rangeC = false;

            //Check if vertices are in range of contact points
            foreach (ContactPoint p in contactPoints) {
                if (Vector3.Distance(vA, p.point * transform.localScale.x) < TriangleDistTolerance) rangeA = true;
                else continue;
                if (Vector3.Distance(vB, p.point * transform.localScale.x) < TriangleDistTolerance) rangeB = true;
                else continue;
                if (Vector3.Distance(vC, p.point * transform.localScale.x) < TriangleDistTolerance) rangeC = true;
            }

            //Remove contact triangles
            if (rangeA && rangeB && rangeC) {
                tris.RemoveRange(i * 3, 3);
            }
        }

        triBuffer = tris;
    }

    private void RemoveTris_Raycast(ContactPoint[] contactPoints) {
        int[] tris = GetComponent<MeshFilter>().mesh.triangles;
        foreach (ContactPoint cp in contactPoints) {
            RaycastHit hit;
            Collider collider = GetComponent<Collider>();
            collider.Raycast(new Ray(cp.point, cp.normal), out hit, TriangleDistTolerance);

            if (hit.collider != null && hit.triangleIndex != -1) {
                tris = removeTriangleIndividual(hit.triangleIndex, tris);
            }
        }

        triBuffer = new List<int>(tris);
    }

    /// <summary>
    /// Determine if a vector is in the tolerance range
    /// </summary>
    /// <returns></returns>
    private bool VectorIsInRange(Vector3 v, Vector2 xBounds, Vector2 yBounds, Vector2 zBounds) {
        if (v.x > xBounds.x + TriangleDistTolerance) return false;
        if (v.x < xBounds.y - TriangleDistTolerance) return false;
        if (v.y > yBounds.x + TriangleDistTolerance) return false;
        if (v.y < yBounds.y - TriangleDistTolerance) return false;
        if (v.z > zBounds.x + TriangleDistTolerance) return false;
        if (v.z < zBounds.y - TriangleDistTolerance) return false;
        return true;
    }

    private int[] removeTriangleIndividual(int triangle, int[] tris) {
        for (var i = triangle * 3; i < tris.Length - 3; ++i) {
            if (tris[i] == -1) break;
            tris[i] = tris[i + 3];
        }
        return tris;
    }

    /// <summary>
    /// Update the mesh and mesh collider to reflect triangle buffer
    /// </summary>
    private void UpdateMesh() {
        GetComponent<MeshFilter>().mesh.triangles = triBuffer.ToArray();
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().mesh;
    }

    /// <summary>
    /// Update the obi cloth to represent mesh w/ removed tris
    /// </summary>
    private void UpdateCloth(ObiCloth cloth, Mesh targetMesh) {
        ObiMeshTopology updatedTopology = ScriptableObject.CreateInstance(typeof(ObiMeshTopology)) as ObiMeshTopology;
        updatedTopology.InputMesh = targetMesh;
        //updatedTopology.Generate();
        cloth.clothMesh = targetMesh;
        cloth.sharedMesh = targetMesh;

        //cloth.topology = updatedTopology;
    }


}
