using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AdminTools
{
	//A serverside optional auto moderator to help make
	//server admin work easier. Only works in headless mode
	public class AutoMod : MonoBehaviour
	{
		private static AutoMod autoMod;

		public static AutoMod Instance
		{
			get
			{
				if (autoMod == null)
				{
					autoMod = FindObjectOfType<AutoMod>();
				}
				return autoMod;
			}
		}

		private AutoModConfig loadedConfig;

		private static string AutoModConfigPath =>
			Path.Combine(Application.streamingAssetsPath, "admin", "automodconfig.json");

	}

	[Serializable]
	public class AutoModConfig
	{
		public bool enableAutoMod;
		public bool enableAllocationProtection;
		public bool enableSpamAutoBan;
		public bool enableBadWordBan;
		public bool enableRdmNotification;
		public bool enablePlasmaReleaseNotification;
	}
}