using System.Collections.Generic;
using Shared.Managers;

namespace Systems.Faith
{
	public class FaithManager : SingletonManager<FaithManager>
	{
		public Faith DefaultFaith;
		public Faith CurrentFaith { get; private set; }
		public int FaithPoints { get; private set; }
		public float FaithEventsCheckTimeInSeconds = 390f;
		public List<PlayerScript> FaithMembers { get; private set; } = new List<PlayerScript>();

		public override void Awake()
		{
			base.Awake();
			if(CustomNetworkManager.IsServer == false) return;
			EventManager.AddHandler(Event.RoundEnded, ResetReligion);
#if UNITY_EDITOR
			UpdateManager.Add(UpdateMe, 60f);
#else
			UpdateManager.Add(UpdateMe, FaithEventsCheckTimeInSeconds);
#endif
		}

		private void ResetReligion()
		{
			CurrentFaith = DefaultFaith;
			FaithPoints = 0;
		}

		private void UpdateMe()
		{
			if (FaithPoints.IsBetween(-50, 50)) return;
			if (DMMath.Prob(35))
			{
				CurrentFaith.FaithProperties.PickRandom().RandomEvent();
			}
			CheckTolerance();
		}

		private void CheckTolerance()
		{
			if (FaithMembers.Count < 5 || CurrentFaith.ToleranceToOtherFaiths is ToleranceToOtherFaiths.Accepting) return;
			//TODO: Logic me up daddy uwu
		}
	}
}