using System;
using System.Collections;
using System.Collections.Generic;
using Doors;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AccessRestrictions))]
public class AccessModule : DoorModuleBase
{
	private AccessRestrictions accessRestrictions;
	private DoorAnimatorV2 doorAnimator;
	private APCPoweredDevice apc;

	[SerializeField]
	[Tooltip("When the door is at low voltage, this is the chance that the access check gives a false positive.")]
	private float lowVoltageOpenChance = 0.05f;

	protected override void Awake()
	{
		base.Awake();
		doorAnimator = GetComponentInParent<DoorAnimatorV2>();
		accessRestrictions = GetComponent<AccessRestrictions>();
		apc = GetComponentInParent<APCPoweredDevice>();
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
			//If the door is in low voltage, there's a very low chance the access check fails and opens anyway.
			//Meant to represent the kind of weird flux state bits are when in low voltage systems.
			if (apc.State == PowerStates.LowVoltage)
			{
				if (Random.value < lowVoltageOpenChance)
				{
					return true;
				}

			}
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
