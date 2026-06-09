using US13.Managers;

namespace US13.UI.Systems.AdminTools
{
	/// <summary>
	/// Lets Admins play music
	/// </summary>
	public class AdminGlobalMusic : AdminGlobalAudio
	{
		public override void PlayAudio(string index) //send music to audio manager
		{
			AdminCommandsManager.Instance.CmdPlayMusic(index);
		}
	}
}
