using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using AccessType;

/// <summary>
/// ID card properties
/// </summary>
public class IDCard : NetworkBehaviour
{
    public int MiningPoints = 0; //For redeeming at mining equipment vendors
                                 //The actual list of access allowed set via the server and synced to all clients
    public SyncListInt accessSyncList = new SyncListInt();
    [Tooltip("This is used to place ID cards via map editor and then setting their initial access type")]
    public List<Access> ManuallyAddedAccess = new List<Access>();
    [Tooltip("For cards added via map editor and set their initial IDCardType here. This will only work" +
             "if there are entries in ManuallyAddedAccess list")]
    public IDCardType ManuallyAssignCardType;
    [SyncVar(hook = "SyncName")]
    public string RegisteredName;
    [SyncVar(hook = "SyncJobType")]
    public int jobTypeInt;
    //What type of card? (standard, command, captain, emag etc)
    [SyncVar(hook = "SyncIDCardType")]
    public int idCardTypeInt;
    public JobType GetJobType { get { return (JobType)jobTypeInt; } }
    public IDCardType GetIdCardType { get { return (IDCardType)idCardTypeInt; } }
    private bool isInit = false;

    //To switch the card sprites when the type changes
    public SpriteRenderer spriteRenderer;
    public Sprite standardSprite;
    public Sprite commandSprite;
    public Sprite captainSprite;

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

    void InitCard()
    {
        if (isInit) return;

        isInit = true;
        accessSyncList.Callback = SyncAccess;

        //This will add the access from ManuallyAddedAccess list
        if (isServer)
        {
            if (ManuallyAddedAccess.Count > 0)
            {
                AddAccessList(ManuallyAddedAccess);
                idCardTypeInt = (int)ManuallyAssignCardType;
            }
        }
    }
    //Sync all of the current in game ID's throughout the map with new players
    IEnumerator WaitForLoad()
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
            if (!accessSyncList.Contains((int)accessToBeAdded[i]))
            {
                accessSyncList.Add((int)accessToBeAdded[i]);
            }
        }
    }

    [Server]
    public void RemoveAccessList(List<Access> accessToBeRemoved)
    {
        for (int i = 0; i < accessToBeRemoved.Count; i++)
        {
            if (accessSyncList.Contains((int)accessToBeRemoved[i]))
            {
                accessSyncList.Remove((int)accessToBeRemoved[i]);
            }
        }
    }

    public void SyncAccess(SyncListInt.Operation op, int index)
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

		if (MiningPoints > 0) {
			message = "There's " + MiningPoints + " mining equipment redemption points loaded onto this card.";
		}
		else {
			message = "This is " + RegisteredName + "'s ID card\nThey are the " + GetJobType.ToString() + " of the station!";
		}

		UI.UIManager.Chat.AddChatEvent(new ChatEvent(message, ChatChannel.Examine));
	}
}
