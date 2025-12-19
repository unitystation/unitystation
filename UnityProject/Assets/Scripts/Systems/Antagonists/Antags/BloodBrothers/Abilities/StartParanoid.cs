namespace Systems.Antagonists.Antags.BloodBrothers.Abilities
{
	public class StartParanoid : IBloodBrotherAbility
	{
		public float ChanceToGiveOnSpawn { get; } = 25f;

		public void GiveAbility(Mind mind)
		{
			mind.Body.playerHealth.reagentPoolSystem.BloodPool.Add(CommonSicknesses.Instance.ParanoiaReagent, 10f);
			Chat.AddExamineMsg(mind.Body.gameObject,
				"Due to your past in prison.. You've gained paranoia from the experiments they've done on you.");
		}
	}
}