using Systems.Score;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class RockAndStone : IFaithProperty
	{
		string IFaithProperty.FaithPropertyName { get; set; } = "Rock and Stone";
		string IFaithProperty.FaithPropertyDesc { get; set; } = "Channel your inner dwarf. Mine the earth.";
		[SerializeField] private Sprite propertyIcon;
		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
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
			//Work on adding beard additions when joining this faith.
			//(Max): Adding body parts and changing sprites for them on the player is still too difficult and unreliable, Bod.
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			//Todo: Remove beard when leaving faith.
		}


		public void RandomEvent()
		{
			//Todo: Add events tied to lavaland
		}
	}
}