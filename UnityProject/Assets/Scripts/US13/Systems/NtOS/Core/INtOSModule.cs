using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using US13.UI.Core.Net.Elements;

namespace US13.Systems.NtOS.Core
{
	public interface INtOSModule
	{
		public string CommandName { get; set; }
		public UniTask<string> Execute(NetText_label ownedLabel, string[] args, NtOS_Device callingDevice);
		public string HelpDoc(NtOS_Device callingDevice);
	}
}