﻿using System.Linq;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Xenophobia : IFaithProperty
	{
		[SerializeField] private Sprite propertyIcon;

		string IFaithProperty.FaithPropertyName { get; set; } = "Xenophobia";
		string IFaithProperty.FaithPropertyDesc { get; set; } = "Only the leaders' species of this faith is considered the 'acceptable' one.";

		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
		}
		FaithData IFaithProperty.AssociatedFaith { get; set; }
		private FaithData Faith => ((IFaithProperty)this).AssociatedFaith;

		[SerializeField] private int nonMemberTakePoints = 10;
		[SerializeField] private int memberGivePoints = 15;

		public void Setup(FaithData associatedFaith)
		{
			FaithManager.Instance.FaithPropertiesEventUpdate.Add(CheckForMemberRaces);
			((IFaithProperty)this).AssociatedFaith = associatedFaith;
		}

		private void CheckForMemberRaces()
		{
			if (Faith.FaithLeaders.Count == 0) return;
			var leaderRaces = Faith.FaithLeaders.Select(leader => leader.characterSettings.GetRaceSo().name).ToList();
			foreach (var member in Faith.FaithMembers)
			{
				if (member.IsDeadOrGhost) continue;
				if (leaderRaces.Contains(member.characterSettings.GetRaceSo().name) == false)
				{
					FaithManager.TakePoints(nonMemberTakePoints, Faith.Faith.FaithName);
					Chat.AddExamineMsg(member.gameObject, "<i>You feel like you don't belong here..</i>");
					continue;
				}
				FaithManager.AwardPoints(memberGivePoints, Faith.Faith.FaithName);
			}
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			//(Max): Need ideas
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			//(Max): Need ideas
		}

		public void RandomEvent()
		{
			//TODO: Add Xenophobia events
		}
	}
}