using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using AddressableReferences;
using Items;

namespace Objects
{
	/// <summary>
	/// Allows closet to be opened / closed / locked
	/// </summary>
	public class ClosetControl : NetworkBehaviour, IServerSpawn, ICheckedInteractable<PositionalHandApply>, IRightClickable, IExaminable, IEscapable
	{
		// These sprite enums coincide with the sprite SOs set in SpriteHandler.
		public enum Door
		{
			Closed = 0,
			Opened = 1,
		}

		public enum Lock
		{
			NoLock = 0,
			Locked = 1,
			Unlocked = 2,
			Broken = 3,
		}

		public enum Weld
		{
			NotWelded = 0,
			Welded = 1,
		}

		private static readonly StandardProgressActionConfig ProgressConfig =
				new StandardProgressActionConfig(StandardProgressActionType.Escape);

		#region Inspector

		[Header("Settings")]
		[SerializeField]
		[Tooltip("Whether the closet is passable when open.")]
		private bool passableWhenOpen = true;

		[SerializeField]
		[Tooltip("Whether the closet can be locked.")]
		private bool isLockable = false;

		[SerializeField]
		[Tooltip("Whether the closet can be welded.")]
		private bool isWeldable = false;

		[SerializeField]
		[Tooltip("Time to breakout or resist out of closet.")]
		private float breakoutTime = 120f;

		[SerializeField]
		[Tooltip("Whether or not the lock sprite is hidden when the container is opened.")]
		private bool hideLockWhenOpened = true;

		// TODO: These next two fields should really be a part of... ObjectAttributes?
		[SerializeField]
		[Tooltip("Type of material to drop when destroyed.")]
		private GameObject matsOnDestroy = null;

		[SerializeField]
		[Tooltip("How much material to drop when destroyed.")]
		private int matsDroppedOnDestroy = 2;

		[Header("References")]
		[SerializeField] private SpriteHandler doorSpriteHandler;
		[SerializeField] private SpriteHandler lockSpritehandler;
		[SerializeField] private SpriteHandler weldSpriteHandler;

		[Header("Sounds")]
		[SerializeField]
		[Tooltip("Sound to play when opened.")]
		private AddressableAudioSource soundOnOpen = null;

		[SerializeField]
		[Tooltip("Sound to play when closed.")]
		private AddressableAudioSource soundOnClose = null;

		[Tooltip("Sound to play when emagged.")]
		[SerializeField]
		private AddressableAudioSource soundOnEmag = null;

		[SerializeField]
		[Tooltip("Sound when an escape attempt begins.")]
		private AddressableAudioSource soundOnEscapeAttempt = default;

		[SerializeField]
		[Tooltip("Sound when an escape attempt succeeds.")]
		private AddressableAudioSource soundOnEscape = null;

		#endregion

		// Components
		private RegisterObject registerObject;
		private ObjectAttributes attributes;
		private ObjectContainer container;
		private PushPull pushPull;
		private AccessRestrictions accessRestrictions;

		private static readonly float weldTime = 5.0f;

		private string closetName;

		[SyncVar(hook = nameof(SyncDoorState))]
		private Door doorState = Door.Closed;
		private Lock lockState;
		private Weld weldState = Weld.NotWelded;

		private Matrix Matrix => registerObject.Matrix;
		public bool IsOpen => doorState == Door.Opened;
		public bool IsLocked => lockState == Lock.Locked;
		public bool IsWelded => weldState == Weld.Welded;

		#region Lifecycle

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			attributes = GetComponent<ObjectAttributes>();
			container = GetComponent<ObjectContainer>();
			pushPull = GetComponent<PushPull>();
			accessRestrictions = GetComponent<AccessRestrictions>();

			lockState = isLockable ? Lock.Locked : Lock.NoLock;
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);

