using Systems.Score;

namespace Systems.Faith.FaithProperties
{
	public class ScienceComesFirst : IFaithProperty
	{
		private string faithPropertyName;
		private string faithPropertyDesc;

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

		public void Setup()
		{
			ScoreMachine.Instance.OnScoreChanged.AddListener(TrackScore);
		}

		private void TrackScore(string name, int score)
		{
			if (name != RoundEndScoreBuilder.COMMON_SCORE_SCIENCEPOINTS || score < 1) return;
			FaithManager.AwardPoints(score);
		}

		public void OnJoinFaith(PlayerScript newMember) { }

		public void OnLeaveFaith(PlayerScript member) { }

		public bool HasTriggeredFaithAction(PlayerScript memberWhoTriggered)
		{
			return false;
		}

		public void RandomEvent()
		{

		}
	}
}