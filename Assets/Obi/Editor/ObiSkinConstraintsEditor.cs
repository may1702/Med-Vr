using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiSkinConstraints component. 
	 */
	
	[CustomEditor(typeof(ObiSkinConstraints)), CanEditMultipleObjects] 
	public class ObiSkinConstraintsEditor : Editor
	{
		
		ObiSkinConstraints constraints;
		
		public void OnEnable(){
			constraints = (ObiSkinConstraints)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
				constraints.PushDataToSolver(new ObiSolverData(ObiSolverData.SkinConstraintsData.ALL));
				
			}
			
		}

		public void OnSceneGUI(){

			if (Event.current.type != EventType.Repaint || !ObiParticleActorEditor.editMode) return;
			
			// Get the particle actor editor to retrieve selected particles:
			ObiParticleActorEditor[] editors = (ObiParticleActorEditor[])Resources.FindObjectsOfTypeAll(typeof(ObiParticleActorEditor));

			// If there's any particle actor editor active, we can show pin constraints:
			if (editors.Length >0)
 			{
		
				// Get the list of pin constraints from the selected particles:
				for (int i = 0; i < constraints.activeStatus.Count; i++){

					if (!constraints.activeStatus[i]) continue;

					int particleIndex = constraints.skinIndices[i];
					
					if (particleIndex >= 0 && particleIndex < ObiParticleActorEditor.selectionStatus.Length && 
						ObiParticleActorEditor.selectionStatus[particleIndex]){

						float radius = constraints.skinRadiiBackstop[i*2];
						float backstop = constraints.skinRadiiBackstop[i*2+1];
						Vector3 point = constraints.GetSkinPosition(i);
						Vector3 normal = constraints.GetSkinNormal(i);

						if (radius > 0){
							Handles.color = Color.red;
							Handles.DrawLine(point,point + normal * backstop);	
							Handles.color = Color.green;
							Handles.DrawLine(point + normal * backstop,point + normal * radius);
						}

					}
				}
			}
			
		}
		
	}
}

