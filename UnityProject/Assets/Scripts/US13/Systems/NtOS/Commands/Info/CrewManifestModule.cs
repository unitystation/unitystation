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

			output.AppendLine("╔══════════════════════════════════════════════════╗");
			output.AppendLine("║                 CREW MANIFEST                   ║");
			output.AppendLine("╠══════════════════════════════════════════════════╣");

			int crewCount = 0;

			foreach (var p in players)
			{
				if (p.Mind == null || p.Mind.occupation == null || p.Mind.occupation.IsCrewmember == false) continue;

				crewCount++;
				var name = p.Mind.CurrentCharacterSettings.Name;
				var occupation = p.Mind.occupation.DisplayName;

				output.AppendLine($"║ {crewCount,2}. {name,-30} {occupation,-12} ║");
			}

			if (crewCount == 0)
			{
				output.AppendLine("║              No crew members found.             ║");
			}

			output.AppendLine("╠══════════════════════════════════════════════════╣");
			output.AppendLine($"║ Total Crew: {crewCount,-36}║");
			output.AppendLine("╚══════════════════════════════════════════════════╝");

			return UniTask.CompletedTask;
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Returns a list of crew members that are relevant to your organization.";
		}
	}
}