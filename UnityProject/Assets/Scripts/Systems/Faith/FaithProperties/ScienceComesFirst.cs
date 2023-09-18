using Systems.Score;

namespace Systems.Faith.FaithProperties
{
	public class ScienceComesFirst : IFaithProperty
	{
		private string faithPropertyName = "Science comes first";
		private string faithPropertyDesc = "To worship is to know. The only way to get close to our creator is to study and observe the universe around us and unlock its secrets.";

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

		public void RandomEvent()
		{

		}
	}
}