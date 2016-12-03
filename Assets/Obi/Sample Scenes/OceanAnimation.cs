using UnityEngine;
using System.Collections;

public class OceanAnimation : MonoBehaviour {

	public Vector2 waveSpeed;
	Renderer renderer_;

	// Use this for initialization
	void Start () {
		renderer_ = gameObject.GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update () {
		if (renderer_ != null && renderer_.material != null){
			Vector2 offset = renderer_.material.mainTextureOffset;
			offset += waveSpeed * Time.deltaTime;
			renderer_.material.mainTextureOffset = offset;
		}
	}
}
