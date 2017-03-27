using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
 	 * Holds information about volume constraints for an actor.
 	 */
	[DisallowMultipleComponent]
	public class ObiVolumeConstraints : ObiConstraints 
	{
		
		[Tooltip("Amount of pressure applied to the cloth.")]
		public float overpressure = 1;

		[Range(0,1)]
		[Tooltip("Stiffness of the volume constraints. Higher values will make the constraints to try harder to enforce the set volume.")]
		public float stiffness = 1;
		
		[HideInInspector] [NonSerialized] int volumeTrianglesOffset;	/**< start of triangle indices in solver*/
		[HideInInspector] [NonSerialized] int volumeParticlesOffset;	/**< start of particle indices in solver*/

		[HideInInspector] public List<int> triangleIndices = new List<int>();			/**< Triangle indices.*/
		[HideInInspector] public List<int> firstTriangle = new List<int>();				/**< index of first triangle for each constraint.*/
		[HideInInspector] public List<int> numTriangles = new List<int>();				/**< num of triangles for each constraint.*/

		[HideInInspector] public List<float> restVolumes = new List<float>();				/**< rest volume for each constraint.*/
		[HideInInspector] public List<Vector2> pressureStiffness = new List<Vector2>();		/**< pressure and stiffness for each constraint.*/
		
		int[] solverIndices;
		int[] solverFirstTriangle;
		int[] solverFirstParticle;

		/**
		 * Initialize with the total amount of triangles used by all constraints, and the number of constraints.
		 */
		public override void Initialize(){
			activeStatus.Clear();
			triangleIndices.Clear();
			firstTriangle.Clear();
			numTriangles.Clear();
			restVolumes.Clear();
			pressureStiffness.Clear();
		}
		

		public void AddConstraint(bool active,  int[] triangles, float restVolume, float pressure, float stiffness){

			if (InSolver){
				Debug.LogError("You need to remove the constraints from the solver before attempting to add new individual constraints.");
				return;
			}

			activeStatus.Add(active);

			firstTriangle.Add((int)triangleIndices.Count/3);
			numTriangles.Add((int)triangles.Length/3);

			triangleIndices.AddRange(triangles);

			restVolumes.Add(restVolume);
			pressureStiffness.Add(new Vector2(pressure,stiffness));
			
		}

		protected override Oni.ConstraintType GetConstraintType(){
			return Oni.ConstraintType.Volume;
		}

		protected override ObiSolverData GetParticleDataFlags(){
			return new ObiSolverData(ObiSolverData.VolumeConstraintsData.ALL);
		}

		/**
		 * Since each individual volume constraint can have a variable number of particles and/or triangles, we need a way
		 * to update the indices of the first triangle/particle in the array when constraints are added/removed from the solver.
		 */
		private void UpdateFirstParticleAndTriangleIndices(int removedParticles, int removedTriangles){

			if (!inSolver || actor == null || !actor.InSolver)
				return;

			/*if (constraintOffset < actor.solver.volumeConstraints.volumeFirstTriangle.Length)
				actor.solver.volumeConstraints.volumeFirstTriangle[constraintOffset] -= removedTriangles;
			if (constraintOffset < actor.solver.volumeConstraints.volumeFirstParticle.Length)
				actor.solver.volumeConstraints.volumeFirstParticle[constraintOffset] -= removedParticles;*/

		}
		
		public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
		
			List<int> constraints = new List<int>();
			
			for (int i = 0; i < restVolumes.Count; i++){
			
				/*for (int j = 0; j < numParticles[i]; j++){
					if (particleIndices[firstParticle[i]+j] == particleIndex) 
						constraints.Add(i);
				}*/
				
			}
			
			return constraints;
		}
		
		protected override void OnAddToSolver(object info){
			
			ObiSolver solver = actor.Solver;
			
			// Set solver constraint data:
			solverIndices = new int[triangleIndices.Count];
			for (int i = 0; i < triangleIndices.Count; i++)
			{
				solverIndices[i] = actor.particleIndices[triangleIndices[i]];
			}

			solverFirstTriangle = new int[firstTriangle.Count];
			for (int i = 0; i < firstTriangle.Count; i++)
			{
				solverFirstTriangle[i] = Oni.GetVolumeTriangleCount(solver.OniSolver) + firstTriangle[i];
			}

			Oni.SetVolumeConstraints(solver.OniSolver,solverIndices,
												  solverFirstTriangle,
											      numTriangles.ToArray(),
												  restVolumes.ToArray(),
												  pressureStiffness.ToArray(),
												  ConstraintCount,constraintOffset);
			
		}
		
		public override void PushDataToSolver(ObiSolverData data){ 
			
			if (actor == null || !actor.InSolver)
				return;
			
			if ((data.volumeConstraintsData & ObiSolverData.VolumeConstraintsData.VOLUME_PRESSURE_STIFFNESSES) != 0){

				for (int i = 0; i < pressureStiffness.Count; i++){
					pressureStiffness[i] = new Vector2(overpressure,stiffness);
				}

			}

			Oni.SetVolumeConstraints(actor.Solver.OniSolver,solverIndices,
												  solverFirstTriangle,
											      numTriangles.ToArray(),
												  restVolumes.ToArray(),
												  pressureStiffness.ToArray(),
												  ConstraintCount,constraintOffset);

			if ((data.volumeConstraintsData & ObiSolverData.VolumeConstraintsData.ACTIVE_STATUS) != 0){
				UpdateConstraintActiveStatus();
			}
			
		}
		
	}
}





