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
	/// Lets Admins play sounds
	/// </summary>
	public class AdminGlobalSound : AdminGlobalAudio
	{
		public override void PlayAudio(string index) //send sound to audio manager
		{
			AdminCommandsManager.Instance.CmdPlaySound(index);
		}
	}
}
