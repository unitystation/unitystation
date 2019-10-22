using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
	[SyncVar(hook = nameof(SyncIDCardType))] public int idCardTypeInt;

	private bool isInit;

	[SyncVar(hook = nameof(SyncJobType))] public int jobTypeInt;

	[Tooltip("This is used to place ID cards via map editor and then setting their initial access type")]
	public List<Access> ManuallyAddedAccess = new List<Access>();

	[Tooltip("For cards added via map editor and set their initial IDCardType here. This will only work" +
	         "if there are entries in ManuallyAddedAccess list")] public IDCardType ManuallyAssignCardType;

	public int MiningPoints; //For redeeming at mining equipment vendors

	[SyncVar(hook = nameof(SyncName))] public string RegisteredName;

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

	/// <summary>
	/// Configures the ID card with the specified settings
	/// </summary>
	/// <param name="idCardType">type of card</param>
	/// <param name="jobType">job on the card</param>
	/// <param name="allowedAccess">what the card can access</param>
	/// <param name="name">name listed on card</param>
	public void Initialize(IDCardType idCardType, JobType jobType, List<Access> allowedAccess, string name)
	{
		//Set all the synced properties for the card
		RegisteredName = name;
		jobTypeInt = (int) jobType;
		idCardTypeInt = (int) idCardType;
		AddAccessList(allowedAccess);
	}

	private void InitCard()
	{
		if (isInit)
		{
			return;
		}

		isInit = true;
		accessSyncList.Callback += SyncAccess;

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
		yield return WaitFor.Seconds(3f);
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

	public void SyncAccess(SyncList<int>.Operation op, int index, int item)
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

		Chat.AddExamineMsgToClient(message);
	}
}