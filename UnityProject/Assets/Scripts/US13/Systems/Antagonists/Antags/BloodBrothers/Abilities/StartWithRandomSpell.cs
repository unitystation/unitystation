using US13.Core.Chat;
using US13.Player;

namespace US13.Systems.Antagonists.Antags.BloodBrothers.Abilities
{
	public class StartWithRandomSpell : IBloodBrotherAbility
	{
		public float ChanceToGiveOnSpawn { get; } = 45f;
		public void GiveAbility(Mind mind)
		{
			Wizard.AddSpellToPlayer(Wizard.GetRandomNonRobeSpecificWizardSpell(), mind);
			Chat.AddExamineMsg(mind.Body.gameObject,
				"Due to your past in prison.. You've gained magical ability.");
		}
	}
}