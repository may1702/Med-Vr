using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Takes note of all triangles encountered by a specific collider on a deformable mesh.
/// Place on target mesh with collider.
/// </summary>
public class TriangleCollisionTracker : MonoBehaviour {

    public Collider ActorCollider;
    public float TriangleDistTolerance;
    private MeshCollider _targetCollider;

    void Awake() {
        _targetCollider = GetComponent<MeshCollider>();
    }

    void OnCollisionEnter(Collision collision) {
        if (collision.collider != ActorCollider) return;

        //Determine contact points of collision
        ContactPoint[] contactPoints = collision.contacts;
        RemoveTriangles(contactPoints);
    }

    void OnCollisionStay(Collision collision) {
        if (collision.collider != ActorCollider) return;

        //Determine contact points of collision
        ContactPoint[] contactPoints = collision.contacts;
        RemoveTriangles(contactPoints);
    }

    /// <summary>
    /// Remove triangles containing contact points
    /// </summary>
    /// <param name="contactPoints">All contact points for the current collision</param>
    private void RemoveTriangles(ContactPoint[] contactPoints) {
        List<Vector3> verts = new List<Vector3>(GetComponent<MeshFilter>().mesh.vertices);
        List<int> tris = new List<int>(GetComponent<MeshFilter>().mesh.triangles);
        int count = tris.Count / 3;

        //Iterate, strip any triangles that contain only contact vertices
        for (int i = count - 1; i >= 0; i--) {
            Vector3 vA = verts[tris[i * 3]];
            Vector3 vB = verts[tris[i * 3 + 1]];
            Vector3 vC = verts[tris[i * 3 + 2]];

            bool rangeA = false, rangeB = false, rangeC = false;

            //Check if vertices are in range of contact points
            foreach (ContactPoint p in contactPoints) {
                if (Vector3.Distance(vA, p.point) < TriangleDistTolerance) rangeA = true;
                else continue;
                if (Vector3.Distance(vB, p.point) < TriangleDistTolerance) rangeB = true;
                else continue;
                if (Vector3.Distance(vC, p.point) < TriangleDistTolerance) rangeC = true;
            }

            //Remove qualifying triangles
            if (rangeA && rangeB && rangeC) {
                tris.RemoveRange(i * 3, 3);
            }
        }

        //Update mesh and mesh collider to reflect changes
        GetComponent<MeshFilter>().mesh.triangles = tris.ToArray();
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().mesh;
    }

}
