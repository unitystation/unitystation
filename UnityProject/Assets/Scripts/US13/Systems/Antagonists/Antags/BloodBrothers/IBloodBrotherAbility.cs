using US13.Player;

namespace US13.Systems.Antagonists.Antags.BloodBrothers
{
	public interface IBloodBrotherAbility
	{
		public float ChanceToGiveOnSpawn { get; }
		public void GiveAbility(Mind mind);
	}
}