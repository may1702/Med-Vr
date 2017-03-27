using UnityEngine;
using System.Collections;
using System;

namespace Obi{

/**
 * Holds information about the physical properties of the substance emitted by an emitter.
 */
public class ObiEmitterMaterial : ScriptableObject
{

	public class MaterialChangeEventArgs : EventArgs{

		public MaterialChanges changes;

		public MaterialChangeEventArgs(MaterialChanges changes){
			this.changes = changes;
		}
	}

	[Flags]
	public enum MaterialChanges{
		PER_MATERIAL_DATA = 0,
		PER_PARTICLE_DATA = 1 << 0
	}

	// fluid parameters:
	public bool isFluid = true;	/**< do the emitter particles generate density constraints?*/
	public float restRadius = 0.1f;
	public float randomRadius = 0.0f;		/**< A random amount between 0 and randomRadius gets added to each particle if the material is set to non-fluid.*/
	public float smoothingRadius = 0.2f;
	public float relaxationFactor = 600;	/**< how stiff the density corrections are.*/
	public float restDensity = 1000;		/**< rest density of the fluid particles.*/
	public float viscosity = 0.01f;			/**< viscosity of the fluid particles.*/
	public float cohesion = 0.1f;
	public float surfaceTension = 0.1f;	/**< surface tension of the fluid particles.*/

	// gas parameters:
	public float buoyancy = -1.0f; 						/**< how dense is this material with respect to air?*/
	public float atmosphericDrag = 0;					/**< amount of drag applied by the surrounding air to particles near the surface of the material.*/
	public float atmosphericPressure = 0;				/**< amount of pressure applied by the surrounding air particles.*/
	public float vorticity = 0.0f;						/**< amount of baroclinic vorticity injected.*/
	
	// elastoplastic parameters:
	//public float elasticRange; 		/** radius around a particle in which distance constraints are created.*/
	//public float plasticCreep;		/**< rate at which a deformed plastic material regains its shape*/
	//public float plasticThreshold;	/**< amount of stretching stress that a elastic material must undergo to become plastic.*/

	private EventHandler<MaterialChangeEventArgs> onChangesMade;
	public event EventHandler<MaterialChangeEventArgs> OnChangesMade {
	
	    add {
	        onChangesMade -= value;
	        onChangesMade += value;
	    }
	    remove {
	        onChangesMade -= value;
	    }
	}

	public void CommitChanges(MaterialChanges changes){
		if (onChangesMade != null)
				onChangesMade(this,new MaterialChangeEventArgs(changes));
	}

	public void OnValidate(){
		smoothingRadius = Mathf.Max(0.001f,smoothingRadius);
		restRadius = Mathf.Max(0.001f,restRadius);
		viscosity = Mathf.Max(0,viscosity);
		atmosphericDrag = Mathf.Max(0,atmosphericDrag);
	}

	public Oni.FluidMaterial GetEquivalentOniMaterial()
	{
		Oni.FluidMaterial material = new Oni.FluidMaterial();
		material.smoothingRadius = smoothingRadius;
		material.relaxationFactor = relaxationFactor;
		material.restDensity = restDensity;
		material.viscosity = viscosity;
		material.cohesion = cohesion;
		material.surfaceTension = surfaceTension;
		material.buoyancy = buoyancy;
		material.atmosphericDrag = atmosphericDrag;
		material.atmosphericPressure = atmosphericPressure;
		material.vorticity = vorticity;
		return material;
	}
}
}

