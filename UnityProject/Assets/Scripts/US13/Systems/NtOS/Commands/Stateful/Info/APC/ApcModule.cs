using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Systems.NtOS.Core;
using Util;

namespace US13.Systems.NtOS.Commands.Stateful.Info.APC
{
	public class ApcModule : MonoBehaviour, INtOSModule
	{
		public string CommandName { get; set; } = "apc";

		public const string INFO_COMMAND = "info";

		public UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			if (callingDevice.TryGetComponent<Objects.Engineering.APC>(out var apcDevice) == false)
			{
				output.AppendLine("No APC found");
				return UniTask.CompletedTask;
			}
			if (args.Length <= 0)
			{
				output.AppendLine($"{HelpDoc(callingDevice)}");
				return UniTask.CompletedTask;
			}
			HandleArgs(args, output, apcDevice);
			return UniTask.CompletedTask;
		}

		private void HandleArgs(string[] args, StringBuilder output, Objects.Engineering.APC apcDevice)
		{
			switch (args[0])
			{
				case INFO_COMMAND:
					ShowApcStatus(apcDevice, output);
					break;
			}
		}

		private void ShowApcStatus(Objects.Engineering.APC apcDevice, StringBuilder output)
		{
			output.AppendLine($"APC STATUS  [{apcDevice.State}]");
			output.AppendLine($"Voltage    : {apcDevice.Voltage}V");
			output.AppendLine($"Current    : {apcDevice.Current}A");
			output.AppendLine($"Charge     : {apcDevice.CalculateChargePercentage()*100}%");
			output.AppendLine();

			output.AppendLine($"BATTERIES ({apcDevice.ConnectedDepartmentBatteries.Count})");
			foreach (var battery in apcDevice.ConnectedDepartmentBatteries)
			{
				var module = battery.BatterySupplyingModule;

				output.AppendLine(
					$"- {battery.gameObject.ExpensiveName()} [{(battery.isOn ? "ON" : "OFF")}]\n" +
					$"out:{module.OutputLevel} pull:{module.PullingWatts}W charge:{module.ChargingWatts}W");
			}

			output.AppendLine();
			output.AppendLine($"DEVICES ({apcDevice.ConnectedDevices.Count})");

			foreach (var poweredDevice in apcDevice.ConnectedDevices)
			{
				var distance = Vector2.Distance(
					apcDevice.gameObject.AssumedWorldPosServer(),
					poweredDevice.gameObject.AssumedWorldPosServer());

				output.AppendLine(
					$"- {poweredDevice.gameObject.ExpensiveName()} " +
					$"(d:{distance:0.0}m)\n" +
					$"v:{poweredDevice.Voltage}V " +
					$"w:{poweredDevice.Wattusage}W " +
					$"r:{poweredDevice.Resistance}Ω");
			}
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "";
		}
	}
}