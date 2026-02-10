using US13.Core.Chat;
using US13.Player;

namespace US13.Systems.Antagonists.Antags.BloodBrothers.Abilities
{
	public class ImmuneToAllHeat : IBloodBrotherAbility
	{
		public float ChanceToGiveOnSpawn { get; } = 10f;
		public void GiveAbility(Mind mind)
		{
			var health = mind.Body.playerHealth;
			foreach (var bodyPart in health.BodyPartList)
			{
				if (bodyPart == null) continue;
				bodyPart.SelfArmor.Energy = 100f;
				bodyPart.SelfArmor.Fire = 100f;
				bodyPart.SelfArmor.Laser = 100f;
			}

			Chat.AddExamineMsg(mind.Body.gameObject,
				"You've an unremarkable ability to withstand all forms of energy.");
		}
	}
}