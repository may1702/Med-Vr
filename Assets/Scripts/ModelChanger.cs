using UnityEngine;
using System.Collections;

public class ModelChanger : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void changeModel(string name)
    {
        for (int i = 0; i < this.gameObject.transform.childCount; i++)
        {
            if (!gameObject.transform.GetChild(i).gameObject.name.Equals("RadialMenu") && gameObject.transform.GetChild(i).gameObject.activeSelf)
            {
                gameObject.transform.GetChild(i).gameObject.SetActive(false);
                break;
            }
        }

        Transform transform = this.gameObject.transform.FindChild(name);
        transform.gameObject.SetActive(true);

        
    }
}
