using System.Collections;
using System.Collections.Generic;
using AccessType;
using UI;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     ID card properties
/// </summary>
public class IDCard : NetworkBehaviour
{
	//The actual list of access allowed set via the server and synced to all clients
	public SyncListInt accessSyncList = new SyncListInt();

	public Sprite captainSprite;
	public Sprite commandSprite;

	//What type of card? (standard, command, captain, emag etc)
	[SyncVar(hook = "SyncIDCardType")] public int idCardTypeInt;

	private bool isInit;

	[SyncVar(hook = "SyncJobType")] public int jobTypeInt;

	[Tooltip("This is used to place ID cards via map editor and then setting their initial access type")]
	public List<Access> ManuallyAddedAccess = new List<Access>();

	[Tooltip("For cards added via map editor and set their initial IDCardType here. This will only work" +
	         "if there are entries in ManuallyAddedAccess list")] public IDCardType ManuallyAssignCardType;

	public int MiningPoints; //For redeeming at mining equipment vendors

	[SyncVar(hook = "SyncName")] public string RegisteredName;

	//To switch the card sprites when the type changes
	public SpriteRenderer spriteRenderer;

	public Sprite standardSprite;

	public JobType GetJobType => (JobType) jobTypeInt;

	public IDCardType GetIdCardType => (IDCardType) idCardTypeInt;

	public override void OnStartServer()
	{
		InitCard();
		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		InitCard();
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	private void InitCard()
	{
		if (isInit)
		{
			return;
		}

		isInit = true;
		accessSyncList.Callback = SyncAccess;

		//This will add the access from ManuallyAddedAccess list
		if (isServer)
		{
			if (ManuallyAddedAccess.Count > 0)
			{
				AddAccessList(ManuallyAddedAccess);
				idCardTypeInt = (int) ManuallyAssignCardType;
			}
		}
	}

	//Sync all of the current in game ID's throughout the map with new players
	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(3f);
		SyncName(RegisteredName);
		SyncJobType(jobTypeInt);
		SyncIDCardType(idCardTypeInt);
	}

	[Server]
	public void AddAccessList(List<Access> accessToBeAdded)
	{
		for (int i = 0; i < accessToBeAdded.Count; i++)
		{
			if (!accessSyncList.Contains((int) accessToBeAdded[i]))
			{
				accessSyncList.Add((int) accessToBeAdded[i]);
			}
		}
	}

	[Server]
	public void RemoveAccessList(List<Access> accessToBeRemoved)
	{
		for (int i = 0; i < accessToBeRemoved.Count; i++)
		{
			if (accessSyncList.Contains((int) accessToBeRemoved[i]))
			{
				accessSyncList.Remove((int) accessToBeRemoved[i]);
			}
		}
	}

	public void SyncAccess(SyncList<int>.Operation op, int index)
	{
		//Do anything special when the synclist changes on the client
	}

	public void SyncName(string name)
	{
		RegisteredName = name;
	}

	public void SyncJobType(int jobType)
	{
		jobTypeInt = jobType;
	}

	public void SyncIDCardType(int cardType)
	{
		idCardTypeInt = cardType;
		IDCardType cType = GetIdCardType;
		switch (cType)
		{
			case IDCardType.standard:
				spriteRenderer.sprite = standardSprite;
				break;
			case IDCardType.command:
				spriteRenderer.sprite = commandSprite;
				break;
			case IDCardType.captain:
				spriteRenderer.sprite = captainSprite;
				break;
		}
	}

	public void OnExamine()
	{
		string message = "";

		if (MiningPoints > 0)
		{
			message = "There's " + MiningPoints + " mining equipment redemption points loaded onto this card.";
		}
		else
		{
			message = "This is " + RegisteredName + "'s ID card\nThey are the " + GetJobType + " of the station!";
		}

		UIManager.Chat.AddChatEvent(new ChatEvent(message, ChatChannel.Examine));
	}
}