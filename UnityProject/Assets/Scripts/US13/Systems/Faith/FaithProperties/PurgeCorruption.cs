using Logs;
using UnityEngine;
using US13.Player;
using US13.Systems.Antagonists;

namespace US13.Systems.Faith.FaithProperties
{
	public class PurgeCorruption : IFaithProperty
	{
		[SerializeField] private string faithPropertyName = "Purge Corruption";
		[SerializeField] private string faithPropertyDesc = "This faith believes that all those of unholy blood must be exorcised or killed.";

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

		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
		}

		public FaithData AssociatedFaith { get; set; }

		[SerializeField] private Sprite propertyIcon;
		[SerializeField] private TeamData vampireTeam;
		[SerializeField] private int livingVampirePunishmentPoints = 10;
		[SerializeField] private int deadVampireRewardPoints = 10;

		public void Setup(FaithData associatedFaith)
		{
			FaithManager.Instance.FaithPropertiesEventUpdate.Add(CheckForVampires);
			AssociatedFaith = associatedFaith;
		}

		private void CheckForVampires()
		{
			foreach (var antag in AntagManager.Instance.ActiveAntags)
			{
				if (antag.CurTeam.Data != vampireTeam) continue;
				if (antag.Owner?.Body?.playerHealth?.IsDead == true)
				{
					FaithManager.AwardPoints(deadVampireRewardPoints, AssociatedFaith.Faith.FaithName);
				}
				else FaithManager.TakePoints(livingVampirePunishmentPoints, AssociatedFaith.Faith.FaithName);
			}
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
		}

		public void OnLeaveFaith(PlayerScript member)
		{
		}

		public void RandomEvent()
		{
			//Todo: add random events for gluttony.
		}
	}
}