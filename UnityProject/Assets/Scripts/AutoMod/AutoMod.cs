using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Telepathy;

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

		//Monitors the time between interactions and limits it by the min cool down time
		private static Dictionary<GameObject, DateTime> chatCoolDown = new Dictionary<GameObject, DateTime>();
		private static Dictionary<GameObject, int> playersMaxMessaging = new Dictionary<GameObject, int>();
		private static double minCoolDown = 1f;
		private static int maxMessages = 5;

		private AutoModConfig loadedConfig;

		private static string AutoModConfigPath =>
			Path.Combine(Application.streamingAssetsPath, "admin", "automodconfig.json");

		private void Start()
		{
			LoadConfig();
		}

		private void SaveConfig()
		{
			if (loadedConfig == null) return;

			File.WriteAllText(AutoModConfigPath, JsonUtility.ToJson(loadedConfig));
		}

		private void LoadConfig()
		{
			if (File.Exists(AutoModConfigPath))
			{
				var config = File.ReadAllText(AutoModConfigPath);
				loadedConfig = JsonUtility.FromJson<AutoModConfig>(config);
				Logger.Log("Successfully loaded Auto Mod config");
			}
		}

		void Update()
		{
			if (!IsEnabled()) return;
			MonitorEnvironment();
		}

		void MonitorEnvironment()
		{
			if (Common.allocationAttackQueue.Count > 0)
			{
				ProcessAllocationAttack(Common.allocationAttackQueue.Dequeue());
			}
		}

		public static void ProcessAllocationAttack(string ipAddress)
		{
			if (!Instance.loadedConfig.enableAllocationProtection) return;
			if (Application.platform == RuntimePlatform.LinuxPlayer)
			{
				Logger.Log($"Auto mod has taken steps to protect against an allocation attack from {ipAddress}");
				ProcessStartInfo processInfo = new ProcessStartInfo();
				processInfo.FileName = "ufw";
				processInfo.Arguments = $"insert 1 deny from {ipAddress} to any";
				processInfo.CreateNoWindow = true;
				processInfo.UseShellExecute = false;
				Process.Start(processInfo);
			}
		}

		public static string ProcessChatServer(ConnectedPlayer player, string message)
		{
			if (player == null || !IsEnabled()) return message;

			return message;
		}

		private static bool IsEnabled()
		{
			if (Instance == null || !GameData.IsHeadlessServer || Instance.loadedConfig == null) return false;
			if (!Instance.loadedConfig.enableAutoMod) return false;
			return true;
		}
	}

	[Serializable]
	public class AutoModConfig
	{
		public bool enableAutoMod;
		public bool enableAllocationProtection;
		public bool enableSpamProtection;
		public bool enableBadWordFilter;
		public bool enableRdmNotification;
		public bool enablePlasmaReleaseNotification;
	}
}