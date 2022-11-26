using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Managers;
using UnityEngine;
using Mirror;
using Systems.Clearance;
using UnityEngine.Serialization;
using WebSocketSharp;

/// <summary>
///     ID card properties
/// </summary>
public class IDCard : NetworkBehaviour, IServerInventoryMove, IServerSpawn, IInteractable<HandActivate>, IExaminable, IClearanceProvider
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

	[Tooltip("This is used to place ID cards via map editor and then setting their initial clearance type")]
	[SerializeField]
	private List<Clearance> manuallyAddedClearance = new List<Clearance>();

	[Tooltip("For cards added via map editor and set their initial IDCardType here. This will only work" +
	         "if there are entries in ManuallyAddedAccess list")]
	[FormerlySerializedAs("ManuallyAssignCardType")]
	[SerializeField]
	private IDCardType manuallyAssignCardType = IDCardType.standard;

	[Tooltip("If true, will initialize itself with the correct access list, name, job, etc...based on the" +
	         " first player whose inventory it is added to. Used for initial loadout.")]
	[SerializeField]
	private bool autoInitOnPickup = false;
	private bool initialized;

	public JobType JobType => jobType;
	public Occupation Occupation => OccupationList.Instance.Get(JobType);
	public string RegisteredName => registeredName;

	[SyncVar(hook = nameof(SyncIDCardType))]
	private IDCardType idCardType;

	[SyncVar(hook = nameof(SyncJobType))]
	private JobType jobType;

	[SyncVar(hook = nameof(SyncJobTitle))]
	private string jobTitle = "";

	[SyncVar(hook = nameof(SyncName))]
	private string registeredName;

	public int[] currencies = new int[(int)CurrencyType.Total];

	//The actual list of access allowed set via the server and synced to all clients
	private readonly SyncListClearance clearanceSyncList = new SyncListClearance();

	//To switch the card sprites when the type changes
	private SpriteRenderer spriteRenderer;
	private Pickupable pickupable;

	private ItemAttributesV2 itemAttributes;

	public IEnumerable<Clearance> GetClearance => clearanceSyncList;

	private void Awake()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		pickupable = GetComponent<Pickupable>();
		itemAttributes = GetComponent<ItemAttributesV2>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		//This will add the access from ManuallyAddedAccess list
		if (manuallyAddedClearance.Count > 0)
		{
			ServerAddAccess(manuallyAddedClearance);
			SyncIDCardType(idCardType, manuallyAssignCardType);
		}

		initialized = false;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer,
			$"You show the {itemAttributes.ArticleName}",
			$"{interaction.Performer.ExpensiveName()} shows you: {itemAttributes.ArticleName}");
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{

		if (info.MovedObject.gameObject != gameObject || initialized || !autoInitOnPickup || info.ToPlayer == null)
			return;

		//auto init if being added to a player's inventory
		initialized = true;

		//these checks protect against NRE when spawning a player who has no mind, like dummy
		var inventory = info.ToPlayer.GetComponent<DynamicItemStorage>();
		if (inventory == null)
			return;



		var occupation = inventory.InitialisedWithOccupation;
		if (occupation == null)
			return;

		jobType = occupation.JobType;

		var clearanceToGive = GameManager.Instance.CentComm.IsLowPop
			? occupation.IssuedLowPopClearance
			: occupation.IssuedClearance;

		if (clearanceToGive == occupation.IssuedLowPopClearance && clearanceToGive.Count == 0)
			clearanceToGive = occupation.IssuedClearance; //(Max) : Incase we forgot to set it up in the SO aka you're lazy like me

		if (jobType == JobType.CAPTAIN)
		{
			Initialize(IDCardType.captain, jobType, clearanceToGive, inventory.gameObject.name);
		}
		else if (jobType == JobType.HOP || jobType == JobType.HOS || jobType == JobType.CMO || jobType == JobType.RD ||
		         jobType == JobType.CHIEF_ENGINEER)
		{
			Initialize(IDCardType.command, jobType, clearanceToGive, inventory.gameObject.name);
		}
		else
		{
			Initialize(IDCardType.standard, jobType, clearanceToGive, inventory.gameObject.name);
		}
	}


	/// <summary>
	/// Configures the ID card with the specified settings
	/// </summary>
	/// <param name="idCardType">type of card</param>
	/// <param name="jobType">job on the card</param>
	/// <param name="allowedAccess">what the card can access</param>
	/// <param name="name">name listed on card</param>
	private void Initialize(IDCardType idCardType, JobType newJobType, List<Clearance> allowedAccess, string name)
	{
		//Set all the synced properties for the card
		SyncName(registeredName, name);
		SyncJobType(jobType, newJobType);
		SyncIDCardType(idCardType, idCardType);
		ServerAddAccess(allowedAccess);
	}

	public void SyncName(string oldName, string newName)
	{
		registeredName = newName;
		RenameIDObject();
	}

	public void SyncJobType(JobType oldJobType, JobType newJobType)
	{
		jobType = newJobType;
		RenameIDObject();
	}

	public void SyncJobTitle(string oldJobTitle, string newJobTitle)
	{
		jobTitle = newJobTitle;
		RenameIDObject();
	}

	private void RenameIDObject()
	{
		var newName = "";
		if (!RegisteredName.IsNullOrEmpty())
		{
			newName += $"{RegisteredName}'s ";
		}
		newName += "ID Card";

		if (jobTitle.IsNullOrEmpty() == false)
		{
			newName += $" ({jobTitle})";
		}
		else if (Occupation != null)
		{
			newName += $" ({Occupation.DisplayName})";
		}

		itemAttributes.ServerSetArticleName(newName);
	}

	public void SyncIDCardType(IDCardType oldCardType, IDCardType cardType)
	{
		idCardType = cardType;
		if (idCardType == IDCardType.standard)
		{
			spriteRenderer.sprite = standardSprite;
		}
		else if (idCardType == IDCardType.command)
		{
			spriteRenderer.sprite = commandSprite;
		}
		else if (idCardType == IDCardType.captain)
		{
			spriteRenderer.sprite = captainSprite;
		}
		pickupable.RefreshUISlotImage();
	}

	//TODO Move over to use ClearanceCheckable to do this check
	/// <summary>
	/// Checks if this id card has the indicated clearance.
	/// </summary>
	/// <param name="access"></param>
	/// <returns></returns>
	public bool HasAccess(Clearance access)
	{
		return clearanceSyncList.Contains(access);
	}

	//TODO Move over to use ClearanceCheckable to do this che
	/// <summary>
	/// Checks if this id card has the indicated access from a list of clearances.
	/// </summary>
	/// <param name="access"></param>
	/// <returns></returns>
	public bool HasAccess(List<Clearance> access)
	{
		foreach (var accessToCheck in access)
		{
			if (clearanceSyncList.Contains(accessToCheck)) return true;
		}
		return false;
	}

	public string GetJobTitle()
	{
		if (jobTitle.IsNullOrEmpty())
		{
			var occupation = Occupation;
			return occupation != null ? occupation.DisplayName : "";
		}

		return jobTitle;
	}

	/// <summary>
	/// Removes the indicated clearance from this IDCard
	/// </summary>
	[Server]
	public void ServerRemoveAccess(Clearance access)
	{
		if (!HasAccess(access)) return;
		clearanceSyncList.Remove(access);
		netIdentity.isDirty = true;
	}

	/// <summary>
	/// Adds the indicated clearance to this IDCard
	/// </summary>
	[Server]
	public void ServerAddAccess(Clearance access)
	{
		if (HasAccess(access)) return;
		clearanceSyncList.Add(access);
		netIdentity.isDirty = true;
	}

	[Server]
	public void ReplaceAccessWithLowPopVersion()
	{
		ServerAddAccess(Occupation.IssuedLowPopClearance);
	}

	/// <summary>
	/// Adds the indicated clearance to this id card
	/// </summary>
	/// <param name="accessToBeAdded"></param>
	[Server]
	public void ServerAddAccess(IEnumerable<Clearance> accessToBeAdded)
	{
		foreach (var access in accessToBeAdded)
		{
			ServerAddAccess(access);
		}
	}

	/// <summary>
	/// Removes the indicated clearance from this id card
	/// </summary>
	/// <param name="accessToBeAdded"></param>
	[Server]
	public void ServerRemoveAccess(IEnumerable<Clearance> accessToBeRemoved)
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
	/// default clearance afforded by this occupation, if false, only changes
	/// the occupation</param>
	/// <param name="clear">if true, removes the existing access of this card
	/// before granting them the occupation.</param>
	[Server]
	public void ServerChangeOccupation(Occupation occupation, bool grantDefaultAccess = true, bool clear = true)
	{
		jobTitle = "";

		if (clear)
		{
			clearanceSyncList.Clear();
			netIdentity.isDirty = true;
		}

		if (grantDefaultAccess)
		{
			ServerAddAccess(occupation.IssuedClearance);
		}

		SyncJobType(jobType, occupation.JobType);
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

	[Server]
	public void ServerSetJobTitle(string newJobTitle)
	{
		if (string.IsNullOrEmpty(newJobTitle))
		{
			newJobTitle = "";
		}

		SyncJobTitle(jobTitle, newJobTitle);
	}

	public string Examine(Vector3 pos)
	{
		return $"The account linked to the ID belongs to '{registeredName}' and reports a balance of {currencies[(int)CurrencyType.Credits]} credits. " +
		       $"The labor budget reports an allowance of {currencies[(int)CurrencyType.LaborPoints]} points.";
	}
}

public enum CurrencyType
{
	Credits,
	LaborPoints,
	Total
}