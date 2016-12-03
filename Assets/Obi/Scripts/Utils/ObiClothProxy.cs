using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

/**
 * This class allows a mesh (skinned or not) to follow the simulation performed by a different cloth object. The most
 * common use case is having a high-poly mesh mimic the movement of a low-poly cloth simulation. 
 */
[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu("Physics/Obi/Obi Cloth Proxy")]
public class ObiClothProxy : MonoBehaviour {

	[Serializable]
	public struct ParticleInfluence{
		public float weight;
		public int particleIndex;
		public Vector3 bindVector;		/**< Vector from the influence particle to this vertex in bind pose.*/
	}

	MeshRenderer meshRenderer;
	MeshFilter meshFilter;
	SkinnedMeshRenderer skinnedMeshRenderer;

	[Range(1,4)]
	public int numInfluences = 2;

	[Range(1,6)]
	public float falloff = 2;

	[HideInInspector] public Mesh deformedMesh;
	[HideInInspector] public Mesh sharedMesh;				/**< Original unmodified mesh.*/

	[HideInInspector] public Quaternion[] bindPoses = null;			/**< Per-particle bind poses (inverse particle rotations stored as quaternions).*/
	[HideInInspector] public ParticleInfluence[] influences = null;	/**< Per-vertex particle influences. Its length must be numVertices*numInfluences.*/

	[SerializeField][HideInInspector] private ObiCloth proxy;
	
	protected bool initializing = false;
	[HideInInspector][SerializeField] protected bool initialized = false;

	public ObiCloth Proxy{
		set{

			if (proxy != null){
				proxy.OnFrameEnd -= Cloth_OnFrameEnd;
				proxy.OnFrameBegin -= Cloth_OnFrameBegin;
			}

				proxy = value;

			if (proxy != null){
				proxy.OnFrameBegin += Cloth_OnFrameBegin;
				proxy.OnFrameEnd += Cloth_OnFrameEnd;
			}

		}
		get{return proxy;}
	}

	public bool Initializing{
		get{return initializing;}
	}
	
	public bool Initialized{
		get{return initialized;}
	}

	void Awake () {
		skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
	}

	void OnEnable(){

		if (skinnedMeshRenderer == null)
			InitializeWithRegularMesh();
		else{
			Debug.LogWarning("Cloth proxies do not work with SkinnedMeshRenderers yet.");
		}

		if (proxy != null){
			proxy.OnFrameBegin += Cloth_OnFrameBegin;
			proxy.OnFrameEnd += Cloth_OnFrameEnd;
		}
	}

	void OnDisable(){

		if (meshFilter != null)
			meshFilter.mesh = sharedMesh;
		//if (skinnedMeshRenderer != null)
		//	skinnedMeshRenderer.sharedMesh = sharedMesh;

		GameObject.DestroyImmediate(deformedMesh);

		if (proxy != null){
			proxy.OnFrameEnd -= Cloth_OnFrameEnd;
			proxy.OnFrameBegin -= Cloth_OnFrameBegin;
		}
	}

	void Cloth_OnFrameBegin (object sender, System.EventArgs e)
	{
		proxy.needParticleOrientations = true;
	}

	void Cloth_OnFrameEnd (object sender, System.EventArgs e)
	{
		ApplyProxySkinning();
	}

	private void InitializeWithRegularMesh(){
		
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
		
		if (meshFilter == null || meshRenderer == null)
			return;
		
		// Store the shared mesh if it hasn't been stored previously.
		if (sharedMesh != null)
			meshFilter.mesh = sharedMesh;
		else
			sharedMesh = meshFilter.sharedMesh;
		
		// Make a deep copy of the original shared mesh.
		deformedMesh = Mesh.Instantiate(meshFilter.sharedMesh) as Mesh;
		deformedMesh.MarkDynamic(); 
		
		// Use the freshly created mesh copy as the renderer mesh and the half-edge input mesh, if it has been already analyzed.
		meshFilter.mesh = deformedMesh;
		
	}
	

