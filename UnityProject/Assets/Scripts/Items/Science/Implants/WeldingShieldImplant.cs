using HealthV2;
using Player;

namespace Items.Implants.Organs
{
	public class WeldingShieldImplant : BodyPartFunctionality
	{
		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			if(RelatedPart.HealthMaster.playerScript.gameObject.TryGetComponent<PlayerFlashEffects>(out var flash) == false) return;
			flash.WeldingShieldImplants++;
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			if (RelatedPart.HealthMaster.playerScript.gameObject.TryGetComponent<PlayerFlashEffects>(out var flash) == false) return;
			flash.WeldingShieldImplants--;
		}
	}
}
