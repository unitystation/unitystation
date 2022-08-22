using System.Linq;
using Messages.Client.Admin;
using ScriptableObjects;


namespace UI.AdminTools
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
