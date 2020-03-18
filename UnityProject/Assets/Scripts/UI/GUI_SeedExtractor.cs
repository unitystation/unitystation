using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
	private NetColorChanger hullColor = null;
	[SerializeField]
	private NetButton backButton = null;
	[SerializeField]
	private NetLabel title = null;
	[SerializeField]
	private NetPrefabImage icon = null;
	[SerializeField]
	private string deniedMessage = "Bzzt.";
	private bool inited = false;
	private string selectedSeedType;

	private void Start()
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}
	}

	protected override void InitServer()
	{
		StartCoroutine(WaitForProvider());
	}

	IEnumerator WaitForProvider()
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

	private void GenerateContentList()
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

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
		if (!CustomNetworkManager.Instance._isServer || !inited)
		{
			return;
		}
		UpdateList();
		allowSell = true;
	}

	/// <summary>
	/// Updates GUI with changes to SeedExtractor
	/// Could be optimized to only add/remove rather then rebuild the entire list
	/// </summary>
	public void UpdateItems()
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

	public void SelectSeedType(string seedType)
	{
		title.SetValue = seedType;
		icon.SetValue = seedType;
		backButton.enabled = true;
		selectedSeedType = seedType;
		UpdateList();
	}

	public void Back()
	{
		title.SetValue = "Select Seed Packet";
		icon.SetValue = null;
		backButton.enabled = false;
		selectedSeedType = null;
		UpdateList();
	}

	public void DispenseSeedPacket(SeedPacket item)
	{
		if (item == null || seedExtractor == null) return;
		SeedPacket itemToSpawn = null;
		foreach (var vendorItem in seedExtractorContent[item.name])
		{
			if (vendorItem == item)
			{
				itemToSpawn = item;
				break;
			}
		}

		if (!CanSell(itemToSpawn))
		{
			return;
		}

		Vector3 spawnPos = seedExtractor.gameObject.RegisterTile().WorldPositionServer;
		CustomNetTransform netTransform = itemToSpawn.GetComponent<CustomNetTransform>();
		netTransform.AppearAtPosition(spawnPos);
		netTransform.AppearAtPositionServer(spawnPos);
		//var spawnedItem = Spawn.ServerPrefab(itemToSpawn.gameObject, spawnPos, seedExtractor.transform.parent).GameObject;
		//something went wrong trying to spawn the item
		//if (spawnedItem == null) return;

		seedExtractorContent[itemToSpawn.name].Remove(itemToSpawn);
		if(seedExtractorContent[itemToSpawn.name].Count == 0)
		{
			seedExtractorContent.Remove(itemToSpawn.name);
		}

		SendToChat($"{itemToSpawn.gameObject.ExpensiveName()} was dispensed from the seed extractor");


		UpdateList();
		allowSell = false;
		StartCoroutine(VendorInputCoolDown());
	}

	private bool CanSell(SeedPacket itemToSpawn)
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
		Chat.AddLocalMsgToChat(messageToSend, seedExtractor.transform.position, seedExtractor?.gameObject);
	}

	private IEnumerator VendorInputCoolDown()
	{
		yield return WaitFor.Seconds(cooldownTimer);
		allowSell = true;
	}
}
