using System;
using System.Linq;
using Messages.Client.Admin;


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

			RequestRespawnPlayer.SendNormalRespawn(PlayerEntry.PlayerData.uid, occupation);
		}
	}
}
