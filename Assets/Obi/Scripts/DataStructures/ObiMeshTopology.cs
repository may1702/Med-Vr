using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{

/**
 * Half-Edge data structure. Used to simplify and accelerate adjacency queries for
 * a triangular mesh. You can check out http://www.flipcode.com/archives/The_Half-Edge_Data_Structure.shtml
 * for more information on the half-edge mesh representation.
 *
 * This particular implementation does not use pointers, in order to benefit from Unity's serialization system.
 * Instead it uses arrays and indices, which makes some operations more cumbersome due to the need of updating
 * indices across the whole structure when removing faces, edges, or vertices.
 */

public class ObiMeshTopology : ScriptableObject
{
	
	public Mesh input = null;
    public Vector3 scale = Vector3.one;
	[HideInInspector] public bool initialized = false;

	//[NonSerialized] public EditorCoroutine generationRoutine = null;

	public class HEEdge{

		public int halfEdgeIndex;		  /**< Index to one of the half-edges in this edge. This is always the lower-index half-edge of the two.*/

		public HEEdge(int halfEdgeIndex){
			this.halfEdgeIndex = halfEdgeIndex;
		}

	}

	IntPtr halfEdgeMesh;										/** half-edge mesh representation used by Oni.*/
	
    [HideInInspector] public List<Oni.Face> heFaces = null;				/**<faces list*/
    [HideInInspector] public List<Oni.HalfEdge> heHalfEdges = null;		/**<half edges list*/
    [HideInInspector] public List<Oni.Vertex> heVertices = null;			/**<vertices list*/
	
	[HideInInspector] public Quaternion[] referenceOrientation; 				/**< Per particle reference orientation.*/
	[HideInInspector][NonSerialized] public List<List<int>> visualVertexBuffer; /**< holds the visual vertices for each half-edge vertex*/

	[HideInInspector][SerializeField] protected Oni.MeshInformation meshInfo;

	private GCHandle facesHandle;
	private	GCHandle verticesHandle;
	private	GCHandle halfEdgesHandle;

	public bool Initialized{
		get{return initialized;}
	}

	public IntPtr HalfEdgeMesh{
		get {return halfEdgeMesh;}
	}

	public Mesh InputMesh{
		set{
			if (value != input){
				initialized = false;
				heFaces = new List<Oni.Face>();
            	heVertices = new List<Oni.Vertex>();
            	heHalfEdges = new List<Oni.HalfEdge>();
				input = value;
			}
		}
		get{return input;}
	}

	/**
	 * Returns volume for a closed mesh (readonly)
	 */
	public float MeshVolume{
		get{return meshInfo.volume;}
	}

	public float MeshArea{
		get{return meshInfo.area;}
	}

	public int BorderEdgeCount{
		get{return meshInfo.borderEdgeCount;}
	}

	public bool IsClosed{
		get{return meshInfo.closed;}
	}

	public bool IsModified{
		get{return false;}
	} 

	public bool IsNonManifold{
		get{return meshInfo.nonManifold;}
	}

    public void OnEnable(){

		halfEdgeMesh = Oni.CreateHalfEdgeMesh();

        if (scale == Vector3.zero)
            scale = Vector3.one;

        // Check integrity after serialization, (re?) initialize if there's data missing.
		if (heFaces == null || heVertices == null || heHalfEdges == null){
			initialized = false;
            heFaces = new List<Oni.Face>();
            heVertices = new List<Oni.Vertex>();
            heHalfEdges = new List<Oni.HalfEdge>();
		}
		else{
			initialized = true;
			facesHandle = Oni.PinMemory(heFaces.ToArray());
			verticesHandle = Oni.PinMemory(heVertices.ToArray());
			halfEdgesHandle = Oni.PinMemory(heHalfEdges.ToArray());
			Oni.SetFaces(halfEdgeMesh,facesHandle.AddrOfPinnedObject(),heFaces.Count);
			Oni.SetVertices(halfEdgeMesh,verticesHandle.AddrOfPinnedObject(),heVertices.Count);
			Oni.SetHalfEdges(halfEdgeMesh,halfEdgesHandle.AddrOfPinnedObject(),heHalfEdges.Count);
			Oni.UnpinMemory(facesHandle);
			Oni.UnpinMemory(halfEdgesHandle);
			Oni.UnpinMemory(verticesHandle);
		}

		GenerateVisualVertexBuffer();

    }

	public void OnDisable(){	
		Oni.DestroyHalfEdgeMesh(halfEdgeMesh);
	}

	public void Generate(){

		initialized = false;

		GCHandle meshVertices = Oni.PinMemory(input.vertices);
		GCHandle meshTriangles = Oni.PinMemory(input.triangles);
		GCHandle scaleHandle = Oni.PinMemory(scale);
		Oni.Generate(halfEdgeMesh,meshVertices.AddrOfPinnedObject(),meshTriangles.AddrOfPinnedObject(),input.vertices.Length,input.triangles.Length,scaleHandle.AddrOfPinnedObject());
		Oni.GetHalfEdgeMeshInfo(halfEdgeMesh,ref meshInfo);
		Oni.UnpinMemory(meshVertices);
		Oni.UnpinMemory(meshTriangles);
		Oni.UnpinMemory(scaleHandle);

		heFaces.Clear();
		heVertices.Clear();
		heHalfEdges.Clear();

		for (int i = 0; i < Oni.GetFaceCount(halfEdgeMesh); ++i)
		heFaces.Add(new Oni.Face());

		for (int i = 0; i < Oni.GetVertexCount(halfEdgeMesh); ++i)
		heVertices.Add(new Oni.Vertex());

		for (int i = 0; i < Oni.GetHalfEdgeCount(halfEdgeMesh); ++i)
		heHalfEdges.Add(new Oni.HalfEdge());

		Oni.Face[] faces = heFaces.ToArray();
		Oni.Vertex[] vertices = heVertices.ToArray();
		Oni.HalfEdge[] halfEdges = heHalfEdges.ToArray();

		facesHandle = Oni.PinMemory(faces);
		verticesHandle = Oni.PinMemory(vertices);
		halfEdgesHandle = Oni.PinMemory(halfEdges);

		Oni.GetFaces(halfEdgeMesh,facesHandle.AddrOfPinnedObject());
		Oni.GetVertices(halfEdgeMesh,verticesHandle.AddrOfPinnedObject());
		Oni.GetHalfEdges(halfEdgeMesh,halfEdgesHandle.AddrOfPinnedObject());

		heFaces = new List<Oni.Face>(faces);
		heVertices = new List<Oni.Vertex>(vertices);
		heHalfEdges = new List<Oni.HalfEdge>(halfEdges);

		Oni.UnpinMemory(facesHandle);
		Oni.UnpinMemory(halfEdgesHandle);
		Oni.UnpinMemory(verticesHandle);

		GenerateReferenceOrientations();

		initialized = true;
	}

	public void GenerateVisualVertexBuffer(){

		visualVertexBuffer = new List<List<int>>(heVertices.Count); 

		foreach(Oni.Vertex vertex in heVertices){

			HashSet<int> set = new HashSet<int>();

			Oni.HalfEdge startEdge = heHalfEdges[vertex.halfEdge];
			Oni.HalfEdge edge = startEdge;
		
			do{
				edge = heHalfEdges[edge.pair];
				switch(edge.indexInFace){
					case 0: set.Add(heFaces[edge.face].visualVertex1);break;
					case 1: set.Add(heFaces[edge.face].visualVertex2);break;
					case 2: set.Add(heFaces[edge.face].visualVertex3);break;
				}
				edge = heHalfEdges[edge.nextHalfEdge];

			} while (edge.index != startEdge.index);

			visualVertexBuffer.Add(new List<int>(set));
		}

	}

	public void GenerateReferenceOrientations(){
		referenceOrientation = new Quaternion[heVertices.Count];
		GCHandle refOrientationHandle = Oni.PinMemory(referenceOrientation);
		Vector3[] normals = input.normals;
		GCHandle normalsHandle = Oni.PinMemory(normals);
		Oni.VertexOrientations(halfEdgeMesh,IntPtr.Zero,normalsHandle.AddrOfPinnedObject(),
														refOrientationHandle.AddrOfPinnedObject());
		Oni.UnpinMemory(refOrientationHandle);
		Oni.UnpinMemory(normalsHandle);
	}
		 
	/**
	 * Analyzes the input mesh and populates the half-edge structure. Can be called as many times you want (for examples if the original mesh is modified).
	 */
	/*public IEnumerator Generate(){

		initialized = false;

		heFaces.Clear();
		heVertices.Clear();
		heHalfEdges.Clear();

		vertexOrientation.Clear();

		_area = 0;
		_volume = 0;
		_modified = false;
		_nonManifold = false;

		bool nonManifoldEdges = false;

		if (input != null){

			Dictionary<Vector3, HEVertex> vertexBuffer = new Dictionary<Vector3, HEVertex>();
			Dictionary<KeyValuePair<int,int>,HEHalfEdge> edgeBuffer = new Dictionary<KeyValuePair<int,int>,HEHalfEdge>();
			
			// Get copies of vertex and triangle buffers:
			Vector3[] vertices = input.vertices;
			int[] triangles = input.triangles;
			Vector3[] normals = input.normals;

			// first, create vertices:
			for(int i = 0; i < vertices.Length; i++){

				//if the vertex already exists, add physical vertex index to it.
				HEVertex vertex;
				if (vertexBuffer.TryGetValue(vertices[i], out vertex)){
					vertex.physicalIDs.Add(i);
				}else{
					vertex = new HEVertex(Vector3.Scale(vertices[i],scale),i);
				}

				vertexBuffer[vertices[i]] = vertex;

				if (i % 200 == 0)
					yield return new CoroutineJob.ProgressInfo("Half-edge: analyzing vertices...",i/(float)vertices.Length);
			}
			
			// assign unique indices to vertices:
			int index = 0;
			foreach(KeyValuePair<Vector3,HEVertex> pair in vertexBuffer){
				((HEVertex)pair.Value).index = index;
				heVertices.Add(pair.Value);
				if (index % 200 == 0)
					yield return new CoroutineJob.ProgressInfo("Half-edge: assigning indices...",index/(float)vertices.Length);
				index++;
			}
			
			// build half edge structure:
			for(int i = 0; i<triangles.Length;i+=3){

				Vector3 pos1 = vertices[triangles[i]];
				Vector3 pos2 = vertices[triangles[i+1]];
				Vector3 pos3 = vertices[triangles[i+2]];

				HEVertex v1 = vertexBuffer[pos1];
				HEVertex v2 = vertexBuffer[pos2];
				HEVertex v3 = vertexBuffer[pos3];

                pos1.Scale(scale);
                pos2.Scale(scale);
                pos3.Scale(scale);

				// create half edges:
				HEHalfEdge e1 = new HEHalfEdge();
				e1.index = heHalfEdges.Count;
				e1.indexOnFace = 0;
				e1.faceIndex = heFaces.Count;
				e1.endVertex = v1.index;
				e1.startVertex = v2.index;
				
				HEHalfEdge e2 = new HEHalfEdge();
				e2.index = heHalfEdges.Count+1;
				e2.indexOnFace = 1;
				e2.faceIndex = heFaces.Count;
				e2.endVertex = v2.index;
				e2.startVertex = v3.index;
				
				HEHalfEdge e3 = new HEHalfEdge();
				e3.index = heHalfEdges.Count+2;
				e3.indexOnFace = 2;
				e3.faceIndex = heFaces.Count;
				e3.endVertex = v3.index;
				e3.startVertex = v1.index;

				// link half edges together:
				e1.nextEdgeIndex = e3.index;
				e2.nextEdgeIndex = e1.index;
				e3.nextEdgeIndex = e2.index;

				// vertex outgoing half edge indices:
				v1.halfEdgeIndex = e3.index;
				v2.halfEdgeIndex = e1.index;
				v3.halfEdgeIndex = e2.index;

				KeyValuePair<int,int> e1Key = new KeyValuePair<int,int>(v1.index,v2.index);
				KeyValuePair<int,int> e2Key = new KeyValuePair<int,int>(v2.index,v3.index);
				KeyValuePair<int,int> e3Key = new KeyValuePair<int,int>(v3.index,v1.index);

				// Check if vertex winding order is consistent with existing triangles. If not, ignore this one.
				if (edgeBuffer.ContainsKey(e1Key) || edgeBuffer.ContainsKey(e2Key) || edgeBuffer.ContainsKey(e3Key))
				{
					nonManifoldEdges = true;
					continue;
				}else{
					edgeBuffer.Add(e1Key,e1);
					edgeBuffer.Add(e2Key,e2);
					edgeBuffer.Add(e3Key,e3);
				}

				// add edges:
				heHalfEdges.Add(e1);
				heHalfEdges.Add(e2);
				heHalfEdges.Add(e3);
				
				// populate and add face:
				HEFace face = new HEFace();
				face.edges[0] = e1.index;
				face.edges[1] = e2.index;
				face.edges[2] = e3.index;
				face.area = ObiUtils.TriangleArea(pos1,pos2,pos3);
				_area += face.area;
				_volume += Vector3.Dot(Vector3.Cross(pos1,pos2),pos3)/6f;
				face.index = heFaces.Count;
				heFaces.Add(face);

				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("Half-edge: generating edges and faces...",i/(float)triangles.Length);

			}
			
			// Calculate average/min/max edge length:
			_avgEdgeLength = 0;
			_minEdgeLength = float.MaxValue;
			_maxEdgeLength = float.MinValue;
			for (int i = 0; i < heHalfEdges.Count; i++){
				float edgeLength = Vector3.Distance(heVertices[heHalfEdges[i].startVertex].position,
				                                    heVertices[heHalfEdges[i].endVertex].position);
				_avgEdgeLength += edgeLength;
				_minEdgeLength = Mathf.Min(_minEdgeLength,edgeLength);
				_maxEdgeLength = Mathf.Max(_maxEdgeLength,edgeLength);
			}
			_avgEdgeLength /= heHalfEdges.Count;

			List<HEHalfEdge> borderEdges = new List<HEHalfEdge>();		//edges belonging to a mesh border.
			
			// stitch half edge pairs together:
			index = 0;
			foreach(KeyValuePair<KeyValuePair<int,int>,HEHalfEdge> pair in edgeBuffer){

				KeyValuePair<int,int> edgeKey = new KeyValuePair<int,int>(pair.Key.Value,pair.Key.Key);

				HEHalfEdge edge = null;
				if (edgeBuffer.TryGetValue(edgeKey, out edge)){
					((HEHalfEdge)pair.Value).pair = edge.index;
				}else{

					//create border edge:
					HEHalfEdge e = new HEHalfEdge();
					e.index = heHalfEdges.Count;
					e.endVertex = ((HEHalfEdge)pair.Value).startVertex;
					e.startVertex = ((HEHalfEdge)pair.Value).endVertex;
					heVertices[e.startVertex].halfEdgeIndex = e.index;
					e.pair = ((HEHalfEdge)pair.Value).index;
					((HEHalfEdge)pair.Value).pair = e.index;
					heHalfEdges.Add(e);

					borderEdges.Add(e);
				}

				if (index % 1000 == 0)
					yield return new CoroutineJob.ProgressInfo("Half-edge: stitching half-edges...",index/(float)edgeBuffer.Count);
				
				index++;

			}

			_closed = (borderEdges.Count == 0);
			_borderEdgeCount = borderEdges.Count;

			// link together border edges:
			foreach(HEHalfEdge edge in borderEdges){
				edge.nextEdgeIndex = heVertices[edge.endVertex].halfEdgeIndex;
			}

			if (nonManifoldEdges)
				Debug.LogWarning("Non-manifold edges found (vertex winding is not consistent, and/or there are more than 2 faces sharing an edge). Affected faces/edges will be ignored.");

			_nonManifold = nonManifoldEdges;

			// Calculate vertex orientations:
			for(int i = 0; i < heVertices.Count; i++){
				vertexOrientation.Add(CalculateVertexOrientation(i,normals, vertices));
			}

			initialized = true;

		}else{
			Debug.LogWarning("Tried to generate adjacency info for an empty mesh.");
		}
		
	}*/

	/*public bool AreLinked(Oni.Vertex v1, Oni.Vertex v2){
		
		Oni.HalfEdge startEdge = heHalfEdges[v1.halfEdge];
		Oni.HalfEdge edge = startEdge;
		
		do{
			edge = heHalfEdges[edge.pair];
			if (edge.startVertex == v2.index)
				return true;
			edge = heHalfEdges[edge.nextHalfEdge];
			
		} while (edge != startEdge);

		return false;
	}*/

	public int GetHalfEdgeStartVertex(Oni.HalfEdge edge){

		// In a border edge, get the ending vertex of the pair edge:
		if (edge.face == -1)
			return  heHalfEdges[edge.pair].endVertex;

		// In case of an interior edge, find the vertex by going around the face:
		return heHalfEdges[heHalfEdges[edge.nextHalfEdge].nextHalfEdge].endVertex;
	}

	public float GetFaceArea(Oni.Face face){

		Oni.HalfEdge e1 = heHalfEdges[face.halfEdge];
		Oni.HalfEdge e2 = heHalfEdges[e1.nextHalfEdge];
		Oni.HalfEdge e3 = heHalfEdges[e2.nextHalfEdge];

		return Vector3.Cross(heVertices[e2.endVertex].position-heVertices[e1.endVertex].position,
					  		 heVertices[e3.endVertex].position-heVertices[e1.endVertex].position).magnitude / 2.0f;
	}

	public IEnumerable<Oni.Vertex> GetNeighbourVerticesEnumerator(Oni.Vertex vertex)
	{
		
		Oni.HalfEdge startEdge = heHalfEdges[vertex.halfEdge];
		Oni.HalfEdge edge = startEdge;
		
		do{
			yield return heVertices[edge.endVertex];
			edge = heHalfEdges[edge.pair];
			edge = heHalfEdges[edge.nextHalfEdge];
			
		} while (edge.index != startEdge.index);
		
	}

	public IEnumerable<Oni.HalfEdge> GetNeighbourEdgesEnumerator(Oni.Vertex vertex)
	{
		
		Oni.HalfEdge startEdge = heHalfEdges[vertex.halfEdge];
		Oni.HalfEdge edge = startEdge;
		
		do{
			edge = heHalfEdges[edge.pair];
			yield return edge;
			edge = heHalfEdges[edge.nextHalfEdge];
			yield return edge;
			
		} while (edge.index != startEdge.index);
		
	}

	public IEnumerable<Oni.Face> GetNeighbourFacesEnumerator(Oni.Vertex vertex)
	{

		Oni.HalfEdge startEdge = heHalfEdges[vertex.halfEdge];
		Oni.HalfEdge edge = startEdge;

		do{

			edge = heHalfEdges[edge.pair];
			if (edge.face > -1)
				yield return heFaces[edge.face];
			edge = heHalfEdges[edge.nextHalfEdge];

		} while (edge.index != startEdge.index);

	}

	public int[] GetFaceEdges(Oni.Face face){

		Oni.HalfEdge e1 = heHalfEdges[face.halfEdge];
		Oni.HalfEdge e2 = heHalfEdges[e1.nextHalfEdge];
		Oni.HalfEdge e3 = heHalfEdges[e2.nextHalfEdge];

		return new int[]{
			e1.index,e2.index,e3.index
		};
	}

	/**
	 * Calculates and returns a list of all edges (note: not half-edges, but regular edges) in the mesh.
	 * This is O(2N) in both time and space, with N = number of edges.
	 */
	public List<HEEdge> GetEdgeList(){

		List<HEEdge> edges = new List<HEEdge>();
		bool[] listed = new bool[heHalfEdges.Count];

		for (int i = 0; i < heHalfEdges.Count; i++)
		{
			if (!listed[heHalfEdges[i].pair])
			{
				edges.Add(new HEEdge(i));
				listed[heHalfEdges[i].pair] = true;
				listed[i] = true;
			}
		}

		return edges;
	}

	public void AreaWeightedNormals(Vector3[] vertices,ref Vector3[] normals){

		if (!initialized) return;

		GCHandle normalsHandle = Oni.PinMemory(normals);
		GCHandle verticesHandle = Oni.PinMemory(vertices);
		
		Oni.AreaWeightedNormals(halfEdgeMesh,verticesHandle.AddrOfPinnedObject(),normalsHandle.AddrOfPinnedObject());

		Oni.UnpinMemory(normalsHandle);
		Oni.UnpinMemory(verticesHandle);

	}

	/**
	 * Calculates angle-weighted normals for the input mesh, taking into account shared vertices.
	 */
	/*public Vector3[] AngleWeightedNormals(){
		
		if (input == null) return null;

		Vector3[] normals = input.normals;
		Vector3[] vertices = input.vertices;

		for(int i = 0; i < normals.Length; i++)
			normals[i] = Vector3.zero;

		int i1,i2,i3;
		Vector3 e1, e2;
		foreach(HEFace face in heFaces){
			
			HEVertex hv1 = heVertices[heHalfEdges[face.edges[0]].endVertex];
			HEVertex hv2 = heVertices[heHalfEdges[face.edges[1]].endVertex];
			HEVertex hv3 = heVertices[heHalfEdges[face.edges[2]].endVertex];

			i1 = hv1.physicalIDs[0];
			i2 = hv2.physicalIDs[0];
			i3 = hv3.physicalIDs[0];
			
			e1 = vertices[i2]-vertices[i1];
			e2 = vertices[i3]-vertices[i1];
			foreach(int pi in hv1.physicalIDs)
				normals[pi] += Vector3.Cross(e1,e2) * Mathf.Acos(Vector3.Dot(e1.normalized,e2.normalized));
			
			e1 = vertices[i3]-vertices[i2];
			e2 = vertices[i1]-vertices[i2];
			foreach(int pi in hv2.physicalIDs)
				normals[pi] += Vector3.Cross(e1,e2) * Mathf.Acos(Vector3.Dot(e1.normalized,e2.normalized));
			
			e1 = vertices[i1]-vertices[i3];
			e2 = vertices[i2]-vertices[i3];
			foreach(int pi in hv3.physicalIDs)
				normals[pi] += Vector3.Cross(e1,e2) * Mathf.Acos(Vector3.Dot(e1.normalized,e2.normalized));
			
		}

		for(int i = 0; i < normals.Length; i++)
			normals[i].Normalize();
		
		return normals;
	}*/

	/**
	 * Splits a vertex in two along a plane. Returns true if the vertex can be split, false otherwise.
	 * \param vertex the vertex to split.
     * \param splitPlane plane to split the vertex at.
     * \param newVertex the newly created vertex after the split operation has been performed.
     * \param vertices new mesh vertices list after the split operation.
     * \param updatedEdges indices of half-edges that need some kind of constraint update.
	 */
	public bool SplitVertex(Oni.Vertex vertex, Plane splitPlane, MeshBuffer meshBuffer, Vector4[] positions, List<int> particleIndices, out Oni.Vertex newVertex, out HashSet<int> updatedEdges, out HashSet<int> addedEdges){

		// initialize return values:
		updatedEdges = new HashSet<int>();
		addedEdges = new HashSet<int>();
		newVertex = new Oni.Vertex();
		
		// initialize face lists for each side of the split plane.
		List<Oni.Face> side1Faces = new List<Oni.Face>();
		List<Oni.Face> side2Faces = new List<Oni.Face>();
		HashSet<int> side2Edges = new HashSet<int>();
		
		// classify adjacent faces depending on which side of the cut plane they reside in:
		foreach(Oni.Face face in GetNeighbourFacesEnumerator(vertex)){

			Oni.HalfEdge e1 = heHalfEdges[face.halfEdge];
			Oni.HalfEdge e2 = heHalfEdges[e1.nextHalfEdge];
			Oni.HalfEdge e3 = heHalfEdges[e2.nextHalfEdge];
			
			// Skip this face if it doesnt contain the splitted vertex. 
			// This can happen because edge pair links are not updated, and so a vertex in the cut stil "sees"
			// the faces at the other side like neighbour faces.
			if (e1.endVertex != vertex.index && e2.endVertex != vertex.index && e3.endVertex != vertex.index) continue;
			
			// Average positions to get the center of the face:
			Vector3 faceCenter = (positions[particleIndices[e1.endVertex]] +
			                      positions[particleIndices[e2.endVertex]] +
			                      positions[particleIndices[e3.endVertex]]) / 3.0f;
			
			if (splitPlane.GetSide(faceCenter)){
				side1Faces.Add(face);
			}else{
				side2Faces.Add(face);
				side2Edges.Add(e1.index);
				side2Edges.Add(e2.index);
				side2Edges.Add(e3.index);
			}
		}
		
		// If the vertex cant be split, return false.
		if (side1Faces.Count == 0 || side2Faces.Count == 0) return false;
		
		// create a new vertex:
		newVertex = new Oni.Vertex(vertex.position,heVertices.Count,vertex.halfEdge);

		// add a new vertex to the mesh too, if needed.
		if (meshBuffer != null){
			visualVertexBuffer.Add(new List<int>(){meshBuffer.vertexCount});
			meshBuffer.AddVertex(visualVertexBuffer[vertex.index][0]);
		}
		
		// rearrange edges at side 1:
		foreach(Oni.Face face in side1Faces){ 
			
			// find half edges that start or end at the split vertex:
			int[] faceEdges = GetFaceEdges(face);
			Oni.HalfEdge edgeIn = heHalfEdges[Array.Find<int>(faceEdges,e => heHalfEdges[e].endVertex == vertex.index)];
			Oni.HalfEdge edgeOut = heHalfEdges[Array.Find<int>(faceEdges,e => this.GetHalfEdgeStartVertex(heHalfEdges[e]) == vertex.index)];

			// Edges whose pair is on the other side of the cut and share the same vertices, will spawn a new constraint.
			if (side2Edges.Contains(edgeIn.pair) && GetHalfEdgeStartVertex(edgeIn) == heHalfEdges[edgeIn.pair].endVertex){
				addedEdges.Add(Mathf.Max(edgeIn.index,edgeIn.pair));
			}

			if (side2Edges.Contains(edgeOut.pair) && GetHalfEdgeStartVertex(heHalfEdges[edgeOut.pair]) == edgeOut.endVertex){
				addedEdges.Add(Mathf.Max(edgeOut.index,edgeOut.pair));
			}

			// Constraints for these edges should be updated. (There's no guarantee the constraint exists!).
			updatedEdges.Add(edgeIn.index);
			updatedEdges.Add(edgeIn.pair);
			updatedEdges.Add(edgeOut.index);
			updatedEdges.Add(edgeOut.pair);
			
			// stitch in half edge to new vertex
			edgeIn.endVertex = newVertex.index;
			newVertex.halfEdge = edgeOut.index;

			heHalfEdges[edgeIn.index] = edgeIn;
			heHalfEdges[edgeOut.index] = edgeOut;
            
            // update mesh triangle buffer to point at new vertex where needed:
			if (meshBuffer != null){
				if (meshBuffer.triangles[face.index*3] == visualVertexBuffer[vertex.index][0]) meshBuffer.triangles[face.index*3] = meshBuffer.vertexCount-1;
				if (meshBuffer.triangles[face.index*3+1] == visualVertexBuffer[vertex.index][0]) meshBuffer.triangles[face.index*3+1] = meshBuffer.vertexCount-1;
				if (meshBuffer.triangles[face.index*3+2] == visualVertexBuffer[vertex.index][0]) meshBuffer.triangles[face.index*3+2] = meshBuffer.vertexCount-1;
			}
            
        }

		// Add the nex vertex to the half-edge.
		heVertices.Add(newVertex);

		meshInfo.closed = false;
        
        return true;
        
    }

}
}


