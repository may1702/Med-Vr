using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Obi;

[RequireComponent(typeof(ObiSolver))]
public class CollisionEventHandler : MonoBehaviour {

 	ObiSolver solver;
	public GameObject obj;
	
	Dictionary<int,GameObject> objects = new Dictionary<int, GameObject>();
	HashSet<int> collisionIndices = new HashSet<int>();

	void Awake(){
		solver = GetComponent<Obi.ObiSolver>();
	}

	void OnEnable () {
		solver.OnCollision += Solver_OnCollision;
	}

	void OnDisable(){
		solver.OnCollision -= Solver_OnCollision;
	}
	
	void Solver_OnCollision (object sender, Obi.ObiSolver.ObiCollisionEventArgs e)
	{
		collisionIndices.Clear();

		for(int i = 0;  i < e.points.Length; ++i){
			if (e.distances[i] <= 0.1f){

				collisionIndices.Add(e.indices[i*2]);

				GameObject pSystem = null;
				Vector3 pos = ((ObiSolver)sender).renderablePositions[e.indices[i*2]];
				if (!objects.TryGetValue(e.indices[i*2],out pSystem)){
					objects[e.indices[i*2]] = GameObject.Instantiate(obj,pos,Quaternion.identity) as GameObject;
				}else{
					pSystem.transform.position = pos;
				}
			}
		}

		var itemsToRemove = objects.Where(x => !collisionIndices.Contains(x.Key)).ToArray();
		foreach (var item in itemsToRemove){
			GameObject.DestroyImmediate(item.Value);
    		objects.Remove(item.Key);
		}
		
	}

}
