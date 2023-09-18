using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class PainIsVirtue : IFaithProperty
	{
		private string faithPropertyName = "Pain Is Virtue";
		private string faithPropertyDesc = "The flesh is sinful, but the soul is strong. You can only find comfort in pain. Virtue is shown through suffering of self and others.";
		[SerializeField] private Sprite faithIcon;

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

		Sprite IFaithProperty.FaithIcon
		{
			get => faithIcon;
			set => faithIcon = value;
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