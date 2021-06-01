using System.Collections.Generic;
using System.Linq;
using System.Text;
using Systems.Access;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using WebSocketSharp;

namespace Items.Cards
{
	/// <summary>
	/// New ID Cards component. Implements new access type and methods.
	/// </summary>
	public class IDCardV2: NetworkBehaviour, IServerInventoryMove, IServerSpawn, IInteractable<HandActivate>
	{
		#region Inspector
		[Tooltip("Sprite to use when the card is a normal card")]
		[SerializeField]
		private Sprite standardSprite = null;

		[Tooltip("Sprite to use when the card is a captain's card")]
		[SerializeField]
		private Sprite captainSprite = null;

		[Tooltip("Sprite to use when the card is a command-tier card")]
		[SerializeField]
		private Sprite commandSprite = null;

		[Tooltip("Custom job name this ID would get once initialized.")]
		[SerializeField]
		private string initialCustomJobName = default;

		[Tooltip("For cards added via map editor and set their initial IDCardType here. This will only work" +
		         "if there are entries in ManuallyAddedAccess list")]
		[SerializeField]
		private IDCardType manuallyAssignCardType = IDCardType.standard;

		[Tooltip("If true, will initialize itself with the correct access list, name, job, etc...based on the" +
		         " first player whose inventory it is added to. Used for initial loadout.")]
		[SerializeField]
		private bool autoInitOnPickup = false;
		#endregion

		[SyncVar(hook = nameof(SyncIDCardType))]
		private IDCardType idCardType;

		[SyncVar(hook = nameof(SyncJobType))]
		private JobType jobType;

		[SyncVar(hook = nameof(SyncName))]
		private string registeredName;

		[SyncVar(hook = nameof(SyncCustomJobName))]
		private string customJobName;


		private bool initialized;

		public JobType JobType => jobType;
		public Occupation Occupation => OccupationList.Instance.Get(JobType);
		public string RegisteredName => registeredName;
		public AccessHolder AccessHolder => accessHolder;


		//To switch the card sprites when the type changes
		private SpriteRenderer spriteRenderer;
		private Pickupable pickupable;
		private AccessHolder accessHolder;

		private ItemAttributesV2 itemAttributes;

		private void Awake()
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			pickupable = GetComponent<Pickupable>();
			itemAttributes = GetComponent<ItemAttributesV2>();
			accessHolder = GetComponent<AccessHolder>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (accessHolder.Access.Count != 0)
			{
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
			if (initialized || autoInitOnPickup == false) return;
			//auto init if being added to a player's inventory
			if (info.ToPlayer == null) return;

			initialized = true;
			//these checks protect against NRE when spawning a player who has no mind, like dummy
			var ps = info.ToPlayer.GetComponent<PlayerScript>();
			if (ps == null) return;
			var mind = ps.mind;
			if (mind == null) return;
			var occupation = mind.occupation;
			if (occupation == null) return;
			var charSettings = ps.characterSettings;
			jobType = occupation.JobType;

			switch (jobType)
			{
				case JobType.CAPTAIN:
					Initialize(IDCardType.captain, jobType, occupation.Access, occupation.MinimalAccess, charSettings.Name);
					break;
				case JobType.HOP:
				case JobType.HOS:
				case JobType.CMO:
				case JobType.RD:
				case JobType.CHIEF_ENGINEER:
					Initialize(IDCardType.command, jobType, occupation.Access, occupation.MinimalAccess, charSettings.Name);
					break;
				default:
					Initialize(IDCardType.standard, jobType, occupation.Access, occupation.MinimalAccess, charSettings.Name);
					break;
			}
		}

		/// <summary>
		/// Configures the ID card with the specified settings
		/// </summary>
		/// <param name="newIdType">type of card</param>
		/// <param name="newJobType">job on the card</param>
		/// <param name="access">what the card can access</param>
		/// <param name="minimalAccess">what the card can access on lowpop rounds</param>
		/// <param name="newName">name listed on card</param>
		private void Initialize(IDCardType newIdType, JobType newJobType, List<AccessDefinitions> access,
			List<AccessDefinitions> minimalAccess, string newName)
		{
			//Set all the synced properties for the card
			SyncName(registeredName, newName);
			SyncJobType(jobType, newJobType);
			if (initialCustomJobName.IsNullOrEmpty() == false)
			{
				SyncCustomJobName(string.Empty, customJobName);
			}
			SyncIDCardType(newIdType, newIdType);
			ServerAddAccess(access, minimalAccess.Any() ? minimalAccess : access);
		}

		public void ServerAddAccess(List<AccessDefinitions> access, List<AccessDefinitions> minimalAccess = null)
		{
			accessHolder.ServerSetAccess(access);
			accessHolder.ServerSetMinimalAccess(minimalAccess ?? access);
		}

		public void SyncName(string oldName, string newName)
		{
			registeredName = newName;
			RenameIDObject();
		}

		public void SyncCustomJobName(string oldName, string newName)
		{
			customJobName = newName;
			RenameIDObject();
		}

		public void SyncJobType(JobType oldJobType, JobType newJobType)
		{
			jobType = newJobType;
			RenameIDObject();
		}

		private void RenameIDObject()
		{
			var newName = new StringBuilder();
			if (registeredName.IsNullOrEmpty() == false)
			{
				newName.AppendFormat("{0}'s ", registeredName);
			}

			newName.Append(" ID Card");

			if (customJobName.IsNullOrEmpty() == false)
			{
				newName.AppendFormat(" {0}", customJobName);
			}
			else if (Occupation != null)
			{
				newName.AppendFormat(" {0}", Occupation.DisplayName);
			}
			else
			{
				newName.AppendFormat(" {0}", jobType.ToString());
			}

			itemAttributes.ServerSetArticleName(newName.ToString());
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
			if (clear)
			{
				accessHolder.ServerClearAccess();
				accessHolder.ServerClearMinimalAccess();
			}
			if (grantDefaultAccess)
			{
				ServerAddAccess(occupation.Access);
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

		/// <summary>
		/// Public interface to set a custom job name for this id.
		/// </summary>
		/// <param name="newName"></param>

		[Server]
		public void ServerSetCustomJobName(string newName)
		{
			SyncCustomJobName(customJobName, newName);
		}
	}
}