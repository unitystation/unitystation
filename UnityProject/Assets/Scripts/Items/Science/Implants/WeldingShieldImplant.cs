using HealthV2;
using Player;

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

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			flash.WeldingShieldImplants--;
			flash = null;
		}
	}
}
