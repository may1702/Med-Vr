using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
 	* Holds information about skin constraints for an actor.
 	*/
	[DisallowMultipleComponent]
	public class ObiSkinConstraints : ObiConstraints 
	{
		
		[Range(0,1)]
		[Tooltip("Skin constraints stiffness.")]
		public float stiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/
		
		[HideInInspector] public List<int> skinIndices = new List<int>();						/**< Distance constraint indices.*/
		[HideInInspector] public List<Vector4> skinPoints = new List<Vector4>();				/**< Skin constraint anchor points, in world space.*/
		[HideInInspector] public List<Vector4> skinNormals = new List<Vector4>();				/**< Rest distances.*/
		[HideInInspector] public List<float> skinRadiiBackstop = new List<float>();				/**< Rest distances.*/
		[HideInInspector] public List<float> skinStiffnesses = new List<float>();				/**< Stiffnesses of distance constraits.*/
		
		int[] solverIndices = new int[0];

		public override void Initialize(){
			activeStatus.Clear();
			skinIndices.Clear();
			skinPoints.Clear();
			skinNormals.Clear();	
			skinRadiiBackstop.Clear();
			skinStiffnesses.Clear();
		}

		public void AddConstraint(bool active, int index, Vector4 point, Vector4 normal, float radius, float backstop, float stiffness){
			
			if (InSolver){
				Debug.LogError("You need to remove the constraints from the solver before attempting to add new individual constraints.");
				return;
			}

			activeStatus.Add(active);
			skinIndices.Add(index);
			skinPoints.Add(point);
			skinNormals.Add(normal);
			skinRadiiBackstop.Add(radius);
			skinRadiiBackstop.Add(backstop);
			skinStiffnesses.Add(stiffness);
		}

		protected override Oni.ConstraintType GetConstraintType(){
			return Oni.ConstraintType.Skin;
		}

		protected override ObiSolverData GetParticleDataFlags(){
			return new ObiSolverData(ObiSolverData.SkinConstraintsData.ALL);
		}

		public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
		
			List<int> constraints = new List<int>();
			
			for (int i = 0; i < skinIndices.Count; i++){
				if (skinIndices[i] == particleIndex) 
					constraints.Add(i);
			}
			
			return constraints;
		}

		protected override void OnAddToSolver(object info){

			ObiSolver solver = actor.Solver;
			
			// Set solver constraint data:
			solverIndices = new int[skinIndices.Count];
			for (int i = 0; i < skinIndices.Count; i++)
			{
				solverIndices[i] = actor.particleIndices[skinIndices[i]];
				solverIndices[i] = actor.particleIndices[skinIndices[i]];
			}
			
			Oni.SetSkinConstraints(solver.OniSolver,solverIndices,skinPoints.ToArray(),
											     skinNormals.ToArray(),
												 skinRadiiBackstop.ToArray(),
											     skinStiffnesses.ToArray(),
												 ConstraintCount,constraintOffset);

		}

		public override void PushDataToSolver(ObiSolverData data){ 
			
			if (actor == null || !actor.InSolver)
				return;

			if ((data.skinConstraintsData & ObiSolverData.SkinConstraintsData.SKIN_STIFFNESSES) != 0){
				for (int i = 0; i < skinStiffnesses.Count; i++){
					skinStiffnesses[i] = stiffness;
				}
			}

			Oni.SetSkinConstraints(actor.Solver.OniSolver,solverIndices,skinPoints.ToArray(),
											     skinNormals.ToArray(),
												 skinRadiiBackstop.ToArray(),
											     skinStiffnesses.ToArray(),
												 ConstraintCount,constraintOffset);
			
			if ((data.skinConstraintsData & ObiSolverData.SkinConstraintsData.ACTIVE_STATUS) != 0){
				UpdateConstraintActiveStatus();
			}
			
		}

		/**
		 * Returns the position of a skin constraint in world space. 
		 * Works both when the constraints are managed by a solver and when they aren't. 
		 */
		public Vector3 GetSkinPosition(int index){
			return skinPoints[index];
		}

		/**
		 * Returns the normal of a skin constraint in world space. 
		 * Works both when the constraints are managed by a solver and when they aren't. 
		 */
		public Vector3 GetSkinNormal(int index){
			return skinNormals[index];
		}
		
	}
}


