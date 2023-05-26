using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminCommands;
using DatabaseAPI;
using Audio.Containers;
using System.Threading.Tasks;
using AddressableReferences;


namespace AdminTools
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
