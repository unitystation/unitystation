using HealthV2;
using Player;
using UnityEngine;

namespace Items.Implants.Organs
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
