using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Systems.Clearance;
using UnityEngine;

public class RemotelyControlledBrain : BodyPartFunctionality
{

	private bool lockdown = false;

	public bool Lockdown => lockdown;

	private BasicClearanceSource BasicClearanceSource;

	public override void Awake()
	{
		base.Awake();

		BasicClearanceSource = this.GetComponent<BasicClearanceSource>();
	}

	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		if (lockdown)
		{
			var MovementSynchronisation = livingHealth.GetComponent<MovementSynchronisation>();
			if (MovementSynchronisation != null)
			{
				MovementSynchronisation.ServerAllowInput.RemovePosition(this);
			}
		}

	}

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		if (lockdown)
		{
			var MovementSynchronisation = livingHealth.GetComponent<MovementSynchronisation>();
			if (MovementSynchronisation != null)
			{
				MovementSynchronisation.ServerAllowInput.RecordPosition(this, !lockdown);
			}
		}

	} //Warning only add body parts do not remove body parts in this


	private void LockdownChange()
	{
		//MovementSynchronisation


		if (BasicClearanceSource != null)
		{
			BasicClearanceSource.ClearanceDisabled = lockdown;
		}

	}

}
