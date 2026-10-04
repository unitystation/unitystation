using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Items.PDA;
using US13.Player;
using US13.Systems.NtOS.Core;

namespace US13.Systems.NtOS.Commands.Stateful.Util
{
	public class LightToggleModule : MonoBehaviour, INtOSModule
	{
		public string CommandName { get; set; } = "flashlight";

		public UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			if (callingDevice.TryGetComponent<PDALogic>(out var pda))
			{
				pda.ToggleFlashlight();
				return UniTask.CompletedTask;
			}
			if (callingDevice.TryGetComponent<ItemLightControl>(out var itemLightControl) == false)
			{
				output.AppendLine("No lights found");
				return UniTask.CompletedTask;
			}
			itemLightControl.Toggle(!itemLightControl.IsOn);
			output.AppendLine($"Light has been turned {(itemLightControl.IsOn ? "on" : "off")}.");
			return UniTask.CompletedTask;
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Toggles the PDA's flashlight.";
		}
	}
}