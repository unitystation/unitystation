using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdminCommands;
using UnityEngine;
using Mirror;
using Core.Editor.Attributes;
using UI.Core.Net;
using Messages.Client.NewPlayer;
using Messages.Server;
using Systems.Electricity;
using Systems.Hacking;
using Systems.Interaction;
using Doors.Modules;
using HealthV2;
using Objects;
using Objects.Wallmounts;
using Shared.Systems.ObjectConnection;
using UnityEngine.Serialization;

namespace Doors
{
	/// <summary>
	/// This is the master 'controller' for the door. It handles interactions by players and passes any interactions it need to to its components.
	/// </summary>
	public class DoorMasterController : NetworkBehaviour, ICheckedInteractable<HandApply>,
		ICheckedInteractable<AiActivate>, ICanOpenNetTab, IMultitoolSlaveable, IServerSpawn,
		IBumpableObject, IRightClickable
	{
		#region Inspector
		[SerializeField]
		[Tooltip("Toggle damaging any living entities caught in the door as it closes")]
		private bool damageOnClose = false;

		[SerializeField]
		[Tooltip("Amount of damage when closed on someone.")]
		private float damageClosed = 90;

		[SerializeField]
		[Tooltip("Does this door open automatically when you walk into it?")]
		private bool isAutomatic = true;

		[SerializeField]
		[Tooltip("Can you interact with the door by HandApply or Bump?")]
		private bool allowInteraction = true;

		[SerializeField]
		[Tooltip("Is this door designed to close no matter what is underneath it?")]
		private bool ignorePassableChecks = false;

		[SerializeField]
		[Tooltip("Does this door push living entities when it closes on them?")]
		private bool closingPushesEntities = false;

		//Maximum time the door will remain open before closing itself.
		[SerializeField]
		[Tooltip("Time this door will wait until autoclosing")]
		private float maxTimeOpen = 5;

		[SerializeField]
		[Tooltip("Prevent the door from auto closing when opened.")]
		public bool BlockAutoClose = false;

		[SerializeField]
		[Tooltip("Prevent the door from auto closing when opened if was Clicked on to be opened.")]
		private bool clickDisablesAutoClose = false;



		private DoorAnimatorV2 doorAnimator;
		public DoorAnimatorV2 DoorAnimator => doorAnimator;
		private DoorSoundController soundController;
		public DoorSoundController SoundController => soundController;

		private const float INPUT_COOLDOWN = 0.25f;

		#endregion

		#region Initialization

		/// <summary>
		/// Sets whether the door is open or closed
		/// </summary>
		public bool IsClosed
		{
			get => registerTile.IsClosed;
			set => registerTile.IsClosed = value;
		}

		//Whether or not users can interact with the door.
		private bool allowInput = true;
		private IEnumerator coWaitOpened;
		private IEnumerator coBlockAutomaticClosing;

		private bool isPerformingAction = false;
		public bool IsPerformingAction => isPerformingAction;
		public bool HasPower => CheckPower();

		private RegisterDoor registerTile;
		public RegisterDoor RegisterTile => registerTile;
		private SpriteRenderer spriteRenderer;

		public Matrix matrix => registerTile.Matrix;

		private List<DoorModuleBase> modulesList;
		public List<DoorModuleBase> ModulesList => modulesList;



		[Tooltip("Does it have a glass window you can see through?")]
		public bool isWindowedDoor;

		// These variables are seperated out because open and closed doors are different layers and
		// windowed doors are set to a different mask layer than non-windowed doors
		private int openMaskingLayer;
		private int closedMaskingLayer;
		private int openSortingLayer;
		private int closedSortingLayer;

		public bool UseMachinesForOpenLayer = false;

		public HackingProcessBase HackingProcessBase;

		private GameObject byPlayer;

		public ConstructibleDoor ConstructibleDoor;

		private bool isFireLock;
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		private string doorName;

		private GameObject originator;
		private bool byForce;
		private bool OverrideLogic;


		private void Awake()
		{
			//Gets the door name.  Note this may not have the right capitalization
			doorName = gameObject.ExpensiveName();

			// Set masking and sorting layers
			SetLayerData();
			

			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			registerTile = GetComponent<RegisterDoor>();
			modulesList = GetComponentsInChildren<DoorModuleBase>().ToList();

			doorAnimator = GetComponent<DoorAnimatorV2>();
			doorAnimator.AnimationOpened += OnAnimationOpened;
			doorAnimator.AnimationClosed += OnAnimationClosed;
			doorAnimator.AnimationStarted += OnAnimationStarted;
			doorAnimator.AnimationFinished += OnAnimationFinished;

			soundController = GetComponent<DoorSoundController>();

			//Initialize the door state
			if (CustomNetworkManager.IsServer == true)
			{
				if (IsClosed) Close();
				else Open();
			}
		}
		
		/// <summary>
        /// Sets the appropriate sorting and masking layer for the door
        /// </summary>
		private void SetLayerData()
        {
            openMaskingLayer = LayerMask.NameToLayer("Door Open");

			// Windowed doors uses the windowed masking layer
			if (isWindowedDoor == true)
				closedMaskingLayer = LayerMask.NameToLayer("Windows");
			else
				closedMaskingLayer = LayerMask.NameToLayer("Door Closed");

			//If this is a firelock it goes on top of other doors when closed
			if (TryGetComponent<FireLock>(out _))
			{
				isFireLock = true;
				closedSortingLayer = SortingLayer.NameToID("WallObject");
				openSortingLayer = SortingLayer.NameToID("Machines");
			}
			else
			{
				closedSortingLayer = SortingLayer.NameToID("Doors Closed");
				openSortingLayer = SortingLayer.NameToID("Doors Open");
			}
        }

		public void OnSpawnServer(SpawnInfo info)
		{
			HackingProcessBase.RegisterPort(TryBump, this.GetType());
			HackingProcessBase.RegisterPort(TryClose, this.GetType());
			HackingProcessBase.RegisterPort(ConfirmAIConnection, this.GetType());
		}
		#endregion

		#region Core Functionality
		/// <summary>
        /// Defines what HandApplys interact with ANY door, note that ConstructibleDoor handles
		/// the hacking panel specific to airlocks.
        /// </summary>
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (Validations.IsTarget(gameObject, interaction) == false) return false;
			if (DefaultWillInteract.Default(interaction, side,
					Validations.CheckState(x => x.CanInteractWithDoors)) == false) return false;

			if (interaction.HandObject != null)
			{
				//Welders weld door if intent is Harm, repair door if intent is help
				if (Validations.HasUsedActiveWelder(interaction)) return true;

				//All other hand objects should melee if intent is harm
				if (interaction.Intent == Intent.Harm) return false;

				//Jaws of Life and other special pry tools
				if (Validations.HasItemTrait(interaction.HandObject.gameObject, CommonTraits.Instance.CanPryDoor)) return true;

				//Crowbars
				if (Validations.HasItemTrait(interaction.HandObject.gameObject, CommonTraits.Instance.Crowbar)) return true;
			}
			else
			{
				// Limbs that can pry doors.  //FIXME update this code when prying with hands is moved to body parts
				if (interaction.PerformerPlayerScript.PlayerTypeSettings.CanPryDoorsWithHands)
				{
					if (interaction.Intent == Intent.Harm) return false;
					else return true;
				}
			}

			return true;
		}

