using UnityEngine;
using US13.Core.Chat;
using US13.Health.Objects;
using Util;

namespace US13.HealthV2.Living.BodyParts.Damage.Trauma.TraumaTypes
{
	public class TraumaBombExplosionHumanoid : TraumaLogic
	{

		[SerializeField] private float juddgerResistence = 12f;

		public override void OnTakeDamage(BodyPartDamageData data)
		{
			if ( data.TramuticDamageType != TraumaticDamageTypes.NONE ) return;
			if ( data.AttackType != AttackType.Bomb ) return;
			if ( DMMath.Prob(GetBombProtectionPercentage()) ) return;
			if ( DMMath.Prob(data.TraumaDamageChance) == false ) return;
			if ( deadlyDamageInOneHit > data.DamageAmount)
			{
				DoJuddgerDamage(data);
				return;
			}
			ProgressDeadlyEffect();
		}

		private void DoJuddgerDamage(BodyPartDamageData data)
		{
			bodyPart.TakeDamage(data.DamagedBy, data.DamageAmount / juddgerResistence,
				AttackType.Internal, DamageType.Brute,
				true, false,
				traumaDamageChance: 0, invokeOnDamageEvent: false);
			Chat.AddExamineMsg(bodyPart.HealthMaster.gameObject,
				$"<color=red><size=+4>You feel something itch under your skin.</size></color>");
		}

		public override void ProgressDeadlyEffect()
		{
			Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
				$"<color=red><size=+6>The {SweetExtensions.ExpensiveName(bodyPart.gameObject)} gets torn from the sudden force and removed from its place!</size></color>");
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