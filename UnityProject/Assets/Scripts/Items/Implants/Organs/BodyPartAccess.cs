using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Systems.Clearance;
using UnityEngine;

public class BodyPartAccess : BodyPartFunctionality
{
	public IClearanceSource IClearanceSource;

	public void Awake()
	{
		IClearanceSource = this.GetComponent<IClearanceSource>();
	}

	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		var GroupedAccess = livingHealth.GetComponent<GroupedAccess>();
		if (GroupedAccess != null)
		{
			GroupedAccess.RemoveIClearanceSource(IClearanceSource);
		}
	}

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		var GroupedAccess = livingHealth.GetComponent<GroupedAccess>();
		if (GroupedAccess != null)
		{
			GroupedAccess.AddIClearanceSource(IClearanceSource);
		}
	} //Warning only add body parts do not remove body parts in this
}