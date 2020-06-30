using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
///     ID card properties
/// </summary>
public class IDCard : NetworkBehaviour, IServerInventoryMove, IServerSpawn, IExaminable
{

	[Tooltip("Sprite to use when the card is a normal card")]
	[SerializeField]
	private Sprite standardSprite = null;

	[Tooltip("Sprite to use when the card is a captain's card")]
	[SerializeField]
	private Sprite captainSprite = null;

	[Tooltip("Sprite to use when the card is a command-tier card")]
	[SerializeField]
	private Sprite commandSprite = null;

	[Tooltip("This is used to place ID cards via map editor and then setting their initial access type")]
	[FormerlySerializedAs("ManuallyAddedAccess")]
	[SerializeField]
	private List<Access> manuallyAddedAccess = new List<Access>();

	[Tooltip("For cards added via map editor and set their initial IDCardType here. This will only work" +
	         "if there are entries in ManuallyAddedAccess list")]
	[FormerlySerializedAs("ManuallyAssignCardType")]
	[SerializeField]
	private IDCardType manuallyAssignCardType = IDCardType.standard;

	[Tooltip("If true, will initialize itself with the correct access list, name, job, etc...based on the" +
	         " first player whose inventory it is added to. Used for initial loadout.")]
	[SerializeField]
	private bool autoInitOnPickup = false;
	private bool hasAutoInit;


	public JobType JobType => jobType;
	public Occupation Occupation => OccupationList.Instance.Get(JobType);
	public IDCardType IDCardType => idCardType;
	public string RegisteredName => registeredName;


	[SyncVar(hook = nameof(SyncIDCardType))]
	private IDCardType idCardType;

	[SyncVar(hook = nameof(SyncJobType))]
	private JobType jobType;

	[SyncVar(hook = nameof(SyncName))]
	private string registeredName;


	//The actual list of access allowed set via the server and synced to all clients
	private SyncListInt accessSyncList = new SyncListInt();

	private bool isInit;
	//To switch the card sprites when the type changes
	private SpriteRenderer spriteRenderer;
	private Pickupable pickupable;