	/**
	 * Binds the mesh to the proxy particle actor.
	 */
	public void BindToProxy(){

		initialized = false;
		initializing = false;

		if (deformedMesh == null || proxy == null) return;

		initializing = true;

		influences = new ParticleInfluence[deformedMesh.vertexCount*numInfluences];
		bindPoses = new Quaternion[proxy.positions.Length];

		// Get particle bind poses:
		for (int i = 0; i < proxy.positions.Length; ++i){
			Quaternion proxyToLocal = Quaternion.Inverse(transform.rotation) * proxy.transform.rotation;
			bindPoses[i] = Quaternion.Inverse(proxyToLocal * proxy.sharedTopology.referenceOrientation[i]);
		}

		Vector3[] vertices = deformedMesh.vertices;

		//TODO: if the vertex position has already been processed, skip it. use unique positions only.
		for (int i = 0; i < vertices.Length; ++i){

			// Find nearest "numInfluences" proxy particles:
			List<ParticleInfluence> infs = GetParticleInfluences(vertices[i]);
			
			// Calculate weights using shepard's method:
			float total = 0;
			for (int j = 0; j < infs.Count; ++j){
				ParticleInfluence pi = infs[j];
				pi.weight = 1.0f/(float)Math.Pow(Mathf.Sqrt(pi.weight),falloff);
				infs[j] = pi;
				total += infs[j].weight;
			}
		
			// Normalize to get final linear weights:
			for (int j = 0; j < infs.Count; ++j){
				ParticleInfluence pi = infs[j];
				pi.weight = pi.weight/total;
				infs[j] = pi;
			}

			// Insert this vertex's influence in the array:
			for (int j = 0; j < numInfluences; ++j)
				influences[i*numInfluences+j] = infs[j];
			
		}

		initializing = false;
		initialized = true;

	}

	/**
	 * Retrieves the indices of the particles that influece a given vertex. In the weight
	 * field of each influence, the squared distance from the particle to the vertex is provided.
	 */
	public List<ParticleInfluence> GetParticleInfluences(Vector3 vertex){

		// TODO: try to make it faster.
		List<ParticleInfluence> allInfluences = new List<ParticleInfluence>();

		// Get squared distance to all particles:
		for (int i = 0; i < proxy.positions.Length; ++i){
			ParticleInfluence influence = new ParticleInfluence();
			influence.particleIndex = i;
			influence.bindVector = transform.InverseTransformVector(transform.TransformPoint(vertex) - proxy.GetParticlePosition(i));
			influence.weight = influence.bindVector.sqrMagnitude;
			allInfluences.Add(influence);
		}

		// Sort by distance:
		allInfluences.Sort(
		delegate(ParticleInfluence x, ParticleInfluence y)
		{
			return x.weight.CompareTo(y.weight);
		});

		// Return the K nearest:
		return allInfluences.GetRange(0,numInfluences);

	}
	
	/**
	 * Deforms the mesh to match the proxy shape. The binding proccess (performed by BindToProxy()) must have been
	 * performed succesfully before calling this function.
	 */
	public void ApplyProxySkinning(){
		
		Vector3[] vertices = deformedMesh.vertices;
		Vector3[] normals = deformedMesh.normals;
		Vector4[] tangents = deformedMesh.tangents;

		Vector3[] bindNormals = sharedMesh.normals;
		Vector4[] bindTangents = sharedMesh.tangents;

		Quaternion proxyToLocal = Quaternion.Inverse(transform.rotation) * proxy.transform.rotation;

		for (int i = 0; i < vertices.Length; ++i){
		
			vertices[i] = Vector3.zero;
			normals[i] = Vector3.zero;

			// To hold tangent vector temporarily:
			Vector3 tangent = Vector3.zero;

			ParticleInfluence influence;
			for (int j = 0; j < numInfluences; ++j){

				influence = influences[i*numInfluences+j];

				// Get current particle position:
				Vector3 influencePosition = transform.InverseTransformPoint(proxy.GetParticlePosition(influence.particleIndex)); 
			
				// Calculate delta rotation with respect to bind pose rotation, in local space:
				Quaternion deltaRotation = proxyToLocal * proxy.GetParticleOrientation(influence.particleIndex) * 
										   				  bindPoses[influence.particleIndex];
	
				// Transform vertex positions, normals and tangents according to the proxy binding, all in local space:
				vertices[i] += influence.weight * (influencePosition + deltaRotation * influence.bindVector);
				normals[i] += influence.weight * (deltaRotation * bindNormals[i]);
				tangent += influence.weight * (deltaRotation * (Vector3)bindTangents[i]);
			}

			// Reset tangent w (mirror):
			tangents[i].Set(tangent.x,tangent.y,tangent.z,bindTangents[i].w);
		}

		// Assign new data to the mesh and recalculate bounds:
		deformedMesh.vertices = vertices;
		deformedMesh.normals = normals;
		deformedMesh.tangents = tangents;
		deformedMesh.RecalculateBounds();

	}
}
}
