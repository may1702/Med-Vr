using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Obi{
	
	/**
 * Custom inspector for ObiCloth components.
 * Allows particle selection and constraint edition. 
 * 
 * Selection:
 * 
 * - To select a particle, left-click on it. 
 * - You can select multiple particles by holding shift while clicking.
 * - To deselect all particles, click anywhere on the object except a particle.
 * 
 * Constraints:
 * 
 * - To edit particle constraints, select the particles you wish to edit.
 * - Constraints affecting any of the selected particles will appear in the inspector.
 * - To add a new pin constraint to the selected particle(s), click on "Add Pin Constraint".
 * 
 */
	[CustomEditor(typeof(ObiCloth)), CanEditMultipleObjects] 
	public class ObiClothEditor : ObiParticleActorEditor
	{
		
		[MenuItem("Component/Physics/Obi/Obi Cloth",false,0)]
		static void AddObiCloth()
		{
			foreach(Transform t in Selection.transforms)
				Undo.AddComponent<ObiCloth>(t.gameObject);
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Cloth",false,2)]
		static void CreateObiCloth()
		{
			GameObject c = new GameObject("Obi Cloth");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Cloth");
			c.AddComponent<ObiCloth>();
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Cloth (with solver)",false,3)]
		static void CreateObiClothWithSolver()
		{
			GameObject c = new GameObject("Obi Cloth");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Cloth");
			ObiCloth cloth = c.AddComponent<ObiCloth>();
			ObiSolver solver = c.AddComponent<ObiSolver>();
			ObiColliderGroup group = c.AddComponent<ObiColliderGroup>();
			cloth.Solver = solver;
			solver.colliderGroup = group;
		}

		public enum TextureChannel{
			RED = 0,
			GREEN = 1,
			BLUE = 2,
			ALPHA = 3,
		}
		
		ObiCloth cloth;
		Texture2D propertyTexture;
		TextureChannel textureChannel;
		
		public override void OnEnable(){
			base.OnEnable();
			cloth = (ObiCloth)target;
		}
		
		public override void OnDisable(){
			base.OnDisable();
			EditorUtility.ClearProgressBar();
		}

		public override void UpdateParticleEditorInformation(){
			
			for(int i = 0; i < cloth.positions.Length; i++)
			{
				wsPositions[i] = cloth.GetParticlePosition(i);		
			}

			if (cloth.clothMesh != null){
				for(int i = 0; i < cloth.positions.Length; i++)
				{
					facingCamera[i] = IsParticleFacingCamera(Camera.current, i);
				}
			}
			
		}

		public bool IsParticleFacingCamera(Camera cam, int particleIndex){
			
			if (cam == null) return false;
			
			Oni.Vertex vertex = cloth.topology.heVertices[particleIndex];
			Vector3 camToParticle = cam.transform.position - wsPositions[particleIndex];

			foreach(int index in cloth.topology.visualVertexBuffer[particleIndex]){
				if (Vector3.Dot(cloth.transform.TransformVector(cloth.MeshNormals[index]),camToParticle) > 0)
					return true;
			}

			return false;

		}
		
		protected override void SetPropertyValue(ParticleProperty property,int index, float value){
			
			switch(property){
			case ParticleProperty.MASS: 
				cloth.mass[index] = value;
				float areaMass = cloth.mass[index] * cloth.areaContribution[index];
				if (areaMass > 0){
					cloth.invMasses[index] = 1 / areaMass;
				}else{
					cloth.invMasses[index] = 0;
				}
				break; 
			case ParticleProperty.RADIUS:{
					cloth.solidRadii[index] = value;
				}break;
			case ParticleProperty.SKIN_BACKSTOP: 
				cloth.SkinConstraints.skinRadiiBackstop[index*2+1] = value;
				break;
			case ParticleProperty.SKIN_RADIUS: 
				cloth.SkinConstraints.skinRadiiBackstop[index*2] = value;
				break;
			}
			
		}
		
		protected override float GetPropertyValue(ParticleProperty property, int index){
			switch(property){
			case ParticleProperty.MASS:
				return cloth.mass[index];
			case ParticleProperty.RADIUS:
				return cloth.solidRadii[index];
			case ParticleProperty.SKIN_BACKSTOP: 
				return cloth.SkinConstraints.skinRadiiBackstop[index*2+1];
			case ParticleProperty.SKIN_RADIUS: 
				return cloth.SkinConstraints.skinRadiiBackstop[index*2];
			}
			return 0;
		}

		protected override void UpdatePropertyInSolver(){

			base.UpdatePropertyInSolver();

			switch(currentProperty){
				case ParticleProperty.SKIN_BACKSTOP:
				case ParticleProperty.SKIN_RADIUS:
				 	cloth.SkinConstraints.PushDataToSolver(new ObiSolverData(ObiSolverData.SkinConstraintsData.SKIN_RADII_BACKSTOP));
				break;
			}

		}

		protected override void DrawCustomUI(){

			float oldLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 40;
			propertyTexture = (Texture2D)EditorGUILayout.ObjectField("Source",propertyTexture, typeof(Texture2D),false);
			EditorGUIUtility.labelWidth = oldLabelWidth;	

			selectionMask = GUILayout.Toggle(selectionMask,"Selection mask");

			GUILayout.BeginHorizontal();
			GUILayout.Label("Min value");
			GUILayout.FlexibleSpace();
			minBrushValue = EditorGUILayout.FloatField(minBrushValue,GUILayout.Width(EditorGUIUtility.fieldWidth));
			GUILayout.EndHorizontal();
		
			GUILayout.BeginHorizontal();
			GUILayout.Label("Max value");
			GUILayout.FlexibleSpace();
			maxBrushValue = EditorGUILayout.FloatField(maxBrushValue,GUILayout.Width(EditorGUIUtility.fieldWidth));
			GUILayout.EndHorizontal();	

			textureChannel = (TextureChannel) EditorGUILayout.EnumPopup(textureChannel,GUI.skin.FindStyle("DropDown"));		

			if (GUILayout.Button("Load "+GetPropertyName()+" from "+textureChannel)){
				Undo.RecordObject(cloth, "Load oarticle property");
				if (!cloth.ReadParticlePropertyFromTexture(propertyTexture,(int i,Color color) =>{
					if (!selectionMask || selectionStatus[i])
						SetPropertyValue(currentProperty,i,minBrushValue + color[(int)textureChannel] * (maxBrushValue - minBrushValue));
					})){
					EditorUtility.DisplayDialog("Invalid texture","The texture is either null or not readable.","Ok");
				}
				ParticlePropertyChanged();
				EditorUtility.SetDirty(cloth);
			}
			
		}

		protected override string CustomUIName(){
			return "Texture";
		}

		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();

			GUI.enabled = cloth.Initialized;
			EditorGUI.BeginChangeCheck();
			editMode = GUILayout.Toggle(editMode,new GUIContent("Edit particles",EditorGUIUtility.Load("EditParticles.psd") as Texture2D),"LargeButton");
			if (EditorGUI.EndChangeCheck()){
				SceneView.RepaintAll();
			}		
			GUI.enabled = true;	

			EditorGUILayout.LabelField("Status: "+ (cloth.Initialized ? "Initialized":"Not initialized"));

			GUI.enabled = (cloth.sharedTopology != null);
			if (GUILayout.Button("Initialize")){
				if (!cloth.Initialized){
					CoroutineJob job = new CoroutineJob();
					routine = EditorCoroutine.StartCoroutine(job.Start(cloth.GeneratePhysicRepresentationForMesh()));
				}else{
					if (EditorUtility.DisplayDialog("Actor initialization","Are you sure you want to re-initialize this actor?","Ok","Cancel")){
						CoroutineJob job = new CoroutineJob();
						routine = EditorCoroutine.StartCoroutine(job.Start(cloth.GeneratePhysicRepresentationForMesh()));
					}
				}
			}
			GUI.enabled = true;

			if (cloth.sharedTopology == null){
				EditorGUILayout.HelpBox("No ObiMeshTopology asset present.",MessageType.Info);
			}

			GUI.enabled = cloth.Initialized;
			if (GUILayout.Button("Set Rest State")){
				cloth.PullDataFromSolver(new ObiSolverData(ObiSolverData.ParticleData.POSITIONS | ObiSolverData.ParticleData.VELOCITIES));
			}
			
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Optimize")){
				Undo.RecordObject(cloth, "Optimize");
				cloth.Optimize();
				EditorUtility.SetDirty(cloth);
			}
			if (GUILayout.Button("Unoptimize")){
				Undo.RecordObject(cloth, "Unoptimize");
				cloth.Unoptimize();
				EditorUtility.SetDirty(cloth);
			}
			GUILayout.EndHorizontal();
			GUI.enabled = true;

			cloth.Solver = EditorGUILayout.ObjectField("Solver",cloth.Solver, typeof(ObiSolver), true) as ObiSolver;

			bool newSelfCollisions = EditorGUILayout.Toggle(new GUIContent("Self collisions","Enabling this allows particles generated by this actor to interact with each other."),cloth.SelfCollisions);
			if (cloth.SelfCollisions != newSelfCollisions){
				Undo.RecordObject(cloth, "Set self collisions");
				cloth.SelfCollisions = newSelfCollisions;
			}

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");

			// Progress bar:
			EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",routine);
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
			
		}
		
	}
}


