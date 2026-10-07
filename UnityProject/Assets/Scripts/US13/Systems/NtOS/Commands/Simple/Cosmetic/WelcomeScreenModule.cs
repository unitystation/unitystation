using System.Text;
using Cysharp.Threading.Tasks;
using US13.Systems.NtOS.Core;

namespace US13.Systems.NtOS.Commands.Simple.Cosmetic
{
	public class WelcomeScreenModule : INtOSModule
	{
		public string CommandName { get; set; } = "WelcomeScreen";

		public async UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			output.AppendLine("-- Welcome to NtOS --");
			output.AppendLine("");
			output.AppendLine("                                        \n    +++++++++++++++++++++++++++++++    \n   +++      +    ++===+   +=======+++  \n  +-=+++    +     +++=+   +=========+  \n  +====+++  +       +++   +=========+  \n  +======++++   +    ++   ++++======+  \n  +=========+   +++       +  ++÷====+  \n  ++========+   +=++      +   +++==++  \n   +++======+   +==+++    +     +=++   \n     ++++++++++++++++++++++++++++++    \n                                       \n");
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Hello World";
		}
	}
}