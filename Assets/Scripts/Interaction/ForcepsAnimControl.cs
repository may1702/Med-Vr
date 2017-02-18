using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForcepsAnimControl : MonoBehaviour {

    public VRTK.VRTK_ControllerEvents vrtk;
    public Animator anim;

	// Use this for initialization
	void Start () {
        vrtk.TriggerPressed += new VRTK.ControllerInteractionEventHandler(CloseForceps);
        vrtk.TriggerReleased += new VRTK.ControllerInteractionEventHandler(IdleForceps);
        anim = GameObject.Find("forceps-rigged").GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CloseForceps(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        bool closeBool = anim.GetBool("close");
        Debug.Log("Close: " + closeBool.ToString());
        anim.SetBool("close", true);
    }

    public void IdleForceps(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        anim.SetBool("close", false);
    }
}
