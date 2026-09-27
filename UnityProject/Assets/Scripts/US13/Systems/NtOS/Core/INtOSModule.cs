using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace US13.Systems.NtOS.Core
{
	public interface INtOSModule
	{
		public string CommandName { get; set; }
		public UniTask<string> Execute(List<string> args, NtOS_Device callingDevice);
		public string HelpDoc(NtOS_Device callingDevice);
	}
}