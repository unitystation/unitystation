using System.Linq;
using US13.Messages.Client.Admin;
using US13.ScriptableObjects;
using Util;

namespace US13.UI.Systems.AdminTools
{
	public class AntagRespawnTab: RespawnTab
	{
		public override void RequestRespawn()
		{
			var value = dropdown.value;
			var antag = value != 0
				? SOAdminJobsList.Instance.Antags.ToList()[value - 1]
				//Just a safe value in case for whatever reason user didn't select a job and can click the button
				: SOAdminJobsList.Instance.Antags.PickRandom();

			RequestRespawnPlayer.SendAntagRespawn(PlayerEntry.PlayerData.uid, antag);
		}
	}
}
