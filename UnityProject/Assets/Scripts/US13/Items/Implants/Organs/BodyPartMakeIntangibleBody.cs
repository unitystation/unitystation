using UnityEngine;
using US13.HealthV2.Living;
using US13.Player.MovementV2;

namespace US13.Items.Implants.Organs
{


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
}