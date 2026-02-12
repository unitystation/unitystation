using UnityEngine;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs.Functionality;

namespace US13.Items.Science.Implants
{
	public class WeldingShieldImplant : BodyPartFunctionality
	{
		private EyeFlash flash;

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			flash = RelatedPart.ContainedIn.GetComponent<EyeFlash>();
			flash.WeldingShieldImplants++;
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			flash.WeldingShieldImplants--;
			flash = null;
		}
	}
}