		/// <summary>
		/// Invoke this on server when player clicks a door to interact with it
		/// </summary>
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (allowInput == false) return;

			//When a player interacts with the door, we must first check with each module on what to do.
			//For instance, if one of the modules has locked the door, that module will want to prevent us from
			//opening the door.
			if (IsClosed == false)
			{
				OpenInteraction(interaction);
			}
			else
			{
				ClosedInteraction(interaction);
			}

			StartInputCoolDown();
		}

		/// <summary>
		/// These two methods are called when the door is interacted with, either opened or closed.
		/// They're separated so that the modules can handle interactions differently when either open or closed.
		/// </summary>
		/// <param name="interaction"></param>
		public void OpenInteraction(HandApply interaction)
		{
			// If the door is in motion or interaction is not permitted (firelocks, blast doors)
			if (isPerformingAction || allowInteraction == false) return;

			// If there is nothing preventing the door from closing, try closing it
			if (TryInteraction(interaction))
			{
				//If we are closing a door where clicks disable auto close, we want to re-enable autoclose
				if (clickDisablesAutoClose)
					BlockAutoClose = false;

				//Try to close the door via the wiring
				PulseTryClose(interaction.Performer, inOverrideLogic: true);
			}
		}

		/// <summary>
		/// These two methods are called when the door is interacted with, either opened or closed.
		/// They're separated so that the modules can handle interactions differently when either open or closed.
		/// </summary>
		/// <param name="interaction"></param>
		public void ClosedInteraction(HandApply interaction)
		{
			// If the door is in motion or interaction is not permitted (firelocks, blast doors)
			if (isPerformingAction || allowInteraction == false) return;

			// If there is nothing preventing the door from opening, try opening it
			if (TryInteraction(interaction))
			{
				//If we are opening a door where clicks disable auto close, we want to disable autoclose
				if (clickDisablesAutoClose)
					BlockAutoClose = true;

				//FIXME: Should use wiring, not whatever this is supposed to be
				TryOpen(interaction.Performer);
			}
		}

		/// <summary>
		/// Runs through each door module and sees if they have a special case for this kind of interaction.
		/// This version is for clicking on the door with a HandApply interaction.
		/// </summary>
		/// <param name="interaction">The Handapply interaction being used</param>
		/// <returns>True if none of the door modules would prevent the door from opening/closing</returns>
		private bool TryInteraction(HandApply interaction)
		{
			HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
			foreach (DoorModuleBase module in modulesList)
			{
				if (IsClosed)
					module.ClosedInteraction(interaction, states);
				else
					module.OpenInteraction(interaction, states);
			}
			
			// Forcing the door only cares about physical impediments
			if(byForce)
			{
				if (states.Contains(DoorProcessingStates.PhysicallyPrevented) || states.Contains(DoorProcessingStates.Welded)) return false;
				
				return true;
			}
			 
			//Give the player feedback on any reason we found that the door wont open
			AddChatTryInteractMessage(interaction.Performer, states);

			return CheckStatusAllow(states);
		}

		/// <summary>
		/// Runs through each door module and sees if they have a special case for this kind of interaction.
		/// This version is for bumping the door or other situations when the player isn't using HandApply
		/// </summary>
		/// <returns>True if none of the door modules would prevent the door from opening/closing</returns>
		private bool TryBumpInteraction()
		{
			HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
			foreach (var module in modulesList)
			{
				module.BumpingInteraction(byPlayer, states);
			}

			return CheckStatusAllow(states);
		}

		/// <summary>
		/// Gives the player feedback on the door's state
		/// </summary>
		/// <param name="player">The player to send the interact message to</param>
		/// <param name="states">A list of states, presumably generated by TryInteraction</param>
		public void AddChatTryInteractMessage(GameObject player, HashSet<DoorProcessingStates> states)
		{
			// If the interaction is being handled by the module (eg crowbaring), say nothing here
			if (states.Contains(DoorProcessingStates.PreventSilently)) return;

			if (states.Contains(DoorProcessingStates.Welded))
			{
				Chat.AddExamineMsgFromServer(player, $"The {doorName} is welded shut");
			}
			else if (states.Contains(DoorProcessingStates.PowerPrevented))
			{
				Chat.AddExamineMsgFromServer(player, $"The {doorName} is unpowered");
			}
			else if (states.Contains(DoorProcessingStates.SoftwarePrevented))
			{
				Chat.AddExamineMsgFromServer(player, $"The {doorName} denies access");
			}
			else if (states.Contains(DoorProcessingStates.PhysicallyPrevented))
			{
				Chat.AddExamineMsgFromServer(player, $"The {doorName} tries to move but something is physically preventing it");
			}
		}

		/// <summary>
		/// Invoked by the server when a player bumps into the door, trying to open it
		/// </summary>
		/// <param name="inbyPlayer"></param>
		/// <param name="client"></param>
		public void OnBump(GameObject inbyPlayer, GameObject client)
		{
			byPlayer = inbyPlayer;
			HackingProcessBase.ImpulsePort(TryBump);
		}

		#endregion

		#region Status Checks

		/// <summary>
        /// Searches the door for a power module and returns its state
        /// </summary>
        /// <returns>True if the door has power or has no power module (presumably it doesn't need one to work)</returns>
		private bool CheckPower()
		{
			foreach (var module in modulesList)
			{
				if (module is PowerModule powerModule)
				{
					return powerModule.HasPower;
				}
			}
			return true;
		}

