using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiEmitterMaterial assets. 
	 */
	
	[CustomEditor(typeof(ObiEmitterMaterial)), CanEditMultipleObjects] 
	public class ObiEmitterMaterialEditor : Editor
	{
		
		ObiEmitterMaterial material;

		SerializedProperty isFluid;
		SerializedProperty restRadius;
		SerializedProperty randomRadius;
		SerializedProperty smoothingRadius;
		SerializedProperty relaxationFactor;			
		SerializedProperty restDensity;		
		SerializedProperty viscosity;			
		SerializedProperty cohesion;
		SerializedProperty surfaceTension;	

		SerializedProperty buoyancy; 						
		SerializedProperty atmosphericDrag;				
		SerializedProperty atmosphericPressure;				
		SerializedProperty vorticity;	
		
		public void OnEnable(){
			material = (ObiEmitterMaterial)target;

			isFluid = serializedObject.FindProperty("isFluid");
			restRadius = serializedObject.FindProperty("restRadius");
			randomRadius = serializedObject.FindProperty("randomRadius");
			smoothingRadius = serializedObject.FindProperty("smoothingRadius");
			relaxationFactor = serializedObject.FindProperty("relaxationFactor");
			restDensity = serializedObject.FindProperty("restDensity");
			viscosity = serializedObject.FindProperty("viscosity");
			cohesion = serializedObject.FindProperty("cohesion");
			surfaceTension = serializedObject.FindProperty("surfaceTension");

			buoyancy = serializedObject.FindProperty("buoyancy");
			atmosphericDrag = serializedObject.FindProperty("atmosphericDrag");
			atmosphericPressure = serializedObject.FindProperty("atmosphericPressure");
			vorticity = serializedObject.FindProperty("vorticity");
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();
			
			ObiEmitterMaterial.MaterialChanges changes = ObiEmitterMaterial.MaterialChanges.PER_MATERIAL_DATA;			

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(isFluid);
			EditorGUILayout.PropertyField(restRadius);
			if (EditorGUI.EndChangeCheck())
				changes |= ObiEmitterMaterial.MaterialChanges.PER_PARTICLE_DATA;
				

			GUI.enabled = !isFluid.boolValue;
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(randomRadius);
				if (EditorGUI.EndChangeCheck())
					changes |= ObiEmitterMaterial.MaterialChanges.PER_PARTICLE_DATA;
			GUI.enabled = false;

			GUI.enabled = isFluid.boolValue;
				EditorGUILayout.PropertyField(smoothingRadius);
				EditorGUILayout.PropertyField(relaxationFactor);

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(restDensity);
				if (EditorGUI.EndChangeCheck())
					changes |= ObiEmitterMaterial.MaterialChanges.PER_PARTICLE_DATA;

				EditorGUILayout.PropertyField(viscosity);
				EditorGUILayout.PropertyField(cohesion);
				EditorGUILayout.PropertyField(surfaceTension);
				EditorGUILayout.PropertyField(buoyancy);
				EditorGUILayout.PropertyField(atmosphericDrag);
				EditorGUILayout.PropertyField(atmosphericPressure);
				EditorGUILayout.PropertyField(vorticity);
			GUI.enabled = true;

			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();

				material.CommitChanges(changes);
				
			}
			
		}
		
	}
}


