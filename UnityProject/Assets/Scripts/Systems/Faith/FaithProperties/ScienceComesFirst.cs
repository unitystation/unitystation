using Systems.Score;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class ScienceComesFirst : IFaithProperty
	{
		[SerializeField] private Sprite propertyIcon;

		string IFaithProperty.FaithPropertyName { get; set; } = "Science comes first";
		string IFaithProperty.FaithPropertyDesc { get; set; } = "To worship is to know. The only way to get close to our creator is to study and observe the universe around us and unlock its secrets.";

		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
		}

		public FaithData AssociatedFaith { get; set; }

		public void Setup(FaithData associatedFaith)
		{
			ScoreMachine.Instance.OnScoreChanged.AddListener(TrackScore);
			AssociatedFaith = associatedFaith;
		}

		private void TrackScore(string name, int score)
		{
			if (name != RoundEndScoreBuilder.COMMON_SCORE_SCIENCEPOINTS || score < 1) return;
			FaithManager.AwardPoints(score, AssociatedFaith.Faith.FaithName);
		}

		public void OnJoinFaith(PlayerScript newMember) { /* intentionally left empty */ }

		public void OnLeaveFaith(PlayerScript member) { /* intentionally left empty */ }

		public void RandomEvent()
		{
			//TODO: Add events for science.
		}
	}
}