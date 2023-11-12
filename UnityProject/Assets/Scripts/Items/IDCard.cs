using System.Collections.Generic;
using System.Linq;
using Items;
using Logs;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using Systems.Clearance;
using WebSocketSharp;

/// <summary>
///     ID card properties
/// </summary>
public class IDCard : NetworkBehaviour, IServerInventoryMove, IServerSpawn, IInteractable<HandActivate>, IExaminable
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

	[Tooltip("If true, will initialize itself with the correct access list, name, job, etc...based on the" +
	         " first player whose inventory it is added to. Used for initial loadout.")]
	[SerializeField]
	private bool autoInitOnPickup = false;

	[Tooltip("If set, it will be assigned to the ID card on spawn. Useful for storytelling.")]
	[SerializeField]
	[HideIf(nameof(autoInitOnPickup))]
	private string initialName = "";

	[Tooltip("If set, it will be assigned to the ID card on spawn. Useful for storytelling.")]
	[SerializeField]
	[HideIf(nameof(autoInitOnPickup))]
	private string initialJobTitle = "";

	private bool initialized;
	public BasicClearanceSource ClearanceSource { get; private set; }

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



	// FIXME: move currencies to their own component. Labor points and credits don't really have much in common and should be handled on their own components.
	public int[] currencies = new int[(int)CurrencyType.Total];

	//To switch the card sprites when the type changes
	private SpriteRenderer spriteRenderer;
	private Pickupable pickupable;

	private ItemAttributesV2 itemAttributes;

	private bool HasInitialNameOrTitle => string.IsNullOrEmpty(initialName) == false || string.IsNullOrEmpty(initialJobTitle) == false;


	private void Awake()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		pickupable = GetComponent<Pickupable>();
		itemAttributes = GetComponent<ItemAttributesV2>();
		ClearanceSource = GetComponent<BasicClearanceSource>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		initialized = false;
		if (autoInitOnPickup && HasInitialNameOrTitle)
		{
			Loggy.LogWarning($"{gameObject.name} has autoInitOnPickup and initialName or initialJobTitle set. These values will be overriden when a player picks it up!", Category.Objects);
		}

		if (string.IsNullOrEmpty(initialName) == false)
		{
			SyncName("", initialName);
		}

		if (string.IsNullOrEmpty(initialJobTitle) == false)
		{
			SyncJobTitle("", initialJobTitle);
		}
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

		var issuedClearance = occupation.IssuedClearance;
		var lowPopClearance = occupation.IssuedLowPopClearance;

		switch (jobType)
		{
			case JobType.CAPTAIN:
				Initialize(IDCardType.captain, jobType, issuedClearance, lowPopClearance, inventory.gameObject.name);
				break;
			case JobType.HOP or JobType.HOS or JobType.CMO or JobType.RD or JobType.CHIEF_ENGINEER:
				Initialize(IDCardType.command, jobType, issuedClearance, lowPopClearance, inventory.gameObject.name);
				break;
			default:
				Initialize(IDCardType.standard, jobType, issuedClearance, lowPopClearance, inventory.gameObject.name);
				break;
		}
	}

	/// <summary>
	/// Initialize the ID card with the specified parameters. Normally called after picking up an auto-init ID card.
	/// </summary>
	/// <param name="newIDCardType"></param>
	/// <param name="newJobType"></param>
	/// <param name="issuedClearance"></param>
	/// <param name="issuedLowPopClearance"></param>
	/// <param name="characterName"></param>
	private void Initialize(IDCardType newIDCardType, JobType newJobType, IEnumerable<Clearance> issuedClearance, IEnumerable<Clearance> issuedLowPopClearance, string characterName)
	{
		SyncName(registeredName, characterName);
		SyncJobType(jobType, newJobType);
		SyncIDCardType(newIDCardType, newIDCardType);
		if (ClearanceSource == null)
		{
			Loggy.LogError($"IDCard {gameObject.name} has no IClearanceSource component, cannot set clearance!", Category.Objects);
			return;
		}

		ClearanceSource.ServerSetClearance(issuedClearance);
		ClearanceSource.ServerSetLowPopClearance(issuedLowPopClearance);
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
			ClearanceSource.ServerClearClearance();
		}

		if (grantDefaultAccess)
		{
			ClearanceSource.ServerSetClearance(occupation.IssuedClearance);
			ClearanceSource.ServerSetLowPopClearance(occupation.IssuedLowPopClearance.Any() ? occupation.IssuedLowPopClearance : occupation.IssuedClearance);
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