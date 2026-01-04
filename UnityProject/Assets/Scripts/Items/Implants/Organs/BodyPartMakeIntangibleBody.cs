using HealthV2;
using UnityEngine;

public class BodyPartMakeIntangibleBody : BodyPartFunctionality
{
	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		var Object = livingHealth.GetComponent<MovementSynchronisation>();
		Object.Intangible = true;
		base.OnAddedToBody(livingHealth);
	}

	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
	{
		var Object = livingHealth.GetComponent<MovementSynchronisation>();
		Object.Intangible = false;
		base.OnRemovedFromBody(livingHealth, source);
	}
}
