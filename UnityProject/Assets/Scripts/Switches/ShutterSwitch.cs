using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Component which enables the object to function as a shutter switch, opening / closing shutters it is
/// connected with (via TriggeringObjects)
/// </summary>
public class ShutterSwitch : NetworkBehaviour, IInteractable<HandApply>
{
	private Animator animator;

	[SyncVar(hook = "SyncShutters")] public bool IsClosed;

	[Tooltip("Shutters this switch controls.")]
	public ObjectTrigger[] TriggeringObjects;

	private void Start()
	{
		//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
		gameObject.layer = LayerMask.NameToLayer("WallMounts");

		animator = GetComponent<Animator>();
	}

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	private IEnumerator WaitForLoad()
	{
		yield return WaitFor.Seconds(3f);
		SyncShutters(IsClosed);
	}

	public InteractionControl Interact(HandApply interaction)
	{
		if (!CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS.DoesValidate(interaction, NetworkSide.CLIENT))
		{
			return InteractionControl.CONTINUE_PROCESSING;
		}

		//if the button is idle and not animating it can be pressed
		//this is weird it should check all children objects to see if they are idle and finished
		if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleShutters(gameObject);
			return InteractionControl.STOP_PROCESSING;

		}
		else
		{
			Logger.Log("DOOR NOT FINISHED CLOSING YET!", Category.Shutters);
		}

		return InteractionControl.CONTINUE_PROCESSING;

	}

	private void SyncShutters(bool isClosed)
	{
		foreach (ObjectTrigger s in TriggeringObjects)
		{
			if (s != null)
			{ //Apparently unity can't handle the null reference Properly for this case
				s.Trigger(isClosed);
			}
			else {
				Logger.LogError("Missing reference to shutter.", Category.Shutters);
			}
		}
	}


}