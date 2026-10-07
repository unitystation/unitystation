using System.Text;
using Cysharp.Threading.Tasks;
using US13.Systems.NtOS.Core;

namespace US13.Systems.NtOS.Commands.Simple.Util
{
	public class ClearHistoryModule : INtOSModule
	{
		public string CommandName { get; set; } = "clear";
		public UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			callingDevice.ServerClearHistory();
			return UniTask.CompletedTask;
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Clears history.";
		}
	}
}