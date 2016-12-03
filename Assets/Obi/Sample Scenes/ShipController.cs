using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Obi;

[RequireComponent(typeof(ObiAerodynamicConstraints))]
public class ShipController : MonoBehaviour {

	public float randomWindChangeRate = 3;
	public float randomWindIntensity = 8;

	private ObiAerodynamicConstraints sail;

	private Vector3 wind;
	private float noiseCoord;

	// Use this for initialization
	void Start () {
		sail = GetComponent<ObiAerodynamicConstraints>();
	}
	
	public void ChangeWindDirection(BaseEventData  data){

		PointerEventData pointerData = data as PointerEventData;
		Vector3 drag = pointerData.position - pointerData.pressPosition;
		wind = new Vector3(Mathf.Clamp(drag.x*0.1f,-10,10),0,Mathf.Clamp(drag.y*0.1f,-10,10));

	}

	public void Update(){

		float randomWindX = (Mathf.PerlinNoise(noiseCoord,0)-0.5f)*2;
		float randomWindZ = (Mathf.PerlinNoise(0,noiseCoord)-0.5f)*2;
		noiseCoord += randomWindChangeRate * Time.deltaTime;

		sail.windVector = wind + new Vector3(randomWindX,0,randomWindZ) * randomWindIntensity;
		sail.PushDataToSolver(new ObiSolverData(ObiSolverData.AerodynamicConstraintsData.WIND)); 
	}

	public void SetRandomWindIntensity(float intensity){
		randomWindIntensity = intensity;
	}

}
