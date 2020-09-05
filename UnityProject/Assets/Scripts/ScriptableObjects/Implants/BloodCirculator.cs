using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodCirculator : ImplantProperty
{
	[Tooltip("The maximum heartrate this blood circulator can achieve.")]
	public float HeartRateMax = 100;

	[Tooltip("The maximum amount the heartrate ")]
	public float HeartRateDelta = 20;

	public override void ImplantUpdate(ImplantBase implant, LivingHealthMasterBase healthMaster)
	{

	}
}