			//Fetch the items name to use in messages
			closetName = gameObject.ExpensiveName();
		}

		public override void OnStartClient()
		{
			SyncDoorState(doorState, doorState);
		}

		public virtual void OnSpawnServer(SpawnInfo info)
		{
			// Always spawn closed
			SyncDoorState(doorState, Door.Closed);

			// If this is a mapped spawn, stick any items mapped on top of us in
			if (info.SpawnType == SpawnType.Mapped)
			{
				CollectObjects();
			}
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			if (matsDroppedOnDestroy > 0)
			{
				Spawn.ServerPrefab(
						matsOnDestroy, registerObject.WorldPositionServer, transform.parent, count: matsDroppedOnDestroy,
						scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
			}
		}

		#endregion

		public void SetDoor(Door newState)
		{
			if (newState == doorState) return;

			doorState = newState;
			doorSpriteHandler.ChangeSprite((int) doorState);
			if (hideLockWhenOpened && lockState != Lock.NoLock)
			{
				lockSpritehandler.ChangeSprite((int) (IsOpen ? Lock.NoLock : lockState));
			}

			SoundManager.PlayNetworkedAtPos(IsOpen ? soundOnOpen : soundOnClose, registerObject.WorldPositionServer, sourceObj: gameObject);

			if (IsOpen)
			{
				ReleaseObjects();
			}
			else
			{
				CollectObjects();
			}
		}

		public void SetLock(Lock newState)
		{
			if (isLockable == false) return;

			lockState = newState;
			lockSpritehandler.ChangeSprite((int) lockState);
		}

		public void SetWeld(Weld newState)
		{
			if (isWeldable == false) return;

			weldState = newState;
			weldSpriteHandler.ChangeSprite((int) weldState);
		}

		public void BreakLock()
		{
			isLockable = false;
			lockState = Lock.Broken;
			lockSpritehandler.ChangeSprite((int) lockState);
		}

		public void CollectObjects()
		{
			container.GatherObjects();
		}

		public void ReleaseObjects()
		{
			container.RetrieveObjects();
		}

		public void EntityTryEscape(GameObject performer)
		{
			// First, try to just open the closet. Anything can do this.
			if (IsLocked == false && IsWelded == false)
			{
				SetDoor(Door.Opened);
				return;
			}

			// banging sound
			SoundManager.PlayNetworkedAtPos(soundOnEscapeAttempt, registerObject.WorldPositionServer, sourceObj: gameObject);

			// complex task involved
			if (performer.Player() == null) return;

			var bar = StandardProgressAction.Create(ProgressConfig, () =>
			{
				if (IsWelded)
				{
					// Remove the weld
					SetWeld(Weld.NotWelded);
				}

				if (IsLocked)
				{
					BreakLock();
				}

				SetDoor(Door.Opened);

				SoundManager.PlayNetworkedAtPos(soundOnEmag, registerObject.WorldPositionServer, sourceObj: gameObject);
				Chat.AddActionMsgToChat(performer,
						$"You successfully break out of the {closetName}.",
						$"{performer.ExpensiveName()} emerges from the {closetName}!");
			});

			bar.ServerStartProgress(registerObject, breakoutTime, performer);

			SoundManager.PlayNetworkedAtPos(soundOnEscape, registerObject.WorldPositionServer, sourceObj: gameObject);
			Chat.AddActionMsgToChat(performer,
					$"You begin breaking out of the {closetName}...",
					$"You hear noises coming from the {closetName}... Something must be trying to break out!");
		}

		private void SetPassableAndLayer()
		{
			// Become passable to bullets and people when open
			if (passableWhenOpen)
			{
				registerObject.Passable = IsOpen;
				registerObject.CrawlPassable = IsOpen;
				// Switching to item layer if open so bullets pass through it
				gameObject.layer = LayerMask.NameToLayer(registerObject.Passable ? "Items" : "Machines");
			}
		}

		private void SyncDoorState(Door oldValue, Door value)
		{
			doorState = value;
			SetPassableAndLayer(); // required on client
		}

		#region Interaction

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;

			//only allow interactions targeting this closet
			if (interaction.TargetObject != gameObject) return false;

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (interaction.IsAltClick)
			{
				TryToggleLock(interaction);
			}
			else if (Validations.HasUsedActiveWelder(interaction))
			{
				TryWeld(interaction);
			}
			else if (IsLocked)
			{
				if (interaction.HandSlot.IsOccupied && interaction.HandObject.TryGetComponent<Emag>(out var emag))
				{
					TryEmag(interaction, emag);
				}
				else
				{
					TryToggleLock(interaction);
				}
			}
			else if (IsOpen)
			{
				if (interaction.HandSlot.IsOccupied)
				{
					// If nothing in the player's hand can be used on the closet, drop it in the closet.
					TryStoreItem(interaction);
				}
				else
				{
					// Try close the locker.
					TryToggleDoor(interaction);
				}
			}
			else if (Validations.HasUsedComponent<IDCard>(interaction) || Validations.HasUsedComponent<Items.PDA.PDALogic>(interaction))
			{
				TryToggleLock(interaction);
			}
			else
			{
				TryToggleDoor(interaction);
			}
		}

		private void TryToggleDoor(PositionalHandApply interaction)
		{
			if (IsLocked)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {closetName} is locked!");
				return;
			}

			if (IsWelded)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {closetName} is welded shut!");
				return;
			}

			SetDoor(IsOpen ? Door.Closed : Door.Opened);
		}

		private void TryToggleLock(PositionalHandApply interaction)
		{
			var effector = "hand";
			
			if (interaction.IsAltClick)
			{
				var idSource = interaction.PerformerPlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.id)
						.FirstOrDefault(slot => slot.IsOccupied);
				if (idSource != null)
				{
					effector = idSource.ItemObject.ExpensiveName();
				}
			}
			else if (interaction.HandSlot.IsOccupied)
			{
				effector = interaction.HandObject.ExpensiveName();
			}

			if (lockState == Lock.Broken)
			{
				Chat.AddExamineMsg(interaction.Performer, $"You wave your {effector} over the panel but the lock appears to be broken!");
				return;
			}
			else if (isLockable == false)
			{
				Chat.AddExamineMsg(
						interaction.Performer,
						$"You can't figure out where to wave your {effector}... Perhaps this closet isn't lockable?");
				return;
			}

			if (IsOpen)
			{
				Chat.AddExamineMsg(
						interaction.Performer,
						$"You wave your {effector} over the panel but soon realise the {closetName} is still open! D'oh!");
				return;
			}

			// First checks performer's ID in ID slot, else fall back to hand item.
			if (accessRestrictions.CheckAccess(interaction.Performer))
			{
				SetLock(IsLocked ? Lock.Unlocked : Lock.Locked);
				Chat.AddExamineMsg(interaction.Performer, $"You {(IsLocked ? "lock" : "unlock")} the {closetName}.");
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, $"You wave your {effector} over the panel but it denies your request!");
			}
		}

		private void TryWeld(PositionalHandApply interaction)
		{
			if (isWeldable == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You can't figure out how to weld the {closetName}...");
				return;
			}

			ToolUtils.ServerUseToolWithActionMessages(
					interaction, weldTime,
					$"You start {(IsWelded ? "unwelding" : "welding")} the {closetName}...",
					$"{interaction.Performer.ExpensiveName()} starts {(IsWelded ? "unwelding" : "welding")} the {closetName}...",
					$"You {(IsWelded ? "unweld" : "weld")} the {closetName}.",
					$"{interaction.Performer.ExpensiveName()} {(IsWelded ? "unwelds" : "welds")} the {closetName}.",
					() => SetWeld(IsWelded ? Weld.NotWelded : Weld.Welded));
		}

		private void TryEmag(PositionalHandApply interaction, Emag emag)
		{
			if (emag.EmagHasCharges() == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The emag is out of charges!");
				return;
			}

			if (lockState == Lock.Broken)
			{
				Chat.AddExamineMsgFromServer(
						interaction.Performer,
						"You wave the emag over the panel, but it looks to be already destroyed...");
				return;
			}

			SoundManager.PlayNetworkedAtPos(soundOnEmag, registerObject.WorldPositionServer, sourceObj: gameObject);

			emag.UseCharge(gameObject, interaction.Performer);
			BreakLock();
			Chat.AddActionMsgToChat(interaction,
				"The access panel errors. A slight amount of smoke pours from behind the panel...",
						"You can smell caustic smoke from somewhere...");
		}

		private void TryStoreItem(PositionalHandApply interaction)
		{
			Inventory.ServerDrop(interaction.HandSlot, interaction.TargetVector);
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			if (WillInteract(PositionalHandApply.ByLocalPlayer(gameObject), NetworkSide.Client))
			{
				// TODO: Make this contexual if holding a welder or wrench
				var optionName = IsOpen ? "Close" : "Open";
				result.AddElement("OpenClose", RightClickInteract, nameOverride: optionName);
			}

			return result;
		}

		private void RightClickInteract()
		{
			InteractionUtils.RequestInteract(PositionalHandApply.ByLocalPlayer(gameObject), this);
		}

		public string Examine(Vector3 worldPos = default)
		{
			if (IsWelded)
			{
				if (IsLocked) return "It is locked and welded shut.";
				if (lockState == Lock.Broken) return "The lock is broken and it is welded shut.";
			}
			else
			{
				if (IsLocked) return "It is locked.";
				if (lockState == Lock.Broken) return "The lock is broken.";
			}

			return string.Empty;
		}

		#endregion
	}
}
