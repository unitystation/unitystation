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
	private Dictionary<string, List<GameObject>> seedExtractorContent = new Dictionary<string, List<GameObject>>();
	[SerializeField]
	private EmptyItemList itemList = null;
	[SerializeField]
	private NetColorChanger hullColor = null;
	[SerializeField]
	private string deniedMessage = "Bzzt.";
	private bool inited = false;

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

		seedExtractorContent = new Dictionary<string, List<GameObject>>();
		foreach (var seedPacket in seedExtractor.seedPackets)
		{
			if (seedExtractorContent.ContainsKey(seedPacket.name))
			{
				seedExtractorContent[seedPacket.name].Add(seedPacket);
			}
			else
			{
				seedExtractorContent.Add(seedPacket.name, new List<GameObject> { seedPacket });
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
	/// </summary>
	private void UpdateList()
	{
		itemList.Clear();
		itemList.AddItems(seedExtractorContent.Count);
		for (int i = 0; i < seedExtractorContent.Count; i++)
		{
			SeedExtractorItemTypeEntry item = itemList.Entries[i] as SeedExtractorItemTypeEntry;
			item.SetItem(seedExtractorContent.Values.ToList()[i], this);
		}
	}

	public void DispenseSeedPacket(GameObject item)
	{
		if (item == null || seedExtractor == null) return;
		GameObject itemToSpawn = null;
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
		var spawnedItem = Spawn.ServerPrefab(itemToSpawn, spawnPos, seedExtractor.transform.parent).GameObject;
		//something went wrong trying to spawn the item
		if (spawnedItem == null) return;

		seedExtractorContent[itemToSpawn.name].Remove(itemToSpawn);
		if(seedExtractorContent[itemToSpawn.name].Count == 0)
		{
			seedExtractorContent.Remove(itemToSpawn.name);
		}

		SendToChat($"{spawnedItem.ExpensiveName()} was dispensed from the seed extractor");


		UpdateList();
		allowSell = false;
		StartCoroutine(VendorInputCoolDown());
	}

	private bool CanSell(GameObject itemToSpawn)
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
