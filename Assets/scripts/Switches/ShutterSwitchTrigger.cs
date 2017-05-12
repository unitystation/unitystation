using InputControl;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShutterSwitchTrigger: InputTrigger
{

	public ObjectTrigger[] TriggeringObjects;

    [SyncVar(hook = "SyncShutters")]
	public bool IsClosed;

    private Animator animator;

    void Start()
    {
		animator = GetComponent<Animator>();
		IsClosed = true;
		SyncShutters(IsClosed);
    }

    public override void Interact()
    {
		if (!PlayerManager.LocalPlayerScript.IsInReach(transform, 0.5f))
			return;
			
		Debug.Log("INTERACT!");
		//if the button is idle and not animating it can be pressed
		//this is weird it should check all children objects to see if they are idle and finished
		if (this.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) {
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleShutters(gameObject);
		} else {
			Debug.Log("DOOR NOT FINISHED CLOSING YET!");
		}
    }

    void SyncShutters(bool isClosed)
    {
		foreach (var s in TriggeringObjects)
		{
			s.Trigger(isClosed);
		}
    }
}
