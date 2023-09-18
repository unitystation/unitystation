using System.Linq;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Xenophobia : IFaithProperty
	{
		private string faithPropertyName = "Xenophobia";
		private string faithPropertyDesc = "Only the leaders' species of this faith is considered the 'acceptable' one.";
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

		[SerializeField] private int nonMemberTakePoints = 10;
		[SerializeField] private int memberGivePoints = 15;

		public void Setup()
		{
			FaithManager.Instance.FaithPropertiesConstantUpdate.Add(CheckForMemberRaces);
		}

		private void CheckForMemberRaces()
		{
			if (FaithManager.Instance.FaithLeaders.Count == 0) return;
			var leaderRaces = FaithManager.Instance.FaithLeaders.Select(leader => leader.characterSettings.GetRaceSo().name).ToList();
			foreach (var member in FaithManager.Instance.FaithMembers)
			{
				if (leaderRaces.Contains(member.characterSettings.GetRaceSo().name) == false)
				{
					FaithManager.TakePoints(nonMemberTakePoints);
					Chat.AddExamineMsg(member.gameObject, "<i>You feel like you don't belong here..</i>");
					continue;
				}
				FaithManager.AwardPoints(memberGivePoints);
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

		}
	}
}