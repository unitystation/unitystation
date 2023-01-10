using HealthV2;
using Player;

namespace Items.Implants.Organs
{
	public class WeldingShieldImplant : BodyPartFunctionality
	{
		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			if(RelatedPart.HealthMaster.playerScript.gameObject.TryGetComponent<PlayerFlashEffects>(out var flash) == false) return;
			flash.WeldingShieldImplants.Add(this);
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			if (RelatedPart.HealthMaster.playerScript.gameObject.TryGetComponent<PlayerFlashEffects>(out var flash) == false) return;
			flash.WeldingShieldImplants.Remove(this);
		}
	}
}
