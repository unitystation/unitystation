using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Systems.NtOS.Core;
using US13.UI.Systems.Jobs;

namespace US13.Systems.NtOS.Commands.Stateful.Info
{
	public class CrewManifestModule : MonoBehaviour, INtOSModule
	{
		public string CommandName { get; set; } = "CrewManifest";

		public UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			if (CrewManifestManager.Instance == null)
			{
				output.AppendLine("Records not found.");
				return UniTask.CompletedTask;
			}
			var players = CrewManifestManager.Instance.CrewManifest;

			output.AppendLine("╔══════════════════════════════════════╗");
			output.AppendLine("             CREW MANIFEST            ");
			output.AppendLine("╠══════════════════════════════════════╣");

			int crewCount = 0;

			foreach (var p in players)
			{
				crewCount++;
				var name = p.Name;
				var occupation = p.JobType;
				output.AppendLine($" {crewCount,2}. {name,-22} {occupation,-12} ");
			}

			if (crewCount == 0)
			{
				output.AppendLine("║       No crew members found.      ║");
			}

			output.AppendLine("╠══════════════════════════════════════╣");
			output.AppendLine($"║ Total Crew Present: {crewCount,-30}║");
			output.AppendLine("╚══════════════════════════════════════╝");

			return UniTask.CompletedTask;
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Returns a list of crew members that are relevant to your organization.";
		}
	}
}