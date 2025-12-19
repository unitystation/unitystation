namespace Systems.Antagonists.Antags.BloodBrothers
{
	public interface IBloodBrotherAbility
	{
		public float ChanceToGiveOnSpawn { get; }
		public void GiveAbility(Mind mind);
	}
}