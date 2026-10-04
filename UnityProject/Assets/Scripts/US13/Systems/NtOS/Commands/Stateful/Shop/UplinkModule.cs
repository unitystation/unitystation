using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;
using US13.Systems.NtOS.Core;
using US13.UI.Items.PDA;
using Random = UnityEngine.Random;

namespace US13.Systems.NtOS.Commands.Stateful.Shop
{
	public class UplinkModule : MonoBehaviour, INtOSModule
	{
		//do not give uplinks a direct command name, as they're accessed discretely from other commands.
		public string CommandName { get; set; } = string.Empty;

		public bool HasAccessedBefore = false;

		public async UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			if (args.Length <= 0)
			{
				output.AppendLine("Error: Attack vector prevented.");
				return;
			}
			await UniTask.WaitForEndOfFrame(); // wait a frame so its registered in the history.
			var ourEntry = callingDevice.History.Find(x => x.Id == id);
			ourEntry.Locked = true; //lock it so we don't delete ourselves
			callingDevice.ServerClearHistory();
			if (HasAccessedBefore == false)
			{
				await ShowIntro(output);
				HasAccessedBefore = true;
				CommandName = args[0];
				ShowSyndicateLogo(output);
				output.AppendLine($"Program registered: {CommandName}.\n Use this command to access your uplink from now on.");
				ourEntry.Locked = false;
				return;
			}
			HandleShopTree(args, callingDevice, output);
			ourEntry.Locked = false;
		}

		private void HandleShopTree(string[] args, NtOS_Device  callingDevice, StringBuilder output)
		{
			if (args[0] == "catalogue")
			{
				output.AppendLine("Available Catalogue: ");
				var catalogueIndex = 0;
				foreach (UplinkCategory category in UplinkCategoryList.Instance.ItemCategoryList)
				{
					output.AppendLine($"{catalogueIndex}. {category.CategoryName}");
					var itemIndex = 0;
					foreach (UplinkItem item in category.ItemList)
					{
						output.AppendLine($"{catalogueIndex}.{itemIndex}. {item.Name} - {item.Cost}");
						itemIndex++;
					}
					catalogueIndex++;
				}
			}
			else
			{
				output.AppendLine($"Could not process argument: {args[0]}");
			}

			if (args[0] == "buy")
			{
				if (args.Length < 3)
				{
					output.AppendLine("Error: Not enough arguments.\n Usage: buy [catalogue number] [item number]\n" +
					                  "You can find more info on available catalogues and items using the catalogue command.");
					return;
				}

				try
				{
					var possibleCatagory = UplinkCategoryList.Instance.ItemCategoryList[int.Parse(args[1])];
					var possibleItemCategory = possibleCatagory.ItemList[int.Parse(args[2])];

				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
					output.AppendLine($"NT-OS KERNEL PANIC..\n RECOVERING..\n LOGDUMP:\n{e.ToString()}");
				}
			}
		}

		private void ShowSyndicateLogo(StringBuilder output)
		{
			output.AppendLine(
				"                                              \n    ██████████████████████████████████████    \n  ████   ████████████████████████████   ████  \n  ████     █████              █████     ████  \n  ████   ███████               ██████   ████  \n  ████   ████████      ██████████████   ████  \n  ████   ██████████      ████████████   ████  \n  ████   █         █       █        █   ████  \n  ████   █████████████       ████████   ████  \n  ████   ███████████████      ███████   ████  \n  ████   ███████               ██████   ████  \n  ████     ██████              ████     ████  \n  ████   ████████████████████████████   ████  \n    ██████████████████████████████████████    \n                                              \n");
		}

		private async UniTask ShowIntro(StringBuilder output)
		{
			output.AppendLine("ERROR: BUFFER-OVERFLOW DETECTED.");
			await UniTask.WaitForSeconds(0.4f);
			for (int i = 0; i < 12; i++)
			{
				output.Append($"ox{Random.Range(0,225)}ERRORox{Random.Range(0,225)}");
				await UniTask.WaitForSeconds(0.2f);
			}
			output.AppendLine("Access Granted.");
			await UniTask.WaitForSeconds(0.2f);
		}

		public string HelpDoc(NtOS_Device callingDevice)
		{
			if (HasAccessedBefore == false)
			{
				return "Corrupted Binaries detected. Please remove this module.";
			}
			StringBuilder guide = new StringBuilder();
			return guide.ToString();
		}
	}
}