/// <summary>
/// Runs through a hashset of states and checks if any prevent the door from function
/// </summary>
/// <param name="states"></param>
/// <returns>True if the door is allowed to open/close</returns>
		public bool CheckStatusAllow(HashSet<DoorProcessingStates> states)
		{
			if (states.Contains(DoorProcessingStates.PhysicallyPrevented)) return false;
			if (states.Contains(DoorProcessingStates.PowerPrevented)) return false;
			if (states.Contains(DoorProcessingStates.Welded)) return false;
			if (states.Contains(DoorProcessingStates.PreventSilently)) return false;
			if (states.Contains(DoorProcessingStates.SoftwarePrevented))
			{
				return states.Contains(DoorProcessingStates.SoftwareHacked);
			}
			else
			{
				return true;
			}
		}

/// <summary>
/// Checks if the door has a firelock, and if its engaged
/// </summary>
/// <returns>True if the firelock is engaged</returns>
		private bool FirelockEngagedCheck()
		{
			if (isFireLock == false)
			{
				var firelock = matrix.GetFirst<FireLock>(registerTile.LocalPositionServer, true);
				if (firelock != null && firelock.fireAlarm.activated && firelock.DoorMasterController.IsClosed) return true;
			}
			return false;
		}

		#endregion

		#region Bumping

		private void TryBump()
		{
			//A Door can't be bumped if its in motion, there is a firelock in the way, it isn't automatic, its in 
			//an input cooldown, and when there is a special reason they can't be interacted with (Blast Doors)
			if (FirelockEngagedCheck() || isAutomatic == false || allowInput == false || allowInteraction == false || isPerformingAction) return;

			if (TryBumpInteraction())
			{
				//FIXME: Should use wiring, not whatever this is
				TryOpen(byPlayer);
			}

			StartInputCoolDown();
		}

		/// <summary>
		/// Invoke this on server when player bumps into door to try to open it.
		/// </summary>
		public void Bump(GameObject inbyPlayer)
		{
			byPlayer = inbyPlayer;
			HackingProcessBase.ImpulsePort(TryBump);
		}

		#endregion

		#region Opening

		//FIXME This shouldn't exist
		public void TryOpen(GameObject originator, bool blockClosing = false)
		{
			if (IsClosed == false || isPerformingAction || FirelockEngagedCheck()) return; //Can't open if we are open. Figures.

			Open();
		}

		/// <summary>
		/// Try to force the door open, caring only about physical impediments.
		/// Purely check to see if there is something physically restraining the door from being opened such as a weld or door bolts.
		///	This would be in situations like as prying the door with a crowbar.
		/// </summary>
		/// <returns>True if the door was forcable, useful for followup in AI and other code</returns>
		public bool TryForceOpen()
		{
			//Can't force open a door that is open, is in motion, or has a firelock over it
			if (IsClosed == false || isPerformingAction || FirelockEngagedCheck()) return false;

			byForce = true;

			if (TryInteraction(null))
			{
				Open();
				byForce = false;
				return true;
			}
			
			byForce = false;
			return false;  
		}

		public void Open()
		{
			if (!gameObject) return;  // probably destroyed by a shuttle crash

			if (!BlockAutoClose)
			{
				ResetWaiting();
			}

			UpdateGui();

			doorAnimator.LightsWork = !byForce;
			doorAnimator.PanelOpen = ConstructibleDoor != null && ConstructibleDoor.Panelopen;
			doorAnimator.SyncDoorStatus(doorAnimator.SyncDoorUpdateType, DoorAnimatorV2.DoorUpdateType.Open);

			if (byForce)
			{
				soundController.ServerPlaySound(DoorSoundController.DoorSoundType.Forced);
			}
			else
			{
				soundController.ServerPlaySound(DoorSoundController.DoorSoundType.Open);
			}
		}

		#endregion

		#region Closing

		/// <summary>
		/// Try to force the door open, caring only about physical impediments.
		/// Purely check to see if there is something physically restraining the door from being opened such as a weld or door bolts.
		///	This would be in situations like as prying the door with a crowbar.
		/// </summary>
		/// <returns>True if the door was forcable, useful for followup in AI and other code</returns>
		public bool TryForceClose()
		{
			//Can't force closed a door that is closed, is in motion, or has a firelock over it
			if (IsClosed == true || isPerformingAction || FirelockEngagedCheck()) return false;

			byForce = true;

			if (TryInteraction(null))
			{
				Close();
				byForce = false;
				return true;
			}
			
			byForce = false;
			return false;
		}

