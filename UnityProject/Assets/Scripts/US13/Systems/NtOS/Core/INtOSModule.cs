using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using US13.UI.Core.Net.Elements;

namespace US13.Systems.NtOS.Core
{
	public interface INtOSModule
	{
		public string CommandName { get; set; }
		public UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output);
		public string HelpDoc(NtOS_Device callingDevice);
	}
}