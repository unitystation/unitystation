using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DoorSwitchTrigger : InputTrigger
{
	private Animator animator;
	private SpriteRenderer spriteRenderer;

	public DoorController[] doorControllers;

	private void Start()
	{
		//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
		gameObject.layer = LayerMask.NameToLayer("WallMounts");
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		animator = GetComponent<Animator>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!isServer)
		{
			if (!PlayerManager.LocalPlayerScript.IsInReach(spriteRenderer.transform.position, 1.2f) ||
				PlayerManager.LocalPlayerScript.playerMove.isGhost)
			{
				return true;
			}

			//if the button is idle and not animating it can be pressed
			if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
			{
				InteractMessage.Send(originator, hand);
			}
		}
		else
		{
			var ps = originator.GetComponent<PlayerScript>();
			if (!ps.IsInReach(spriteRenderer.transform.position, 1.2f) ||
				ps.playerMove.isGhost)
			{
				return true;
			}
			for (int i = 0; i < doorControllers.Length; i++)
			{
				if (!doorControllers[i].IsOpened)
				{
					doorControllers[i].Open();
				}
			}
			RpcPlayButtonAnim();
		}

		return true;
	}

	[ClientRpc]
	public void RpcPlayButtonAnim()
	{
		animator.SetTrigger("activated");
	}
}