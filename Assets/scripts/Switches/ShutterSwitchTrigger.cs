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
	}

	public override void OnStartClient(){
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	IEnumerator WaitForLoad(){
		yield return new WaitForSeconds(1f);
		SyncShutters(IsClosed);
	}
		
	public override void Interact()
	{
		if (!PlayerManager.LocalPlayerScript.IsInReach(transform, 1.5f))
			return;

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
		foreach (var s in TriggeringObjects) {
			s.Trigger(isClosed);
		}
	}
}
