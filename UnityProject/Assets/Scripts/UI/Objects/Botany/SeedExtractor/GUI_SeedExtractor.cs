using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Botany;
using Items.Botany;

namespace UI.Objects.Botany
{
	public class GUI_SeedExtractor : NetTab
	{
		[SerializeField]
		private bool allowSell = true;
		[SerializeField]
		private float cooldownTimer = 2f;

		private SeedExtractor seedExtractor;
		private Dictionary<string, List<SeedPacket>> seedExtractorContent = new Dictionary<string, List<SeedPacket>>();
		[SerializeField]
		private EmptyItemList seedTypeList = null;
		[SerializeField]
		private EmptyItemList seedList = null;
		[SerializeField]
		private NetButton backButton = null;
		[SerializeField]
		private NetText_label title = null;
		[SerializeField]
		private NetPrefabImage icon = null;
		[SerializeField]
		private string deniedMessage = "Bzzt.";
		private bool inited = false;
		private string selectedSeedType;

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			seedExtractor = Provider.GetComponent<SeedExtractor>();
			//hullColor.SetValue = ColorUtility.ToHtmlStringRGB(vendor.HullColor);
			inited = true;
			GenerateContentList();
			UpdateList();
			seedExtractor.UpdateEvent.AddListener(UpdateItems);
		}

		/// <summary>
		/// Generates dictionary of seed packets used to populate GUI
		/// </summary>
		private void GenerateContentList()
		{
			if (CustomNetworkManager.IsServer == false) return;

			seedExtractorContent = new Dictionary<string, List<SeedPacket>>();
			foreach (var seedPacket in seedExtractor.seedPackets)
			{
				if (seedExtractorContent.ContainsKey(seedPacket.name))
				{
					seedExtractorContent[seedPacket.name].Add(seedPacket);
				}
				else
				{
					seedExtractorContent.Add(seedPacket.name, new List<SeedPacket> { seedPacket });
				}
			}
		}

		public override void OnEnable()
		{
			base.OnEnable();
			if (CustomNetworkManager.IsServer == false || inited == false) return;

			UpdateList();
			allowSell = true;
		}

		/// <summary>
		/// Updates GUI with changes to SeedExtractor
		/// Could be optimized to only add/remove rather then rebuild the entire list
		/// </summary>
		private void UpdateItems()
		{
			GenerateContentList();
			UpdateList();
		}

		/// <summary>
		/// Updates GUI list from content list
		/// Shows selected seeds or if none are selected then list of types
		/// </summary>
		private void UpdateList()
		{
			seedTypeList.Clear();
			seedList.Clear();

			if (selectedSeedType == null)
			{
				seedTypeList.AddItems(seedExtractorContent.Count);
				for (int i = 0; i < seedExtractorContent.Count; i++)
				{
					SeedExtractorItemTypeEntry item = seedTypeList.Entries[i] as SeedExtractorItemTypeEntry;
					var seedList = seedExtractorContent.Values.ToList();
					item.SetItem(seedList[i], this);
				}
			}
			else
			{
				//If the entry doesn't exist go back to the main menu
				if (!seedExtractorContent.ContainsKey(selectedSeedType))
				{
					Back();
					return;
				}
				var selectedSeeds = seedExtractorContent[selectedSeedType];
				seedList.AddItems(selectedSeeds.Count);
				for (int i = 0; i < selectedSeeds.Count; i++)
				{
					SeedExtractorItemEntry item = seedList.Entries[i] as SeedExtractorItemEntry;
					item.SetItem(selectedSeeds[i], this);
				}
			}
		}

		/// <summary>
		/// Moves GUI into list of specific seeds
		/// </summary>
		/// <param name="seedType">Name of selected seed type</param>
		public void SelectSeedType(string seedType)
		{
			title.MasterSetValue(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(seedType.ToLower()));
			icon.MasterSetValue(seedType);
			backButton.enabled = true;
			selectedSeedType = seedType;
			UpdateList();
		}

		/// <summary>
		/// Moves GUI back to list of seed types
		/// </summary>
		public void Back()
		{
			title.MasterSetValue("Select Seed Packet");
			icon.MasterSetValue(null);
			backButton.enabled = false;
			selectedSeedType = null;
			UpdateList();
		}

		/// <summary>
		/// Spawn seed packet in world
		/// </summary>
		public void DispenseSeedPacket(SeedPacket seedPacket)
		{
			if (!CanSell()) return;
			seedExtractor.DispenseSeedPacket(seedPacket);

			allowSell = false;
			StartCoroutine(VendorInputCoolDown());
		}

		/// <summary>
		/// Checks if cooldown has completed, shows message if it hasn't
		/// </summary>
		/// <returns>True if cooldown completed</returns>
		private bool CanSell()
		{
			if (allowSell)
			{
				return true;
			}
			SendToChat(deniedMessage);
			return false;
		}

		private void SendToChat(string messageToSend)
		{
			Chat.AddActionMsgToChat(seedExtractor.gameObject, messageToSend);
		}

		private IEnumerator VendorInputCoolDown()
		{
			yield return WaitFor.Seconds(cooldownTimer);
			allowSell = true;
		}
	}
}
