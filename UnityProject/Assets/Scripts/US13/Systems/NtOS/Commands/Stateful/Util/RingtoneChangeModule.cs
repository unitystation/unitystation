using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Items.PDA;
using US13.Systems.NtOS.Core;

namespace US13.Systems.NtOS.Commands.Stateful.Util
{
	public class RingtoneChangeModule : MonoBehaviour, INtOSModule
	{
		public string CommandName { get; set; } = "ringtone";

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
			pda.SetRingtone(args[0]);
			output.AppendLine($"PDA ringtone changed to {args[0]}.");
			return UniTask.CompletedTask;
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Changes the device's ringtone.\n Usage: ringtone my_new_sound";
		}
	}
}