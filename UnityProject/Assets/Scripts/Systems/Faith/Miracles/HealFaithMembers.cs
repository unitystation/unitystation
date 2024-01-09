﻿using UnityEngine;
using Util.Independent.FluentRichText;


namespace Systems.Faith.Miracles
{
	public class HealFaithMembers : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Heal all believers.";
		[SerializeField] private string faithMiracleDesc = "Heal all alive believers to max health. This does not cure sicknesses, cure trauma or re-grow limbs.";
		[SerializeField] private SpriteDataSO miracleIcon;

		string IFaithMiracle.FaithMiracleName
		{
			get => faithMiracleName;
			set => faithMiracleName = value;
		}

		string IFaithMiracle.FaithMiracleDesc
		{
			get => faithMiracleDesc;
			set => faithMiracleDesc = value;
		}

		SpriteDataSO IFaithMiracle.MiracleIcon
		{
			get => miracleIcon;
			set => miracleIcon = value;
		}

		public int MiracleCost { get; set; } = 1500;


		public void DoMiracle(FaithData associatedFaith, PlayerScript invoker = null)
		{
			foreach (var member in associatedFaith.FaithMembers)
			{
				if (member.IsDeadOrGhost) continue;
				member.playerHealth.FullyHeal();
				string text = new RichText("You have been blessed..").Bold().Color(RichTextColor.Yellow);
				Chat.AddExamineMsg(member.gameObject, text);
			}
		}
	}
}