using System;
using System.Linq;
using AdminTools;
using DatabaseAPI;
using Messages.Client.Admin;
using UnityEngine;

namespace UI.AdminTools
{
	public class JobRespawnTab: RespawnTab
	{
		public override void RequestRespawn()
		{

			var value = dropdown.value;
			var occupation = value != 0
				? OccupationList.Instance.Occupations.ToList()[value - 1]
				//Just a safe value in case for whatever reason user didn't select a job and can click the button
				: OccupationList.Instance.Occupations.PickRandom();

			RequestRespawnPlayer.SendNormalRespawn(
				ServerData.UserID,
				PlayerList.Instance.AdminToken,
				PlayerEntry.PlayerData.uid,
				occupation);
		}
	}
}