using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class PainIsVirtue : IFaithProperty
	{
		private string faithPropertyName;
		private string faithPropertyDesc;

		string IFaithProperty.FaithPropertyName
		{
			get => faithPropertyName;
			set => faithPropertyName = value;
		}

		string IFaithProperty.FaithPropertyDesc
		{
			get => faithPropertyDesc;
			set => faithPropertyDesc = value;
		}

		public void Setup()
		{
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			newMember.playerHealth.OnTakeDamageType += EvaluatePain;
		}

		private void EvaluatePain(DamageType damageType, GameObject idk, float damage)
		{
			if (damageType.HasFlag(DamageType.Stamina) || damageType.HasFlag(DamageType.Clone)) return;
			if (damage < 4) return;
			FaithManager.AwardPoints((int)damage * 2);
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			member.playerHealth.OnTakeDamageType -= EvaluatePain;
		}

		public void RandomEvent()
		{
		}
	}
}