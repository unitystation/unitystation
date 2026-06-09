using System.Linq;
using US13.Messages.Client.Admin;
using US13.ScriptableObjects;
using Util;

namespace US13.UI.Systems.AdminTools
{
	public class SpecialJobRespawnTab: RespawnTab
	{
		public override void RequestRespawn()
		{
			var value = dropdown.value;
			var occupation = value != 0
				? SOAdminJobsList.Instance.SpecialJobs.ToList()[value - 1]
				//Just a safe value in case for whatever reason user didn't select a job and can click the button
				: SOAdminJobsList.Instance.SpecialJobs.PickRandom();

			RequestRespawnPlayer.SendSpecialRespawn(PlayerEntry.PlayerData.uid, occupation);
		}
	}
}
