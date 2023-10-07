using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class PainIsVirtue : IFaithProperty
	{
		string IFaithProperty.FaithPropertyName { get; set; } = "Pain Is Virtue";
		string IFaithProperty.FaithPropertyDesc { get; set; } = "The flesh is sinful, but the soul is strong. You can only find comfort in pain. Virtue is shown through suffering of self and others.";
		[SerializeField] private Sprite propertyIcon;
		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
		}

		public void Setup()
		{
			//Todo: add discomfort checks.
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
			//Todo: add random events for pain is virtue.
		}
	}
}