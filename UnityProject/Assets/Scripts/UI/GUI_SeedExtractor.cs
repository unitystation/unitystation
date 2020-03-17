using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_SeedExtractor : NetTab
{
	[SerializeField]
	private bool allowSell = true;
	[SerializeField]
	private float cooldownTimer = 2f;

	private SeedExtractor seedExtractor;
	private List<GameObject> seedExtractorContent = new List<GameObject>();
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

		seedExtractorContent = new List<GameObject>();
		foreach(var seedPacket in seedExtractor.seedPackets)
		{
			seedExtractorContent.Add(seedPacket);
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
			SeedExtractorItemEntry item = itemList.Entries[i] as SeedExtractorItemEntry;
			item.SetItem(seedExtractorContent[i], this);
		}
	}

	public void DispenseSeedPacket(GameObject item)
	{
		if (item == null || seedExtractor == null) return;
		GameObject itemToSpawn = null;
		foreach (var vendorItem in seedExtractorContent)
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

		seedExtractorContent.Remove(itemToSpawn);

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
