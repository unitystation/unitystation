using UnityEngine;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs.Functionality;

namespace US13.Items.Science.Implants
{
	[System.Serializable]
	public class WeldingShieldImplant : BodyPartBehavior
	{
		[UnityEngine.SerializeReference] private EyeFlash flash;

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			flash = RelatedPart.ContainedIn.GetBodyPartBehavior<EyeFlash>();
			flash.WeldingShieldImplants++;
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			flash.WeldingShieldImplants--;
			flash = null;
		}
	}
}
