using US13.Core.Chat;
using US13.Player;

namespace US13.Systems.Antagonists.Antags.BloodBrothers.Abilities
{
	public class ImmuneToStuns : IBloodBrotherAbility
	{
		public float ChanceToGiveOnSpawn { get; } = 5f;
		public void GiveAbility(Mind mind)
		{
			var health = mind.Body.playerHealth;
			foreach (var bodyPart in health.BodyPartList)
			{
				if (bodyPart == null) continue;
				bodyPart.SelfArmor.StunImmunity = true;
			}

			Chat.AddExamineMsg(mind.Body.gameObject,
				"You've been hit by stun batons everyday by the guards, and the electricity now tickles.");
		}
	}
}