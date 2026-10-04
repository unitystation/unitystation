using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Systems.NtOS.Core;

namespace US13.Systems.NtOS.Commands.Stateful.Info
{
	public class NotesModule : MonoBehaviour, INtOSModule
	{
		public string CommandName { get; set; } = "notes";

		[SerializeField] private string notes = "";
		private TimeSpan timeSinceLastNote = TimeSpan.Zero;

		public UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			if (args.Length == 0)
			{
				output.AppendLine($"Last edited: {timeSinceLastNote.Minutes} minutes ago.\n\n{notes}");
				return UniTask.CompletedTask;
			}

			if (args.Length == 1 && args[0] == "clear")
			{
				output.AppendLine("Notes cleared.");
				notes = string.Empty;
				timeSinceLastNote = TimeSpan.Zero;
			}
			else
			{
				foreach (var text in args)
				{
					output.Append($" {text}");
				}
				notes = output.ToString();
				timeSinceLastNote = TimeSpan.Zero;
			}
			return UniTask.CompletedTask;
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			return "Stores notes via this module's memory storage.";
		}
	}
}