using US13.Player;

namespace US13.Systems.Faith
{
	public interface IFaithMiracle
	{
		public string FaithMiracleName { get; protected set; }
		public string FaithMiracleDesc { get; protected set; }
		public SpriteDataSO MiracleIcon { get; protected set; }

		public int MiracleCost { get; set; }
		public void DoMiracle(FaithData associatedFaith, PlayerScript invoker = null);
	}
}