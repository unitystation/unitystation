using Systems.Score;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class RockAndStone : IFaithProperty
	{
		private string faithPropertyName = "Rock and Stone";
		private string faithPropertyDesc = "Channel your inner dwarf. Mine the earth.";
		[SerializeField] private Sprite faithIcon;

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

		Sprite IFaithProperty.FaithIcon
		{
			get => faithIcon;
			set => faithIcon = value;
		}

		public void Setup()
		{
			ScoreMachine.Instance.OnScoreChanged.AddListener(UpdatePoints);
		}

		private void UpdatePoints(string ID, int points)
		{
			if (ID != RoundEndScoreBuilder.COMMON_SCORE_LABORPOINTS) return;
			FaithManager.AwardPoints(points);
		}

		public void OnJoinFaith(PlayerScript newMember)
		{

		}

		public void OnLeaveFaith(PlayerScript member)
		{
		}


		public void RandomEvent()
		{
			throw new System.NotImplementedException();
		}
	}
}