using System.Linq;
using Messages.Client.Admin;
using ScriptableObjects;


namespace UI.AdminTools
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
