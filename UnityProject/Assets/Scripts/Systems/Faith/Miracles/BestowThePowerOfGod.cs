﻿using UnityEngine;
using Util.Independent.FluentRichText;
using Color = Util.Independent.FluentRichText.Color;

namespace Systems.Faith.Miracles
{
	public class BestowThePowerOfGod : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Bestow the power of god";
		[SerializeField] private string faithMiracleDesc = "Give all faith leaders a golden weapon that holds the power of god.";
		[SerializeField] private SpriteDataSO miracleIcon;

		[SerializeField] private GameObject goldenRevolver;

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

		public int MiracleCost { get; set; } = 690;
		public void DoMiracle()
		{
			string msg = new RichText().Color(Color.Yellow).Italic().Add("You feel... Power..");
			foreach (var dong in PlayerList.Instance.GetAlivePlayers())
			{
				var weapon = Spawn.ServerPrefab(goldenRevolver, dong.GameObject.AssumedWorldPosServer());
				foreach (var handSlot in dong.Script.Equipment.ItemStorage.GetNamedItemSlots(NamedSlot.hands))
				{
					if (handSlot.IsOccupied) continue;
					Inventory.ServerAdd(weapon.GameObject, handSlot, ReplacementStrategy.DropOther);
					Chat.AddExamineMsg(dong.GameObject, msg);
					break;
				}
			}
		}
	}
}