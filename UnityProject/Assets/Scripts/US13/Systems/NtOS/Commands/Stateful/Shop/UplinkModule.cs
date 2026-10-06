using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;
using US13.Items.Devices;
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

		public const string BUY_COMMAND = "tc-buy";
		public const string MENU_COMMAND = "tc-catalogue";
		public const string BALANCE_COMMAND = "tc";
		public const string LOCK_COMMAND = "tc-lock";

		public async UniTask Execute(int id, string[] args, NtOS_Device callingDevice, StringBuilder output)
		{
			if (args.Length <= 0 || callingDevice.TryGetComponent<Uplink>(out var uplinkModule) == false)
			{
				output.AppendLine("Error: Attack vector prevented.");
				return;
			}
			await UniTask.WaitForEndOfFrame(); // wait a frame so its registered in the history.
			var ourEntry = callingDevice.History.Find(x => x.Id == id);
			ourEntry.Locked = true; //lock it so we don't delete ourselves
			if (HasAccessedBefore == false)
			{
				callingDevice.ServerClearHistory();
				await ShowIntro(output);
				HasAccessedBefore = true;
				CommandName = args[0];
				ShowSyndicateLogo(output);
				output.AppendLine($"Program registered: {CommandName}.\n Use this command to access your uplink from now on.");
				callingDevice.RegisterSpecificModule(this);
				ourEntry.Locked = false;
				uplinkModule.ControlUplinkLock(false);
				return;
			}
			else
			{
				ourEntry.Locked = false;
				HandleShopTree(args, callingDevice, output, uplinkModule);
			}
		}

		private void HandleShopTree(string[] args, NtOS_Device callingDevice, StringBuilder output, Uplink uplink)
		{
			switch (args[0])
			{
				case MENU_COMMAND:
					ShowCatalogue(output);
					return;
				case BUY_COMMAND:
					BuyLogic(args, output, uplink);
					return;
				case BALANCE_COMMAND:
					output.AppendLine($"TC: {uplink.UplinkTC}");
					break;
				case LOCK_COMMAND:
					uplink.ControlUplinkLock(true);
					callingDevice.UnRegisterSpecificModule(this);
					callingDevice.ServerClearHistory();
					HasAccessedBefore = false;
					break;
			}
		}

		private static void ShowCatalogue(StringBuilder output)
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

		private void BuyLogic(string[] args, StringBuilder output, Uplink uplink)
		{
			if (args.Length < 2)
			{
				output.AppendLine("Error: Not enough arguments.\n Usage: buy [catalogue number] [item number]\n" +
				                  "You can find more info on available catalogues and items using the catalogue command.");
				return;
			}

			try
			{
				if (int.TryParse(args[1], out var catagoryNumber) == false || int.TryParse(args[2], out var itemNumber) == false)
				{
					output.AppendLine("Error: wrong argument type.\n Usage: buy [catalogue *number*] [item *number*]");
					return;
				}
				catagoryNumber = Mathf.Clamp(catagoryNumber, 0, UplinkCategoryList.Instance.ItemCategoryList.Count - 1);
				UplinkCategory possibleCatagory = UplinkCategoryList.Instance.ItemCategoryList[catagoryNumber];

				itemNumber = Mathf.Clamp(itemNumber, 0, possibleCatagory.ItemList.Count - 1);
				UplinkItem possibleItem = possibleCatagory.ItemList[itemNumber];

				bool result = uplink.SpawnUplinkItem(possibleItem.Item, possibleItem.Cost);
				output.AppendLine(result
					? $"Item: {possibleItem.Name} - Purchase successful."
					: $"Item: {possibleItem.Name} - Purchase unsuccessful.");

				output.AppendLine($"Current Balance: {uplink.UplinkTC}");
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
				output.AppendLine($"NT-OS KERNEL PANIC..\n RECOVERING..\n LOGDUMP:\n{e}");
			}
		}

		private void ShowSyndicateLogo(StringBuilder output)
		{
			output.AppendLine(
				"                                       \n   777777777777777777777777777777777   \n  777   777777           777777   777  \n  777   777777            77777   777  \n  777   777777     777777777777   777  \n  777   77777777     7777777777   777  \n  777   7       7      7      7   777  \n  777   77777777777      777777   777  \n  777   7777777777777     77777   777  \n  777   777777            77777   777  \n  777   7777777           77777   777  \n   777777777777777777777777777777777   \n                                       \n");
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
				//should not appear in the help list before its first accessed, but just incase someone forces this
				//on an NT device.
				return "Corrupted Binaries detected. Please remove this module.";
			}
			StringBuilder guide = new StringBuilder();
			guide.AppendLine("available arguments:");
			guide.AppendLine($"- {BUY_COMMAND}: \nUsage: {BUY_COMMAND} [catalogue number] [item number]");
			guide.AppendLine($"- {MENU_COMMAND}: \n Shows all available catalogues and their items.");
			guide.AppendLine($"- {BALANCE_COMMAND}: \n Shows how many TeleCrystals are available in your balance.");
			guide.AppendLine($"- {LOCK_COMMAND}: \n uninstall this program from the NtOS registry.");
			return guide.ToString();
		}
	}
}