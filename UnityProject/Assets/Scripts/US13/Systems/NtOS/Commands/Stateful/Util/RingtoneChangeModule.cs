using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Items.Devices;
using US13.Items.PDA;
using US13.Systems.NtOS.Commands.Stateful.Shop;
using US13.Systems.NtOS.Core;

namespace US13.Systems.NtOS.Commands.Stateful.Util
{
	public class RingtoneChangeModule : MonoBehaviour, INtOSModule
	{
		public string CommandName { get; set; } = "ringtone";

		public bool CheckForUplink = true;

		public UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			if (args.Length <= 0)
			{
				output.AppendLine("No arguments provided.");
				return UniTask.CompletedTask;
			}

			if (callingDevice.TryGetComponent<PDALogic>(out var pda) == false)
			{
				output.AppendLine("PDA not detected attached. This module can only run on PDAs.");
				return UniTask.CompletedTask;
			}
			if (TryCheckForUplinkVulnerability(callingDevice, args)) return UniTask.CompletedTask;

			pda.SetRingtone(args[0]);
			output.AppendLine($"PDA ringtone changed to {args[0]}.");
			TryCheckForUplinkVulnerability(callingDevice, args);
			return UniTask.CompletedTask;
		}

		private bool TryCheckForUplinkVulnerability(NtOS_Device callingDevice, string[] args)
		{
			if (CheckForUplink == false) return false;
			if (callingDevice.TryGetComponent<Uplink>(out var uplink) == false) return false;
			if (string.Equals(args[0], uplink.UplinkUnlockCode, StringComparison.OrdinalIgnoreCase) == false) return false;
			if (gameObject.TryGetComponent<UplinkModule>(out var uplinkModule))
			{
				callingDevice.ServerRunProcess(uplinkModule, args);
			}
			else
			{
				var m = gameObject.AddComponent<UplinkModule>();
				callingDevice.ServerRunProcess(m, args);
			}
			return true;
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Changes the device's ringtone.\n Usage: ringtone my_new_sound";
		}
	}
}