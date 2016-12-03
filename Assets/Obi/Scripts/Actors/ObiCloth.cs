using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{

/**
 * An ObiCloth component generates a particle-based physical representation of the object geometry
 * to be feeded to an ObiSolver component. To do that, it needs connectivity information about the mesh,
 * which is provided by an ObiMeshConnectivity asset.
 * 
 * You can use it to make flags, capes, jackets, pants, ropes, drapes, nets, or any kind of object that exhibits cloth-like behavior.
 * 
 * ObiCloth objects have their particle properties expressed in local space. That means that particle positions, velocities, etc
 * are all expressed and serialized using the object's transform as reference. Thanks to this it is very easy to instantiate cloth prefabs and move/rotate/scale
 * them around, while keeping things working as expected. 
 * 
 * For convenience, solver gravity is expressed and applied in world space. 
 * Which means that no matter how you rotate a ObiCloth object, gravity will always pull particles down.
 * (as long as gravity in your solver is meant to pulls things down, heh).
 * 
 */
[ExecuteInEditMode]
[AddComponentMenu("Physics/Obi/Obi Cloth")]
[RequireComponent(typeof (ObiDistanceConstraints))]
[RequireComponent(typeof (ObiBendingConstraints))]
[RequireComponent(typeof (ObiSkinConstraints))]
[RequireComponent(typeof (ObiAerodynamicConstraints))]
[RequireComponent(typeof (ObiVolumeConstraints))]
[RequireComponent(typeof (ObiTetherConstraints))]
[RequireComponent(typeof (ObiPinConstraints))]
public class ObiCloth : ObiActor
{

	public ObiMeshTopology sharedTopology;			/**< Reference mesh topology used to create a particle based physical representation of this actor.*/

	[Tooltip("If enabled, tangent space will be updated after each simulation step. Enable this if your cloth uses normal maps.")]
	public bool updateTangentSpace = false;

	[Tooltip("If enabled, cloth will tear if a stretching threshold is surpassed.")]
	public bool tearable = false;					/**< If true, cloth will be able to tear if tension forces between particles exceed restLenght * tearFactor.*/
	
	[Tooltip("Maximum strain betweeen particles before the spring constraint holding them together would break. A factor of 2 would make springs break when their lenght surpasses restLenght*2")]
	public float tearFactor = 1.5f;					/**< Factor that controls how much a structural cloth spring can stretch before breaking.*/

	public event EventHandler OnFrameBegin;			/**< This event should get triggered right before the actor starts simulating the current frame.*/
	public event EventHandler OnFrameEnd;			/**< This event should get triggered right after the actor has finished simulating the current frame.*/

	[HideInInspector] public ObiMeshTopology topology;		/**< Unique instance of the topology. Can be different from the shared topology due to tearing and other runtime topological changes.*/
	[HideInInspector] public Mesh clothMesh;
	[HideInInspector] public Mesh sharedMesh;				/**< Original unmodified mesh.*/

	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;
	private SkinnedMeshRenderer skinnedMeshRenderer;

	[NonSerialized] public bool needParticleOrientations = false;
	bool fixedUpdateThisFrame;					/**< true if there's been at least 1 fixed update during the frame, false otherwhise.*/

	private Matrix4x4 localToSkin;				/**< matrix that transforms from local space to skin's rootBone space.*/
	private Vector3[] meshVertices;
	private Vector3[] meshNormals;
	private Vector4[] meshTangents;
    private Quaternion[] orientation;							/**< Per particle current orientation.*/

	private ObiDistanceConstraints distanceConstraints;
	private ObiBendingConstraints bendingConstraints;
	private ObiSkinConstraints skinConstraints; 
	private ObiAerodynamicConstraints aerodynamicConstraints;
	private ObiVolumeConstraints volumeConstraints;
	private ObiTetherConstraints tetherConstraints;
	private ObiPinConstraints pinConstraints;
	
	[HideInInspector][SerializeField] private int[] constraintHalfEdgeMap;		/** constraintHalfEdgeMap[half-edge index] = distance constraint index, or -1 if there's no constraint. 
																					Each initial constraint is the lower-index of each pair of half-edges. When a half-edge is split during
																					tearing, one of the two half-edges gets its constraint updated and the other gets a new constraint.*/
	
	[HideInInspector] public float[] mass;										/**< Per particle mass.*/
	[HideInInspector] public float[] areaContribution;							/**< How much mesh surface area each particle represents.*/    

	public ObiDistanceConstraints DistanceConstraints{
		get{return distanceConstraints;}
	}
	public ObiBendingConstraints BendingConstraints{
		get{return bendingConstraints;}
	}
	public ObiSkinConstraints SkinConstraints{
		get{return skinConstraints;}
	}
	public ObiAerodynamicConstraints AerodynamicConstraints{
		get{return aerodynamicConstraints;}
	}
	public ObiVolumeConstraints VolumeConstraints{
		get{return volumeConstraints;}
	}
	public ObiTetherConstraints TetherConstraints{
		get{return tetherConstraints;}
	}
	public ObiPinConstraints PinConstraints{
		get{return pinConstraints;}
	}

	public bool IsSkinned{
		get{return skinnedMeshRenderer != null;}
	}
	
	public Transform RootBone{
		get{
			if (skinnedMeshRenderer == null) 
				return null;
			return skinnedMeshRenderer.rootBone;
		}
	}

	public Vector3[] MeshVertices{
		get{return meshVertices;}
	}
	public Vector3[] MeshNormals{
		get{return meshNormals;}
	}
	public Vector4[] MeshTangents{
		get{return meshTangents;}
	}

	public override void Awake()
	{

		base.Awake();

		// Grab a copy of the serialized topology reference. This happens when duplicating a cloth.
		if (topology != null)
			topology = GameObject.Instantiate(topology);
		// Or a copy of the shared topology, if there is no serialized reference to a topology.
		else if (sharedTopology != null)
			topology = GameObject.Instantiate(sharedTopology);

		distanceConstraints = GetComponent<ObiDistanceConstraints>();
		bendingConstraints = GetComponent<ObiBendingConstraints>();
		skinConstraints	= GetComponent<ObiSkinConstraints>();
		aerodynamicConstraints = GetComponent<ObiAerodynamicConstraints>();
		volumeConstraints = GetComponent<ObiVolumeConstraints>();
		tetherConstraints = GetComponent<ObiTetherConstraints>();
		pinConstraints = GetComponent<ObiPinConstraints>();
		skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
		
	}

	public override void OnEnable(){

		base.OnEnable();

		if (topology != null){
			topology.GenerateVisualVertexBuffer();
			topology.GenerateReferenceOrientations();
		}

		// Initialize cloth:
		//if (particleRenderer == null){

		if (skinnedMeshRenderer == null)
			InitializeWithRegularMesh();
		else 
			InitializeWithSkinnedMesh();

		//}

		// Enable constraints affecting this cloth:
		distanceConstraints.OnEnable();
		bendingConstraints.OnEnable();
		skinConstraints.OnEnable();
		aerodynamicConstraints.OnEnable();
		volumeConstraints.OnEnable();
		tetherConstraints.OnEnable();
		pinConstraints.OnEnable();

	}
		
	public override void OnDisable(){
		
		base.OnDisable();

		if (meshFilter != null)
			meshFilter.mesh = sharedMesh;
		if (skinnedMeshRenderer != null)
			skinnedMeshRenderer.sharedMesh = sharedMesh;

		GameObject.DestroyImmediate(clothMesh);

		// Disable constraints affecting this cloth:
		distanceConstraints.OnDisable();
		bendingConstraints.OnDisable();
		skinConstraints.OnDisable();
		aerodynamicConstraints.OnDisable();
		volumeConstraints.OnDisable();
		tetherConstraints.OnDisable();
		pinConstraints.OnDisable();

	}

	public void FixedUpdate(){
		fixedUpdateThisFrame = true;
	}

	public override void OnSolverStepEnd(){	

		if (isActiveAndEnabled && tearable)
			ApplyTearing();

	}

	public override void OnSolverFrameBegin(){
		if (OnFrameBegin != null)
			OnFrameBegin(this,null);
	}

	public override void OnSolverFrameEnd(){

		base.OnSolverFrameEnd();
            
		if (solver.IsUpdating)
			CommitResultsToMesh();

		if (OnFrameEnd != null)
			OnFrameEnd(this,null);

		// reset the orientations flag for the next frame. 
		needParticleOrientations = false;

	}
	
	public override void OnDestroy(){

		base.OnDestroy();

		// Destroy our copy of the topology:
		GameObject.DestroyImmediate(topology);

	}

	public override void DestroyRequiredComponents(){
		#if UNITY_EDITOR
			GameObject.DestroyImmediate(distanceConstraints);
			GameObject.DestroyImmediate(bendingConstraints);
			GameObject.DestroyImmediate(skinConstraints);
			GameObject.DestroyImmediate(aerodynamicConstraints);
			GameObject.DestroyImmediate(volumeConstraints);
			GameObject.DestroyImmediate(tetherConstraints);
			GameObject.DestroyImmediate(pinConstraints);
		#endif
	}
		
	/*public void OnDrawGizmos(){

		if (!InSolver) return;
		
		for (int i = 0; i < distanceConstraints.stretching.Count; i++){
			Vector3 p =	solver.renderablePositions[particleIndices[distanceConstraints.springIndices[i*2]]];
			Vector3 p1 = solver.renderablePositions[particleIndices[distanceConstraints.springIndices[i*2+1]]];
			float stretch = solver.distanceConstraints.stretching[i];
			Gizmos.color = Color.yellow;//Color.Lerp(Color.blue,Color.red,stretch*5);
			Gizmos.DrawLine(p,p1);
		}
		
		for (int i = 0; i < bendingConstraints.restBends.Count; i++){
			if (bendingConstraints.activeStatus[i]){
			Vector3 p =	solver.renderablePositions[particleIndices[bendingConstraints.bendingIndices[i*3]]];
			Vector3 p1 = solver.renderablePositions[particleIndices[bendingConstraints.bendingIndices[i*3+1]]];
			Vector3 p2 = solver.renderablePositions[particleIndices[bendingConstraints.bendingIndices[i*3+2]]];
			Gizmos.color = Color.cyan;//Color.Lerp(Color.blue,Color.red,stretch*5);
            Gizmos.DrawLine(p,p1);
			Gizmos.DrawLine(p,p2);
			}
         }
   	}*/
        
    public override bool AddToSolver(object info){

		if (Initialized && base.AddToSolver(info)){
			distanceConstraints.AddToSolver(this);
			bendingConstraints.AddToSolver(this);
			skinConstraints.AddToSolver(this);
			aerodynamicConstraints.AddToSolver(this);
			volumeConstraints.AddToSolver(this);
			tetherConstraints.AddToSolver(this);
			pinConstraints.AddToSolver(this);
			return true;
		}
		return false;
    }

	public override bool RemoveFromSolver(object info){

		bool removed = false;

		try{
			if (distanceConstraints != null)
				distanceConstraints.RemoveFromSolver(null);
			if (bendingConstraints != null)
				bendingConstraints.RemoveFromSolver(null);
			if (skinConstraints != null)
				skinConstraints.RemoveFromSolver(null);
			if (aerodynamicConstraints != null)
				aerodynamicConstraints.RemoveFromSolver(null);
			if (volumeConstraints != null)
				volumeConstraints.RemoveFromSolver(null);
			if (tetherConstraints != null)
				tetherConstraints.RemoveFromSolver(null);
			if (pinConstraints != null)
				pinConstraints.RemoveFromSolver(null);
		}catch(Exception e){
			Debug.LogException(e);
		}finally{
			removed = base.RemoveFromSolver(info);
		}
		return removed;
	}

	public void GetMeshDataArrays(Mesh mesh){
		if (mesh != null)
		{
			meshVertices = mesh.vertices;
			meshNormals = mesh.normals;
			meshTangents = mesh.tangents;
		}
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
		clothMesh = Mesh.Instantiate(meshFilter.sharedMesh) as Mesh;
		clothMesh.MarkDynamic(); 
		GetMeshDataArrays(clothMesh);
		
		// Use the freshly created mesh copy as the renderer mesh and the half-edge input mesh, if it has been already analyzed.
		meshFilter.mesh = clothMesh;

		// If we have no physical representation yet, end here.
		if (!Initialized) 
			return;

		// Update mesh according to simulation state:
		for(int i = 0; i < positions.Length; i++){

			for (int j = 0; j < topology.visualVertexBuffer[i].Count; ++j){
				meshVertices[topology.visualVertexBuffer[i][j]] = positions[i];
			}
			
		}
		
		clothMesh.vertices = meshVertices;
		clothMesh.RecalculateBounds();
		UpdateOrientations();
		UpdateNormalsAndTangents();
		UpdateAerodynamicNormals();
		
	}
	
	private void InitializeWithSkinnedMesh(){
		
		// Store the shared mesh if it hasn't been stored previously.
		if (sharedMesh != null)
			skinnedMeshRenderer.sharedMesh = sharedMesh;
		else
			sharedMesh = skinnedMeshRenderer.sharedMesh;
		
		// Make a deep copy of the original shared mesh.
		clothMesh = Mesh.Instantiate(skinnedMeshRenderer.sharedMesh) as Mesh;
		clothMesh.MarkDynamic();
		GetMeshDataArrays(clothMesh);

	}

	/**
	 * If the provided topology asset is not null, instantiates it and uses the instance
	 * as the current topology.
     */
	private void ResetTopology(){

		if (sharedTopology != null ){
			GameObject.DestroyImmediate(topology);
			topology = GameObject.Instantiate(sharedTopology);
		}

	}

	/**
	 * Generates the particle based physical representation of the cloth mesh. This is the initialization method for the cloth object
	 * and should not be called directly once the object has been created.
	 */
	public IEnumerator GeneratePhysicRepresentationForMesh()
	{		
		initialized = false;
		initializing = false;
		
		if (sharedTopology == null){
			Debug.LogError("No ObiMeshConnectivity provided. Cannot initialize physical representation.");
			yield break;
		}else if (!sharedTopology.Initialized){
			Debug.LogError("The provided ObiMeshConnectivity contains no data. Cannot initialize physical representation.");
            yield break;
		}
		
		initializing = true;

		RemoveFromSolver(null);

		ResetTopology();

		active = new bool[topology.heVertices.Count];
		positions = new Vector3[topology.heVertices.Count];
		velocities = new Vector3[topology.heVertices.Count];
		vorticities = new Vector3[topology.heVertices.Count];
		invMasses  = new float[topology.heVertices.Count];
		solidRadii = new float[topology.heVertices.Count];
		phases = new int[topology.heVertices.Count];
		mass = new float[topology.heVertices.Count];
		areaContribution = new float[topology.heVertices.Count];  

		// Create a particle for each vertex:
		for (int i = 0; i < topology.heVertices.Count; i++){
			
			Oni.Vertex vertex = topology.heVertices[i];

			// Get the particle's area contribution.
			areaContribution[i] = 0;
			foreach (Oni.Face face in topology.GetNeighbourFacesEnumerator(vertex)){
				areaContribution[i] += topology.GetFaceArea(face)/3;
            }
			
			// Get the shortest neighbour edge, particle radius will be half of its length.
			float minEdgeLength = Single.MaxValue;
			foreach (Oni.HalfEdge edge in topology.GetNeighbourEdgesEnumerator(vertex)){
				minEdgeLength = Mathf.Min(minEdgeLength,Vector3.Distance(topology.heVertices[topology.GetHalfEdgeStartVertex(edge)].position,
					                                                     topology.heVertices[edge.endVertex].position));
			}

			active[i] = true;
			mass[i] = 0.05f;
			invMasses[i] = (skinnedMeshRenderer == null && areaContribution[i] > 0) ? (1 / (mass[i] * areaContribution[i])) : 0;
			positions[i] = vertex.position;
			solidRadii[i] = minEdgeLength * 0.5f;
			phases[i] = Oni.MakePhase(gameObject.layer,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
			
			if (i % 100 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: generating particles...",i/(float)topology.heVertices.Count);
		}
		
		List<ObiMeshTopology.HEEdge> edges = topology.GetEdgeList();
		distanceConstraints.Initialize();

		// Initialize constraint-halfedge map for cloth tearing purposes: TODO: reset on awake!!!
		constraintHalfEdgeMap = new int[topology.heHalfEdges.Count];
		for (int i = 0; i < constraintHalfEdgeMap.Length; i++) constraintHalfEdgeMap[i] = -1;

		// Create structural springs: 
		for (int i = 0; i < edges.Count; i++){
		
			constraintHalfEdgeMap[edges[i].halfEdgeIndex] = i;
			Oni.HalfEdge hedge = topology.heHalfEdges[edges[i].halfEdgeIndex];
			Oni.Vertex startVertex = topology.heVertices[topology.GetHalfEdgeStartVertex(hedge)];
			Oni.Vertex endVertex = topology.heVertices[hedge.endVertex];
			
			distanceConstraints.AddConstraint(true,topology.GetHalfEdgeStartVertex(hedge),hedge.endVertex,Vector3.Distance(startVertex.position,endVertex.position),1,1);
			
			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: generating structural constraints...",i/(float)topology.heHalfEdges.Count);
		}
		
		// Create aerodynamic constraints:
		aerodynamicConstraints.Initialize();
		for (int i = 0; i < topology.heVertices.Count; i++){

			aerodynamicConstraints.AddConstraint(true,i,
		                                     	 Vector3.up,
			                                     aerodynamicConstraints.windVector,
			                                     areaContribution[i],
			                                     aerodynamicConstraints.dragCoefficient,
			                                     aerodynamicConstraints.liftCoefficient);
		}

		//Create skin constraints (if needed)
		if (skinnedMeshRenderer != null){

			skinConstraints.Initialize();

			for (int i = 0; i < topology.heVertices.Count; i++){
				skinConstraints.AddConstraint(true,i,topology.heVertices[i].position,meshNormals[i],0.1f,0,1);
			}

		}

		//Create pressure constraints if the mesh is closed:
		if (topology.IsClosed){
			
			volumeConstraints.Initialize();

			int[] triangleIndices = new int[topology.heFaces.Count * 3];
			for (int i = 0; i < topology.heFaces.Count; i++){
				Oni.Face face = topology.heFaces[i];
			
				Oni.HalfEdge e1 = topology.heHalfEdges[face.halfEdge];
				Oni.HalfEdge e2 = topology.heHalfEdges[e1.nextHalfEdge];
				Oni.HalfEdge e3 = topology.heHalfEdges[e2.nextHalfEdge];

				triangleIndices[i*3] = e1.endVertex;
				triangleIndices[i*3+1] = e2.endVertex;
				triangleIndices[i*3+2] = e3.endVertex;
			}

			volumeConstraints.AddConstraint(true,triangleIndices,topology.MeshVolume,1,1);
		}
		
		//Create bending constraints:
		bendingConstraints.Initialize();
		Dictionary<int,int> cons = new Dictionary<int, int>();
		for (int i = 0; i < topology.heVertices.Count; i++){
	
			Oni.Vertex vertex = topology.heVertices[i];
	
			foreach (Oni.Vertex n1 in topology.GetNeighbourVerticesEnumerator(vertex)){
	
				float cosBest = 0;
				Oni.Vertex vBest = n1;
	
				foreach (Oni.Vertex n2 in topology.GetNeighbourVerticesEnumerator(vertex)){
					float cos = Vector3.Dot((n1.position-vertex.position).normalized,
					                        (n2.position-vertex.position).normalized);
					if (cos < cosBest){
						cosBest = cos;
						vBest = n2;
					}
				}
				
				if (!cons.ContainsKey(vBest.index) || cons[vBest.index] != n1.index){
				
					cons[n1.index] = vBest.index;
				
					float[] restPositions = new float[]{n1.position[0],n1.position[1],n1.position[2],
														vBest.position[0],vBest.position[1],vBest.position[2],
														vertex.position[0],vertex.position[1],vertex.position[2]};
					float restBend = Oni.BendingConstraintRest(restPositions);
					bendingConstraints.AddConstraint(true,n1.index,vBest.index,vertex.index,restBend,0,1);
				}
	
			}
	
			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: adding bend constraints...",i/(float)sharedTopology.heVertices.Count);
		}

		// Initialize tether constraints:
		tetherConstraints.Initialize();

		// Initialize pin constraints:
		pinConstraints.Initialize();	

		// Disable volume constraints if we are not going to use them now:
		if (skinnedMeshRenderer != null || !topology.IsClosed){	
			volumeConstraints.enabled = false;
		}

		AddToSolver(null);

		initializing = false;
		initialized = true;

	}

	/**
 	 * Applies changes in physics model to the cloth mesh.
 	 */
	public void CommitResultsToMesh()
	{
		if (!Initialized || particleIndices == null || clothMesh == null) return;
				
		if (skinnedMeshRenderer != null){ //Update skinned mesh:
		
			// Avoid accumulating mesh vertices, normals and tangent transformations if there's been no fixed updates this frame.
			if (!fixedUpdateThisFrame) return;
				fixedUpdateThisFrame = false;

			bool[] isClothVertex = new bool[meshVertices.Length];

			// Transform all particle positions to rootBone space:
			for(int i = 0; i < particleIndices.Count; ++i){
								
				if (active[i]){

					Vector3 position = solver.renderablePositions[particleIndices[i]];
					Oni.Vertex vertex = topology.heVertices[i];
	
					for (int j = 0; j < topology.visualVertexBuffer[i].Count; ++j){
						isClothVertex[topology.visualVertexBuffer[i][j]] = true;
						meshVertices[topology.visualVertexBuffer[i][j]] = skinnedMeshRenderer.rootBone.InverseTransformPoint(position);
					}
				}	
				
			}
			
			Matrix4x4 localToSkin = skinnedMeshRenderer.rootBone.worldToLocalMatrix * transform.localToWorldMatrix;
			
			// Then transform all regular vertices:
			for(int i = 0; i < meshVertices.Length; ++i){
				if (!isClothVertex[i])
					meshVertices[i] = localToSkin.MultiplyPoint3x4(meshVertices[i]);
			}

		}else{ //Update regular mesh:

			for(int i = 0; i < particleIndices.Count; ++i){

				// Transform renderable position from world space to local space:
				Vector3 position = transform.InverseTransformPoint(solver.renderablePositions[particleIndices[i]]);

				for (int j = 0; j < topology.visualVertexBuffer[i].Count; ++j){
					meshVertices[topology.visualVertexBuffer[i][j]] = position;
				}

			}
			
		}
		
		clothMesh.vertices = meshVertices;
		clothMesh.RecalculateBounds();
		UpdateOrientations();
		UpdateNormalsAndTangents();
		UpdateAerodynamicNormals();
		
		// Apply the modified mesh to the SkinnedMeshRenderer.
		if (skinnedMeshRenderer != null){
			skinnedMeshRenderer.sharedMesh = clothMesh;
		}
		
	}
	
	private void UpdateNormalsAndTangents(){
		
		if (skinnedMeshRenderer != null){

			Matrix4x4 localToSkin = skinnedMeshRenderer.rootBone.worldToLocalMatrix * transform.localToWorldMatrix;
			
			// Update normals:
			Vector3[] skinNormals = new Vector3[meshNormals.Length];
			for(int i = 0; i < meshNormals.Length; i++){
				skinNormals[i] = localToSkin.MultiplyVector(meshNormals[i]);
			}
			
			clothMesh.normals = skinNormals;

			if (updateTangentSpace){

				UpdateTangentSpace();

				// Move tangents from local to root bone space (since Unity expects them that way...sigh):
				if (clothMesh.tangents.Length > 0){

					Vector4[] skinTangents = new Vector4[meshTangents.Length];
					for(int i = 0; i < meshTangents.Length; i++){
						skinTangents[i] = localToSkin.MultiplyVector(meshTangents[i]);
						skinTangents[i].w = meshTangents[i].w;
					}
					clothMesh.tangents = skinTangents;

				}

			}
			
		}else{

			// Update normals:
			topology.AreaWeightedNormals(meshVertices, ref meshNormals);
			clothMesh.normals = meshNormals;

			// Update tangent space:
			if (updateTangentSpace)
				UpdateTangentSpace();

		}

	}

	/**
	 * Updates orientations for each particle.
	 *
	 * Algorithm overview: During the offline topology processing, a reference edge (the outgoing half-edge) is selected for each vertex. 
	 * Then, this edge and the normal are used as basis vectors to generate a reference frame, which is then encoded as a quaternion.
	 *
	 * At runtime, this function recomputes the per-vertex reference quaternion using deformed version of the mesh, and generates a delta quaternion
	 * from the difference between the original mesh and the deformed mesh quaternions. This delta orientation can then be used
	 * to update the tangent space of the mesh, or to skin other meshes to it. 
	 *
	 * This is similar to how mesh skinning is performed, but lightweight (uses per-vertex quaternions instead of per-vertex matrices).
	 */
	private void UpdateOrientations(){

		if (!needParticleOrientations && !updateTangentSpace) return;

		orientation = new Quaternion[topology.heVertices.Count];
		GCHandle orientationHandle = Oni.PinMemory(orientation);
		GCHandle verticesHandle = Oni.PinMemory(meshVertices);
		GCHandle normalsHandle = Oni.PinMemory(meshNormals);
		Oni.VertexOrientations(topology.HalfEdgeMesh,verticesHandle.AddrOfPinnedObject(),
													 normalsHandle.AddrOfPinnedObject(),
												     orientationHandle.AddrOfPinnedObject());
		Oni.UnpinMemory(orientationHandle);
		Oni.UnpinMemory(verticesHandle);
		Oni.UnpinMemory(normalsHandle);
	}

	/**
	 * Updates the tangent space for each mesh vertex, using the delta quaternions computed in UpdateOrientations.
	 */
	private void UpdateTangentSpace(){

		// No tangents or particle orientations, do nothing.
		if (clothMesh.tangents.Length == 0 || orientation == null) return;

		Vector4[] tangents = sharedMesh.tangents;

		for(int i = 0; i < topology.heVertices.Count; ++i){

			Quaternion reference = topology.referenceOrientation[i];
			Quaternion current = orientation[i];

			Quaternion delta = current * Quaternion.Inverse(reference);

			float oldw;
			int t = 0;
			for(int j = 0; j < topology.visualVertexBuffer[i].Count; ++j){
				t = topology.visualVertexBuffer[i][j];
				oldw = tangents[t].w;
				tangents[t] = delta * tangents[t];
				tangents[t].w = oldw;
			}

		}

		meshTangents = clothMesh.tangents = tangents;

	}

	private void UpdateAerodynamicNormals(){

		if (!aerodynamicConstraints.enabled) return;

		for (int i = 0; i < topology.heVertices.Count; ++i){
			aerodynamicConstraints.aerodynamicNormals[i] = transform.TransformDirection(meshNormals[topology.visualVertexBuffer[i][0]]);
		}

		aerodynamicConstraints.PushDataToSolver(new ObiSolverData(ObiSolverData.AerodynamicConstraintsData.AERODYNAMIC_NORMALS));
	
	}

	/**
 	* Resets cloth mesh to its original state.
 	*/
	public override void ResetActor(){

		ResetTopology();
		
		//reset particle positions:
		foreach (Oni.Vertex vertex in sharedTopology.heVertices){
			positions[vertex.index] = vertex.position;
            velocities[vertex.index] = Vector3.zero;
        }

		//reset mesh, if any:
		if (clothMesh != null){
			GetMeshDataArrays(sharedMesh);
			clothMesh.vertices = meshVertices;
			clothMesh.RecalculateBounds();
			UpdateOrientations();
			UpdateNormalsAndTangents();
			UpdateAerodynamicNormals();
		}

		PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.POSITIONS | ObiSolverData.ParticleData.VELOCITIES));
		            
	}

	private void ApplyTearing(){
	
		distanceConstraints.PullDataFromSolver(new ObiSolverData(ObiSolverData.DistanceConstraintsData.DISTANCE_STRETCH));

		List<int> tearedEdges = new List<int>();
		for (int i = 0; i < distanceConstraints.stretching.Length; i++){
			if (distanceConstraints.stretching[i] > tearFactor){
				tearedEdges.Add(i);
			}
		}

		if (tearedEdges.Count > 0){

			// Get current particle positions:
			Vector4[] solverPositions = new Vector4[solver.maxParticles];
			Oni.GetParticlePositions(solver.OniSolver,solverPositions,solver.maxParticles,0);

			// Prepare a mesh buffer to manipulate the mesh:
			MeshBuffer buffer = null;
			if (clothMesh != null)
				buffer = new MeshBuffer(clothMesh);

			// Removing this actor's constraints from the solver and then re-adding them at the end might not
			// be the fastest thing in the world, but at least is cache-friendly since it avoids constraint data fragmentation.

			distanceConstraints.RemoveFromSolver(null);
			aerodynamicConstraints.RemoveFromSolver(null); 
			for(int i = 0; i < tearedEdges.Count; i++){
				Tear (tearedEdges[i],solverPositions,buffer);
			}
			distanceConstraints.AddToSolver(this);
			aerodynamicConstraints.AddToSolver(this);

			// Apply changes back to the mesh:
			if (buffer != null)
				buffer.Apply();

			Oni.SetParticlePositions(solver.OniSolver,solverPositions,solver.maxParticles,0);

			GetMeshDataArrays(clothMesh);
		}
		
	}

	/**
	 * In the case of skinned cloth, we must grab the skinned vertex positions before starting the simulation steps for this frame.
	 * If the Animator component is set to Animate Physics, then we'll get up to date data here. Otherwise, we will suffer a 1 frame delay
	 * as the animation and simulation update at the same time (instead of first animation, then simulation which is the optimal way).
	 */
	public override void OnSolverStepBegin(){
		if (skinnedMeshRenderer == null){
			base.OnSolverStepBegin();
		}else{
			GrabSkinAnimation();
		}
	}

	/**
	 * If a Skinned Mesh Renderer is present, grabs all mesh data from the current animation state and transfers it to the particle simulation.
	 * Does nothing if no Skinned Mesh Renderer can be found.
	 */
	public void GrabSkinAnimation(){

		if (skinnedMeshRenderer != null){

			if (!Initialized || clothMesh == null || particleIndices == null) return;
				
			// Grab the skinned vertex positions/normals/tangents from the SkinnedMeshRenderer:
			skinnedMeshRenderer.sharedMesh = sharedMesh;
			skinnedMeshRenderer.BakeMesh(clothMesh);
			GetMeshDataArrays(clothMesh);
				
			/**
			 * BakeMesh returns vertex positions in the renderer's local space. We must 
			 * convert them to world space to feed them to the solver and skin constraints.
			 */
			Vector4[] skinPos = new Vector4[1];
			for(int i = 0; i < particleIndices.Count; i++){

				int vindex = topology.visualVertexBuffer[i][0];

				if (invMasses[i] == 0){
					skinPos[0] = skinnedMeshRenderer.transform.TransformPoint(meshVertices[vindex]);
					Oni.SetParticlePositions(solver.OniSolver,skinPos,1,particleIndices[i]);
				}

				skinConstraints.skinPoints[i] = skinnedMeshRenderer.transform.TransformPoint(meshVertices[vindex]);
				skinConstraints.skinNormals[i] = skinnedMeshRenderer.transform.TransformDirection(meshNormals[vindex]);

			}
			skinConstraints.PushDataToSolver(new ObiSolverData(ObiSolverData.SkinConstraintsData.SKIN_POINTS | ObiSolverData.SkinConstraintsData.SKIN_NORMALS));
		}

	}


	/**
	 * Tears a cloth distance constraint, affecting both the physical representation of the cloth and its mesh.
	 */
	public void Tear(int constraintIndex,Vector4[] solverPositions,MeshBuffer buffer){

		if (topology == null) return;

		// dont allow splitting if there are no free particles left in solver.
		if (solver.allocatedParticles.Count == solver.maxParticles) return;

		// get particle indices at both ends of the constraint:
		int splitIndex = distanceConstraints.springIndices[constraintIndex*2];
		int intactIndex = distanceConstraints.springIndices[constraintIndex*2+1];

		// we will split the particle with higher mass, so swap them if needed.
		if (invMasses[splitIndex] > invMasses[intactIndex]){
			int aux = splitIndex;
			splitIndex = intactIndex;
			intactIndex = aux;
		}

		// Calculate the splitting plane in world space:
		Vector4 v1 = solverPositions[particleIndices[splitIndex]];
		Vector4 v2 = solverPositions[particleIndices[intactIndex]];
		Plane splitPlane = new Plane((v2-v1).normalized,v1);

		Oni.Vertex newVertex;
		HashSet<int> updatedEdges; 
		HashSet<int> newEdges;	

		// Try to split the vertex at that particle:
		if (!topology.SplitVertex(topology.heVertices[splitIndex],splitPlane,buffer,solverPositions,particleIndices,out newVertex,out updatedEdges, out newEdges)){
		
			// If we could not split the higher mass particle, try the other one. If that fails too, we cannot tear this edge.
			int aux = splitIndex;
			splitIndex = intactIndex;
            intactIndex = aux;

			v1 = topology.heVertices[splitIndex].position;
			v2 = topology.heVertices[intactIndex].position;
			splitPlane = new Plane((v2-v1).normalized,v1);
            
			if (!topology.SplitVertex(topology.heVertices[splitIndex],splitPlane,buffer,solverPositions,particleIndices,out newVertex,out updatedEdges, out newEdges))
				return;
				
		}

		// If the split operation has successfully updated mesh topology, update physical representation too.

		// halve the mass and radius of the original particle:
		invMasses[splitIndex] *= 2;
		mass[splitIndex] *= 0.5f;
		solidRadii[splitIndex] *= 0.5f;

		// add a new particle, with the same mass and radius (half of the original).
		List<int> newParticle = solver.AllocateParticles(1);
		particleIndices.AddRange(newParticle);

		// resize particle arrays to hold data for the new particle:
		Array.Resize(ref positions,positions.Length+1);
		Array.Resize(ref velocities,velocities.Length+1);
		Array.Resize(ref invMasses,invMasses.Length+1);
		Array.Resize(ref mass,mass.Length+1);
		Array.Resize(ref solidRadii,solidRadii.Length+1);
		Array.Resize(ref phases,phases.Length+1);
		Array.Resize(ref active,active.Length+1);
		Array.Resize(ref areaContribution, areaContribution.Length+1);

		// copy the new particle data in the actor and solver arrays:
		positions[positions.Length-1] = positions[splitIndex];
		velocities[velocities.Length-1] = velocities[splitIndex];
		active[active.Length-1] = active[splitIndex];
		mass[active.Length-1] = mass[splitIndex];
		
		solverPositions[newParticle[0]] = solverPositions[particleIndices[splitIndex]];
	
		// Copy velocity from original particle.
		Vector4[] velocity = {Vector4.zero};
		Oni.GetParticleVelocities(solver.OniSolver,velocity,1,particleIndices[splitIndex]);
		Oni.SetParticleVelocities(solver.OniSolver,velocity,1,newParticle[0]);

		// Also copy mass, radius and phase.
		invMasses[invMasses.Length-1] = invMasses[splitIndex];
		solidRadii[solidRadii.Length-1] = solidRadii[splitIndex];
		phases[phases.Length-1] = phases[splitIndex];

		Oni.SetParticleInverseMasses(solver.OniSolver,new float[]{invMasses[splitIndex]},1,newParticle[0]);
		Oni.SetParticleSolidRadii(solver.OniSolver,new float[]{solidRadii[splitIndex]},1,newParticle[0]);
		Oni.SetParticlePhases(solver.OniSolver,new int[]{phases[splitIndex]},1,newParticle[0]);
		
		// Copy area contribution (not exactly right, but an approximation):
		areaContribution[newParticle[0]] = areaContribution[areaContribution.Length-1] = areaContribution[splitIndex];

		// relocate the mesh vertex to its new position:
		if (buffer != null){
			buffer.vertices[buffer.vertexCount-1] = positions[splitIndex];
		}

		// create new distance constraints:
		foreach (int halfEdgeIndex in newEdges){

			int pairConstraintIndex = constraintHalfEdgeMap[topology.heHalfEdges[halfEdgeIndex].pair];
			
			// update constraint-edge map:
			constraintHalfEdgeMap[halfEdgeIndex] = distanceConstraints.restLengths.Count;
			
			// add the new constraint:
            distanceConstraints.AddConstraint(true,topology.GetHalfEdgeStartVertex(topology.heHalfEdges[halfEdgeIndex]),
		                                 	   topology.heHalfEdges[halfEdgeIndex].endVertex,
		                                  	   distanceConstraints.restLengths[pairConstraintIndex],
		                                 	   distanceConstraints.stiffnesses[pairConstraintIndex].x,
		                                  	   distanceConstraints.stiffnesses[pairConstraintIndex].y);

		}
		
		// re-wire  existing distance constraints.
		foreach (int halfEdgeIndex in updatedEdges){

			if (constraintHalfEdgeMap[halfEdgeIndex] > -1){
				distanceConstraints.springIndices[constraintHalfEdgeMap[halfEdgeIndex]*2] = topology.GetHalfEdgeStartVertex(topology.heHalfEdges[halfEdgeIndex]);
				distanceConstraints.springIndices[constraintHalfEdgeMap[halfEdgeIndex]*2+1] = topology.heHalfEdges[halfEdgeIndex].endVertex;	
			}

		}

		// Create new aerodynamic constraint:
		aerodynamicConstraints.AddConstraint(true,newParticle[0],
												  Vector3.up,
												  aerodynamicConstraints.windVector,
												  areaContribution[newParticle[0]],
			                                      aerodynamicConstraints.dragCoefficient,
			                                      aerodynamicConstraints.liftCoefficient);

		// Remove tether, bending and aerodynamic constraints affecting the split particle:
		List<int> affectedConstraints = aerodynamicConstraints.GetConstraintsInvolvingParticle(splitIndex);
		foreach (int j in affectedConstraints) aerodynamicConstraints.activeStatus[j] = false;
		
		affectedConstraints = tetherConstraints.GetConstraintsInvolvingParticle(splitIndex);
		foreach (int j in affectedConstraints) tetherConstraints.activeStatus[j] = false;
		
		affectedConstraints = bendingConstraints.GetConstraintsInvolvingParticle(splitIndex);
		foreach (int j in affectedConstraints) bendingConstraints.activeStatus[j] = false;
		
		tetherConstraints.UpdateConstraintActiveStatus();
		bendingConstraints.UpdateConstraintActiveStatus();
		aerodynamicConstraints.UpdateConstraintActiveStatus();

	}
		
	/**
	 * Automatically generates tether constraints for the cloth.
	 * Partitions fixed particles into "islands", then generates up to maxTethers constraints for each 
	 * particle, linking it to the closest point in each island.
	 */
	public override bool GenerateTethers(int maxTethers){
		
		if (!Initialized) return false;
		if (tetherConstraints == null) return false;

		tetherConstraints.Initialize();
		
		if (maxTethers > 0){
			
			List<HashSet<int>> islands = new List<HashSet<int>>();
			
			// Partition fixed particles into islands:
			for (int i = 0; i < topology.heVertices.Count; i++){
				
				Oni.Vertex vertex = topology.heVertices[i];
				if (invMasses[i] > 0) continue;
				
				bool inExistingIsland = false;
					
				// If any of the adjacent particles is in an island, this one is in the same island.
				foreach (Oni.Vertex n in topology.GetNeighbourVerticesEnumerator(vertex)){
                    foreach(HashSet<int> island in islands){
                    	if (island.Contains(n.index)){
							inExistingIsland = true;
                            island.Add(i);
                    		break;
                    	}	
                    }
				}
				
				// If no adjacent particle is in an island, create a new one:
				if (!inExistingIsland){
					islands.Add(new HashSet<int>(){i});
				}
			}	
			
			// Generate tether constraints:
			for (int i = 0; i < invMasses.Length; ++i){
			
				if (invMasses[i] == 0) continue;
				
				List<KeyValuePair<float,int>> tethers = new List<KeyValuePair<float,int>>(islands.Count);
				
				// Find the closest particle in each island, and add it to tethers.
				foreach(HashSet<int> island in islands){
					int closest = -1;
					float minDistance = Mathf.Infinity;
					foreach (int j in island){
						float distance = Vector3.Distance(topology.heVertices[i].position,
							                              topology.heVertices[j].position);
						if (distance < minDistance){
							minDistance = distance;
							closest = j;
						}
					}
					if (closest >= 0)
						tethers.Add(new KeyValuePair<float,int>(minDistance, closest));
				}
				
				// Sort tether indices by distance:
				tethers.Sort(
				delegate(KeyValuePair<float,int> x, KeyValuePair<float,int> y)
				{
					return x.Key.CompareTo(y.Key);
				}
				);
				
				// Create constraints for "maxTethers" closest anchor particles:
				for (int k = 0; k < Mathf.Min(maxTethers,tethers.Count); ++k){
					tetherConstraints.AddConstraint(true,i,tethers[k].Value,tethers[k].Key,
																			tetherConstraints.tetherScale,
																			tetherConstraints.stiffness);
				}
			}
            
        }
        
        return true;
        
	}
		
	/**
	 * Deactivates all fixed particles that are attached to fixed particles only, and all the constraints
	 * affecting them.
	 */
	public void Optimize(){

		// Iterate over all particles and get those fixed ones that are only linked to fixed particles.
		for (int i = 0; i < topology.heVertices.Count; ++i){

			Oni.Vertex vertex = topology.heVertices[i];
			if (invMasses[i] > 0) continue;

			active[i] = false;
			foreach (Oni.Vertex n in topology.GetNeighbourVerticesEnumerator(vertex)){
				
				// If at least one neighbour particle is not fixed, then the particle we are considering for optimization should not be removed.
				if (invMasses[n.index] > 0){
					active[i]  = true;
					break;
				}
				
			}
			
			// Deactivate all constraints involving this inactive particle:
			if (!active[i]){
				List<int> affectedConstraints = distanceConstraints.GetConstraintsInvolvingParticle(i);
				foreach (int j in affectedConstraints) distanceConstraints.activeStatus[j] = false;
				
				affectedConstraints = bendingConstraints.GetConstraintsInvolvingParticle(i);
				foreach (int j in affectedConstraints) bendingConstraints.activeStatus[j] = false;
				
				affectedConstraints = skinConstraints.GetConstraintsInvolvingParticle(i);
				foreach (int j in affectedConstraints) skinConstraints.activeStatus[j] = false;
				
				affectedConstraints = aerodynamicConstraints.GetConstraintsInvolvingParticle(i);
				foreach (int j in affectedConstraints) aerodynamicConstraints.activeStatus[j] = false;
				
				affectedConstraints = volumeConstraints.GetConstraintsInvolvingParticle(i);
				foreach (int j in affectedConstraints) volumeConstraints.activeStatus[j] = false;
				
				affectedConstraints = tetherConstraints.GetConstraintsInvolvingParticle(i);
				foreach (int j in affectedConstraints) tetherConstraints.activeStatus[j] = false;

				affectedConstraints = pinConstraints.GetConstraintsInvolvingParticle(i);
				foreach (int j in affectedConstraints) pinConstraints.activeStatus[j] = false;
			}

		}	
		
		distanceConstraints.UpdateConstraintActiveStatus();
		bendingConstraints.UpdateConstraintActiveStatus();
		skinConstraints.UpdateConstraintActiveStatus();
		aerodynamicConstraints.UpdateConstraintActiveStatus();
		volumeConstraints.UpdateConstraintActiveStatus();
		tetherConstraints.UpdateConstraintActiveStatus();
		pinConstraints.UpdateConstraintActiveStatus();
		PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.ACTIVE_STATUS));
					
	}
	
	/**
	 * Undoes all optimization performed by Optimize(). This means that all particles and constraints in the
	 * cloth are activated again.
	 */
	public void Unoptimize(){
	
		// Activate all particles and constraints (particles first):
		
		for (int i = 0; i < topology.heVertices.Count; ++i)
		 	active[i] = true;
		PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.ACTIVE_STATUS));
		
		for (int i = 0; i < distanceConstraints.activeStatus.Count; ++i) distanceConstraints.activeStatus[i] = true;
		distanceConstraints.UpdateConstraintActiveStatus();
		
		for (int i = 0; i < bendingConstraints.activeStatus.Count; ++i) bendingConstraints.activeStatus[i] = true;
		bendingConstraints.UpdateConstraintActiveStatus();
		
		for (int i = 0; i < skinConstraints.activeStatus.Count; ++i) skinConstraints.activeStatus[i] = true;
		skinConstraints.UpdateConstraintActiveStatus();
		
		for (int i = 0; i < aerodynamicConstraints.activeStatus.Count; ++i) aerodynamicConstraints.activeStatus[i] = true;
		aerodynamicConstraints.UpdateConstraintActiveStatus();
		
		for (int i = 0; i < volumeConstraints.activeStatus.Count; ++i) volumeConstraints.activeStatus[i] = true;
		volumeConstraints.UpdateConstraintActiveStatus();
		
		for (int i = 0; i < tetherConstraints.activeStatus.Count; ++i) tetherConstraints.activeStatus[i] = true;
		tetherConstraints.UpdateConstraintActiveStatus();

		for (int i = 0; i < pinConstraints.activeStatus.Count; ++i) pinConstraints.activeStatus[i] = true;
		pinConstraints.UpdateConstraintActiveStatus();
		
	}

	/**
	 * Reads particle data from a 2D texture. Can be used to adjust per particle mass, skin radius, etc. using 
	 * a texture instead of painting it in the editor. 
	 *	
     * Will call onReadProperty once for each particle, passing the particle index and the bilinearly interpolated 
	 * color of the texture at its coordinate.
	 *
	 * Be aware that, if a particle corresponds to more than
	 * one physical vertex and has multiple uv coordinates, and the value read from
	 * the texture at these coordinates is different, the average value is calculated.
	 */
	public bool ReadParticlePropertyFromTexture(Texture2D source,Action<int,Color> onReadProperty){
		
		if (source == null || clothMesh == null || topology == null || onReadProperty == null)
			return false;

		// Iterate over al vertices in the emsh reading back colors from the texture:
		for (int i = 0; i < topology.heVertices.Count; ++i){
			
			Oni.Vertex vertex = topology.heVertices[i];

			// Read an average of all values associated to this vertex:
			Color color = Color.black;
			int count = 0;
			foreach (int j in topology.visualVertexBuffer[i]){
				Vector2 uv = clothMesh.uv[j];
				count++;
				try{
					color += source.GetPixelBilinear(uv.x, uv.y);
				}catch(UnityException e){	
					Debug.LogException(e);
					return false;
				}
			}
			color /= count;

			onReadProperty(i,color);

		}
		
		return true;
	}

	public override Quaternion GetParticleOrientation(int index){
		if (orientation == null) 
			return topology.referenceOrientation[index];
		return orientation[index];
	}

}
}

