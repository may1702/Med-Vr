using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiClothProxy component. 
	 */

	[CustomEditor(typeof(ObiClothProxy)), CanEditMultipleObjects] 
	public class ObiClothProxyEditor : Editor
	{
	
		ObiClothProxy proxy;
		
		public void OnEnable(){
			proxy = (ObiClothProxy)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();

			proxy.Proxy = EditorGUILayout.ObjectField("Particle Proxy",proxy.Proxy, typeof(ObiCloth), true) as ObiCloth;
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");

			if (GUILayout.Button("Bind")){
				proxy.BindToProxy();
			}
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
			}
			
		}
		
	}

}

