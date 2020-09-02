using System.Collections;
using System.Collections.Generic;
using Doors;
using UnityEngine;

[RequireComponent(typeof(AccessRestrictions))]
public class AccessModule : DoorModuleBase
{
	private AccessRestrictions accessRestrictions;
	private DoorAnimatorV2 doorAnimator;

	protected override void Awake()
	{
		base.Awake();
		doorAnimator = GetComponentInParent<DoorAnimatorV2>();
		accessRestrictions = GetComponent<AccessRestrictions>();
	}

	public override ModuleSignal OpenInteraction(HandApply interaction)
	{
		return ModuleSignal.Continue;
	}

	public override ModuleSignal ClosedInteraction(HandApply interaction)
	{
		if (!CheckAccess(interaction.Performer))
		{
			return ModuleSignal.ContinueWithoutDoorStateChange;
		}

		return ModuleSignal.Continue;
	}

	public override bool CanDoorStateChange()
	{
		return true;
	}

	private bool CheckAccess(GameObject player)
	{
		if (!accessRestrictions.CheckAccess(player))
		{
			DenyAccess();
			return false;
		}

		return true;
	}

	private void DenyAccess()
	{
		doorAnimator.RequestAnimation(doorAnimator.PlayDeniedAnimation());
	}
}
