using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using US13.Systems.NtOS.Core;
using US13.UI.Core.Net.Elements;

namespace US13.Systems.NtOS.Commands.Util
{
	public class HelpModule : INtOSModule
	{
		public string CommandName { get; set; }

		public async UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			if (args.Length == 0)
			{
				foreach (var module in callingDevice.Modules)
				{
					output.AppendLine($"{module.CommandName}: {HelpDoc(callingDevice)}");
					await UniTask.WaitForSeconds(0.5f);
				}
			}
			else
			{
				INtOSModule module = callingDevice.Modules.Find(x => x.CommandName == CommandName);
				output.AppendLine(module != null ?
					$"{module.CommandName}: {HelpDoc(callingDevice)}"
					:
					$"Cannot find command '{CommandName}'");
			}
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Shows all commands available on this device.";
		}
	}
}