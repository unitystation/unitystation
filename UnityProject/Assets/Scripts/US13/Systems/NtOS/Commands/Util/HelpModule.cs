using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using US13.Systems.NtOS.Core;

namespace US13.Systems.NtOS.Commands.Util
{
	public class HelpModule : INtOSModule
	{
		public string CommandName { get; set; }

		public async UniTask<string> Execute(List<string> args, NtOS_Device callingDevice)
		{
			StringBuilder sb = new StringBuilder();
			if (args.Count == 0)
			{
				foreach (var module in callingDevice.Modules)
				{
					sb.AppendLine($"{module.CommandName}: {HelpDoc(callingDevice)}");
				}
			}
			else
			{
				var module = callingDevice.Modules.Find(x => x.CommandName == CommandName);
				if (module != null)
				{
					sb.AppendLine($"{module.CommandName}: {HelpDoc(callingDevice)}");
				}
				else
				{
					sb.AppendLine($"Cannot find command '{CommandName}'");
				}
			}
			return sb.ToString();
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Shows all commands available on this device.";
		}
	}
}