namespace HealthV2.TraumaTypes
{
	public class TraumaBombExplosionHumanoid : TraumaLogic
	{
		public override void OnTakeDamage(BodyPartDamageData data)
		{
			if ( data.TramuticDamageType != TraumaticDamageTypes.NONE ) return;
			if ( data.AttackType != AttackType.Bomb ) return;
			if ( deadlyDamageInOneHit > data.DamageAmount ) return;
			if ( DMMath.Prob(GetBombProtectionPercentage()) ) return;
			if ( DMMath.Prob(data.TraumaDamageChance) == false ) return;
			ProgressDeadlyEffect();
		}

		public override void ProgressDeadlyEffect()
		{
			Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
				$"<color=red><size=+6>The {bodyPart.gameObject.ExpensiveName()} gets torn from the sudden force and removed from its place!</size></color>");
			bodyPart.TryRemoveFromBody();
		}

		private float GetBombProtectionPercentage()
		{
			var percent = 0f;
			foreach (var armor in bodyPart.ClothingArmors)
			{
				percent += armor.Bomb;
			}
			return percent;
		}
	}
}