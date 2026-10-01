using System.Text;
using Cysharp.Threading.Tasks;
using US13.Managers;
using US13.Systems.NtOS.Core;

namespace US13.Systems.NtOS.Commands.Info
{
	public class CrewManifestModule : INtOSModule
	{
		public string CommandName { get; set; } = "CrewManifest";

		public UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			var players = PlayerList.Instance.GetAllPlayers();
			var listOfPlayers = new StringBuilder();
			foreach (var p in players)
			{
				if (p.Mind != null && p.Mind.occupation != null && p.Mind.occupation.IsCrewmember)
				{
					listOfPlayers.AppendLine($"{p.Mind.CurrentCharacterSettings.Name} - {p.Mind.occupation.DisplayName}");
				}
			}
			output.AppendLine(listOfPlayers.ToString());
			return UniTask.CompletedTask;
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Returns a list of crew members that are relavent to your orgnization.";
		}
	}
}