/// <summary>
/// Uses wiring to close the door.  This is the prefered close door method.
/// </summary>
/// <param name="inoriginator"></param>
/// <param name="inforce"></param>
/// <param name="inOverrideLogic"></param>
		public void PulseTryClose(GameObject inoriginator = null, bool inforce = false, bool inOverrideLogic = false)
		{
			originator = inoriginator;
			byForce = inforce;
			OverrideLogic = inOverrideLogic;

			HackingProcessBase.ImpulsePort(TryClose);
		}

		//FIXME: Begone with this abomination!
		public void TryClose()
		{
			if (IsClosed) return; //Can't close if we are closed. Figures.
			if (isPerformingAction) return;

			// Sliding door is not passable according to matrix
			if (!isPerformingAction &&
				(ignorePassableChecks || matrix.CanCloseDoorAt(registerTile.LocalPositionServer, true)) &&
				(HasPower || byForce))

			{
				if (OverrideLogic)
				{
					Close();
				}
				else
				{
					HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();

					foreach (DoorModuleBase module in modulesList)
					{
						module.OpenInteraction(null, states);
					}

					if (CheckStatusAllow(states))
					{
						Close();
					}
					else
					{
						ResetWaiting();
					}
				}
			}
			else
			{
				ResetWaiting();
			}
		}

		public void Close()
		{
			if (!gameObject) return; // probably destroyed by a shuttle crash
			UpdateGui();

			doorAnimator.LightsWork = !byForce;
			doorAnimator.PanelOpen = ConstructibleDoor != null && ConstructibleDoor.Panelopen;
			doorAnimator.SyncDoorStatus(doorAnimator.SyncDoorUpdateType, DoorAnimatorV2.DoorUpdateType.Close);

			if (byForce)
			{
				soundController.ServerPlaySound(DoorSoundController.DoorSoundType.Forced);
			}
			else
			{
				soundController.ServerPlaySound(DoorSoundController.DoorSoundType.Close);
			}
		}


		#endregion

		#region Status Handling
		
		/// <summary>
		/// Handles the serverside aspects of doors closing, sets the collision, masking layer, etc
		/// </summary>
		private void BoxCollToggleOn()
		{
			IsClosed = true;
			SetMaskingLayer(closedMaskingLayer);
			spriteRenderer.sortingLayerID = closedSortingLayer;
			registerTile.SetNewSortingLayer(closedSortingLayer);
		}
		
		/// <summary>
        /// Handles the serverside aspects of doors opening, sets the collision, masking layer, etc
        /// </summary>
		private void BoxCollToggleOff()
		{
			IsClosed = false;
			SetMaskingLayer(openMaskingLayer);
			spriteRenderer.sortingLayerID = openSortingLayer;
			registerTile.SetNewSortingLayer(openSortingLayer);
		}

		/// <summary>
        /// Sets the masking layer of the door
        /// </summary>
		private void SetMaskingLayer(int layer)
		{
			gameObject.layer = layer;
			foreach (Transform child in transform)
			{
				child.gameObject.layer = layer;
			}
		}

		public void StartInputCoolDown()
		{
			allowInput = false;
			StartCoroutine(DoorInputCoolDown());
		}

		private IEnumerator DoorInputCoolDown()
		{
			yield return WaitFor.Seconds(INPUT_COOLDOWN);
			allowInput = true;
		}

		/// <summary>
		/// Invoked by doorAnimator once a door animation starts
		/// </summary>
		private void OnAnimationStarted()
		{
			isPerformingAction = true;
		}

		/// <summary>
		/// Invoked by doorAnimator when the animation for closing plays
		/// </summary>
		private void OnAnimationClosed()
		{
			BoxCollToggleOn();

			if (damageOnClose)
			{
				ServerDamageOnClose();
			}
		}

		/// <summary>
		/// Invoked by doorAnimator when the animation for opening plays
		/// </summary>
		private void OnAnimationOpened()
		{
			BoxCollToggleOff();
		}

		/// <summary>
		/// Invoked by doorAnimator once a door animation finishes
		/// </summary>
		private void OnAnimationFinished()
		{
			isPerformingAction = false;
			//check if the door is closing on something, and reopen it if so.

			//When the door first closes, it checks if anything is blocking it, but it is still possible
			//for a laggy client to go into the door while it is closing. There are 2 cases:
			// 1. Client enters door after server knows the door is impassable, but before client knows it is impassable.
			// 2. Client enters door after the close begins but before server marks the door as impassable and before
			// 		the client knows it is impassable. This is rare but there is a slight delay (.15 s) between when the door close
			//		begins and when the server registers the door as impassable, so it is possible (See AirLockAnimator.MakeSolid)
			// Case 1 is handled by our rollback code - the client will be lerp'd back to their previous position.
			// Case 2 won't be handled by the rollback code because the client enters the passable tile while the
			//	server still thinks its passable. So, for the rare situation that case 2 occurs, we will apply
			// the below logic and reopen the door if the client got stuck in the door in the .15 s gap.

			//only do this check when door is closing, and only for doors that block all directions (like airlocks)
			if (!CustomNetworkManager.IsServer ||
				!IsClosed ||
				registerTile.OneDirectionRestricted ||
				ignorePassableChecks)
			{
				return;
			}

			if (MatrixManager.IsPassableAtAllMatrices(
				registerTile.WorldPositionServer,
				registerTile.WorldPositionServer,
				isServer: true,
				includingPlayers: true,
				context: this.gameObject))
			{
				return;
			}

			//something is in the way, open back up
			Open();
		}

		private void ServerDamageOnClose()
		{
			foreach (var healthBehaviour in matrix.Get<LivingHealthMasterBase>(registerTile.LocalPositionServer, true))
			{
				healthBehaviour.ApplyDamageAll(gameObject, damageClosed, AttackType.Melee, DamageType.Brute);
			}
		}

		private void ResetWaiting()
		{
			if (maxTimeOpen.Approx(-1)) return;

			if (CustomNetworkManager.IsServer == false) return;

			if (coWaitOpened != null)
			{
				StopCoroutine(coWaitOpened);
				coWaitOpened = null;
			}

			coWaitOpened = AutoCloseDoor();
			StartCoroutine(coWaitOpened);
		}

		private IEnumerator AutoCloseDoor()
		{
			// After the door opens, wait until it's supposed to close.
			yield return WaitFor.Seconds(maxTimeOpen);

			if (BlockAutoClose) yield break;

			if (isAutomatic == false) yield break;

			if (HasPower == false) yield break;

			//If we are already closed don't need to pulse
			if (IsClosed) yield break;

			PulseTryClose();
		}

		public void ToggleBlockAutoClose(bool newState)
		{
			BlockAutoClose = newState;
		}
		#endregion

		#region Ai interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			//Normal click should open door UI instead
			if (interaction.ClickType == AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		private bool AIConnected;

		public void ConfirmAIConnection()
		{
			AIConnected = true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			if (HasPower == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Door is unpowered");
				return;
			}

			AIConnected = false;
			HackingProcessBase.ImpulsePort(ConfirmAIConnection);
			if (AIConnected == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Door is disconnected");
				return;
			}
			//Try open/close
			if (interaction.ClickType == AiActivate.ClickTypes.ShiftClick)
			{
				if (IsClosed)
				{
					TryForceOpen();
				}
				else
				{
					//FIXME
					//PulseTryForceClose();
				}

				return;
			}

			//Toggle bolts
			if (interaction.ClickType == AiActivate.ClickTypes.CtrlClick)
			{
				foreach (var module in modulesList)
				{
					if (module is BoltsModule bolts)
					{
						//Toggle bolts
						bolts.PulseToggleBolts();
						return;
					}
				}
			}
		}

		#endregion

		#region Airlock UI

		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			bool isAi = playerObject.GetComponent<PlayerScript>().PlayerType == PlayerTypes.Ai;
			if (netTabType == NetTabType.HackingPanel)
			{
				//Block Ai from hacking UI but allow normal player
				return isAi == false;
			}

			if (isAi == false)
			{
				//Block normal player from Ai door controlling UI
				return false;
			}

			if (HasPower == false)
			{
				Chat.AddExamineMsgFromServer(playerObject, "Door is unpowered");
				return false;
			}

			AIConnected = false;
			HackingProcessBase.ImpulsePort(ConfirmAIConnection);
			if (isAi && AIConnected == false)
			{
				Chat.AddExamineMsgFromServer(playerObject, "Door is disconnected");
				return false;
			}

			//Only allow AI to open airlock control UI
			return true;
		}

		public bool CanAIInteract()
		{
			AIConnected = false;
			HackingProcessBase.ImpulsePort(ConfirmAIConnection);
			return AIConnected;
		}

		public void UpdateGui()
		{
			var peppers = NetworkTabManager.Instance.GetPeepers(gameObject, NetTabType.Airlock);
			if (peppers.Count == 0) return;

			List<ElementValue> valuesToSend = new List<ElementValue>();

			valuesToSend.Add(new ElementValue() { Id = "OpenLabel", Value = Encoding.UTF8.GetBytes(IsClosed ? "Closed" : "Open") });

			foreach (var module in modulesList)
			{
				if (module is BoltsModule bolts)
				{
					valuesToSend.Add(new ElementValue() { Id = "BoltLabel", Value = Encoding.UTF8.GetBytes(bolts.BoltsDown ? "Bolted" : "Unbolted") });
				}

				if (module is ElectrifiedDoorModule electric)
				{
					valuesToSend.Add(new ElementValue() { Id = "ShockStateLabel", Value = Encoding.UTF8.GetBytes(electric.IsElectrified ? "DANGER" : "SAFE") });
				}
			}

			// Update all UI currently opened.
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Airlock, TabAction.Update, valuesToSend.ToArray());
		}

		#endregion

		#region Multitool Interaction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.DoorButton;

		[SerializeField]
		[Tooltip("Whether this door type requires a linked door button (e.g. shutters).")]
		private bool requireLink = false;

		MultitoolConnectionType IMultitoolLinkable.ConType => conType;
		IMultitoolMasterable IMultitoolSlaveable.Master => doorMaster;
		bool IMultitoolSlaveable.RequireLink => false;
		// TODO: should be requireLink but hardcoded to false for now,
		// doors don't know about links, only the switches
		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}
		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private IMultitoolMasterable doorMaster;

		private void SetMaster(IMultitoolMasterable master)
		{
			doorMaster = master;

			if (master is DoorSwitch doorSwitch)
			{
				doorSwitch.NewAddDoorControllerFromScene(this);
			}
			else if (master is StatusDisplay statusDisplay)
			{
				statusDisplay.NewLinkDoor(this);
			}
		}

		#endregion

		#region Admin Functions
		public RightClickableResult GenerateRightClickOptions()
		{
			if (KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold) == false)
			{
				return null;
			}

			bool add = false;

			var options = RightClickableResult.Create();
			if (PlayerList.HasTAGClient(TAG.ADMIN_OPEN_DOORS))
			{
				add = true;
				options.AddAdminElement("Force Open", AdminOpen);
			}

			if (PlayerList.HasTAGClient(TAG.ADMIN_TOGGLE_BOLTS))
			{
				add = true;
				if (GetComponentInChildren<BoltsModule>() != null)
				{
					options.AddAdminElement("Toggle Bolts", AdminToggleBolt);
				}
			}

			if (PlayerList.HasTAGClient(TAG.ADMIN_ELECTRIFIE_DOOR))
			{
				add = true;
				if (GetComponentInChildren<ElectrifiedDoorModule>() != null)
				{
					options.AddAdminElement("Toggle Electrify", AdminToggleElectrify);
				}
			}

			if (add)
			{
				return options;
			}
			else
			{
				return null;
			}
		}

		private void AdminOpen()
		{
			AdminCommandsManager.Instance.CmdOpenDoor(gameObject);
		}

		private void AdminToggleBolt()
		{
			AdminCommandsManager.Instance.CmdToggleBoltDoor(gameObject);
		}

		private void AdminToggleElectrify()
		{
			AdminCommandsManager.Instance.CmdToggleElectrifiedDoor(gameObject);
		}
		#endregion
	}
}