	private void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (spriteRenderer != null) return;
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		pickupable = GetComponent<Pickupable>();
	}

	public override void OnStartServer()
	{
		EnsureInit();
		InitCard();
	}

	public override void OnStartClient()
	{
		EnsureInit();
		InitCard();
		StartCoroutine(WaitForLoad());
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		hasAutoInit = false;
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		if (!hasAutoInit && autoInitOnPickup)
		{
			//auto init if being added to a player's inventory
			if (info.ToPlayer != null)
			{
				hasAutoInit = true;
				//these checks protect against NRE when spawning a player who has no mind, like dummy
				var ps = info.ToPlayer.GetComponent<PlayerScript>();
				if (ps == null) return;
				var mind = ps.mind;
				if (mind == null) return;
				var occupation = mind.occupation;
				if (occupation == null) return;
				var charSettings = ps.characterSettings;
				var jobType = occupation.JobType;
				if (jobType == JobType.CAPTAIN)
				{
					Initialize(IDCardType.captain, jobType, occupation.AllowedAccess, charSettings.Name);
				}
				else if (jobType == JobType.HOP || jobType == JobType.HOS || jobType == JobType.CMO || jobType == JobType.RD ||
				         jobType == JobType.CHIEF_ENGINEER)
				{
					Initialize(IDCardType.command, jobType, occupation.AllowedAccess, charSettings.Name);
				}
				else
				{
					Initialize(IDCardType.standard, jobType, occupation.AllowedAccess, charSettings.Name);
				}
			}
		}
	}


	/// <summary>
	/// Configures the ID card with the specified settings
	/// </summary>
	/// <param name="idCardType">type of card</param>
	/// <param name="jobType">job on the card</param>
	/// <param name="allowedAccess">what the card can access</param>
	/// <param name="name">name listed on card</param>
	private void Initialize(IDCardType idCardType, JobType jobType, List<Access> allowedAccess, string name)
	{
		//Set all the synced properties for the card
		SyncName(registeredName, name);
		this.jobType = jobType;
		SyncIDCardType(idCardType, idCardType);
		ServerAddAccess(allowedAccess);
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
			if (manuallyAddedAccess.Count > 0)
			{
				ServerAddAccess(manuallyAddedAccess);
				idCardType = manuallyAssignCardType;
			}
		}
	}

	//Sync all of the current in game ID's throughout the map with new players
	private IEnumerator WaitForLoad()
	{
		yield return WaitFor.Seconds(3f);
		SyncName(registeredName, registeredName);
		SyncJobType(jobType, jobType);
		SyncIDCardType(idCardType, idCardType);
	}

	public void SyncAccess(SyncList<int>.Operation op, int index, int oldItem, int newItem)
	{
		//Do anything special when the synclist changes on the client
	}

	public void SyncName(string oldName, string name)
	{
		EnsureInit();
		registeredName = name;
	}

	public void SyncJobType(JobType oldJobType, JobType jobType)
	{
		EnsureInit();
		this.jobType = jobType;
	}

	public void SyncIDCardType(IDCardType oldCardType, IDCardType cardType)
	{
		EnsureInit();
		idCardType = cardType;
		IDCardType cType = IDCardType;
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

		pickupable.RefreshUISlotImage();

	}

	// When examine is triggered by the server on a player gameobj (by a shift click from another player)
	// the target's Equipment component returns ID info based on presence of ID card in "viewable" slot (id and hands).
	// When ID card itself is examined, it should return full text.
	public string Examine(Vector3 worldPos)
	{
		return "This is the ID card of " + registeredName + ", station " + JobType + ".";
	}



	/// <summary>
	/// Checks if this id card has the indicated access.
	/// </summary>
	/// <param name="access"></param>
	/// <returns></returns>
	public bool HasAccess(Access access)
	{
		return accessSyncList.Contains((int) access);
	}

	/// <summary>
	/// Removes the indicated access from this IDCard
	/// </summary>
	[Server]
	public void ServerRemoveAccess(Access access)
	{
		if (!HasAccess(access)) return;
		accessSyncList.Remove((int)access);
	}

	/// <summary>
	/// Adds the indicated access to this IDCard
	/// </summary>
	[Server]
	public void ServerAddAccess(Access access)
	{
		if (HasAccess(access)) return;
		accessSyncList.Add((int)access);
	}

	/// <summary>
	/// Adds the indicated access to this id card
	/// </summary>
	/// <param name="accessToBeAdded"></param>
	[Server]
	public void ServerAddAccess(IEnumerable<Access> accessToBeAdded)
	{
		foreach (var access in accessToBeAdded)
		{
			ServerAddAccess(access);
		}
	}

	/// <summary>
	/// Removes the indicated access from this id card
	/// </summary>
	/// <param name="accessToBeAdded"></param>
	[Server]
	public void ServerRemoveAccess(IEnumerable<Access> accessToBeRemoved)
	{
		foreach (var access in accessToBeRemoved)
		{
			ServerRemoveAccess(access);
		}
	}

	/// <summary>
	/// Changes the card's occupation to the new occupation, granting them
	/// the default access and clearing any existing access if indicated.
	/// </summary>
	/// <param name="occupation"></param>
	/// <param name="grantDefaultAccess">if true, grants them the
	/// default access afforded by this occupation, if false, only changes
	/// the occupation</param>
	/// <param name="clear">if true, removes the existing access of this card
	/// before granting them the occupation.</param>
	[Server]
	public void ServerChangeOccupation(Occupation occupation, bool grantDefaultAccess = true, bool clear = true)
	{
		if (clear) accessSyncList.Clear();
		if (grantDefaultAccess)
		{
			ServerAddAccess(occupation.AllowedAccess);
		}

		jobType = occupation.JobType;
	}

	/// <summary>
	/// Sets the registered name of this card to the indicated value
	/// </summary>
	/// <param name="newName"></param>
	[Server]
	public void ServerSetRegisteredName(string newName)
	{
		SyncName(registeredName, newName);
	}
}