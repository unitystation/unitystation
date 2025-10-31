using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdminCommands;
using UnityEngine;
using UnityEngine.Serialization;
using Mirror;
using Core.Editor.Attributes;
using UI.Core.Net;
using Messages.Client.NewPlayer;
using Messages.Server;
using Systems.Electricity;
using Systems.Hacking;
using Systems.Interaction;
using System.Threading;
using Cysharp.Threading.Tasks;
using Doors.Modules;
using HealthV2;
using Objects;
using Objects.Wallmounts;
using Shared.Systems.ObjectConnection;


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

		[SerializeField]
		[Tooltip("Time this door will wait until autoclosing")]
		private float maxTimeOpen = 5;

		[SerializeField]
		[Tooltip("Prevent the door from auto closing when opened.")]
		public bool BlockAutoClose = false;

		[SerializeField]
		[Tooltip("Prevent the door from auto closing when opened if was Clicked on to be opened.")]
		private bool clickDisablesAutoClose = false;

		[SerializeField]
		[Tooltip("Does it have a glass window you can see through?")]
		public bool isWindowedDoor;

		#endregion

		#region Initialization

		private DoorAnimatorV2 doorAnimator;
		public DoorAnimatorV2 DoorAnimator => doorAnimator;
		private DoorSoundController soundController;
		public DoorSoundController SoundController => soundController;
		public HackingProcessBase HackingProcessBase;
		public ConstructibleDoor ConstructibleDoor;
		private List<DoorModuleBase> modulesList;
		public List<DoorModuleBase> ModulesList => modulesList;
		private RegisterDoor registerTile;
		public RegisterDoor RegisterTile => registerTile;

		/// <summary>
		/// How long in seconds we should make players wait between clicks/bumps
		/// </summary>
		private const float INPUT_COOLDOWN = 0.25f;

		/// <summary>
        /// Prevents the door from being used during input cooldown
        /// </summary>
		private bool allowInput = true;

		/// <summary>
		/// Sets whether the door is open or closed
		/// </summary>
		public bool IsClosed
		{
			get => registerTile.IsClosed;
			set => registerTile.IsClosed = value;
		}

		private CancellationTokenSource autoCloseTokenSource;

		/// <summary>
        /// Prevents interaction while door is in motion
        /// </summary>
		private bool isPerformingAction = false;
		public bool IsPerformingAction => isPerformingAction;

		public bool HasPower => CheckPower();
		public bool UseMachinesForOpenLayer = false;
		private bool isFireLock;
		public bool IsFireLock => isFireLock;
		private string doorName;
		public string DoorName => doorName;

		/// <summary>
		/// The entity that interacted with the door
		/// </summary>
		private GameObject originator;
		/// <summary>
		/// Whether the entity is trying to force the door or not
		/// </summary>
		private bool byForce;

		public bool IsFireLockEngaged = false;

		private Vector3Int worldPosition;


		private void Awake()
		{
			doorName = gameObject.ExpensiveName();

			if (TryGetComponent<FireLock>(out _))
				isFireLock = true;

			registerTile = GetComponent<RegisterDoor>();
			modulesList = GetComponentsInChildren<DoorModuleBase>().ToList();

			doorAnimator = GetComponent<DoorAnimatorV2>();
			doorAnimator.AnimationOpened += OnAnimationOpened;
			doorAnimator.AnimationClosed += OnAnimationClosed;
			doorAnimator.AnimationStarted += OnAnimationStarted;
			doorAnimator.AnimationFinished += OnAnimationFinished;

			soundController = GetComponent<DoorSoundController>();

			worldPosition = registerTile.WorldPositionServer;

			//Initialize the door state
			if (CustomNetworkManager.IsServer == true)
			{
				if (IsClosed) Close();
				else Open();
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			HackingProcessBase.RegisterPort(Close, this.GetType());
			HackingProcessBase.RegisterPort(Open, this.GetType());
			HackingProcessBase.RegisterPort(TryBump, this.GetType());
			HackingProcessBase.RegisterPort(ConfirmAIConnection, this.GetType());
		}
		#endregion

		#region Core Functionality
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
		}

		/// <summary>
		/// Defines what HandApplys interact with ANY door, note that ConstructibleDoor handles
		/// the hacking panel specific to airlocks.
		/// </summary>
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.TargetObject != gameObject || DefaultWillInteract.Default(interaction, side,
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
		/// These two methods are called when the door is interacted with, either opened or closed.
		/// They're separated so that the modules can handle interactions differently when either open or closed.
		/// </summary>
		/// <param name="interaction"></param>
		public void OpenInteraction(HandApply interaction)
		{
			// If there is nothing preventing the door from closing, try closing it
			if (TryInteraction(interaction))
			{
				//If we are closing a door where clicks disable auto close, we want to re-enable autoclose
				if (clickDisablesAutoClose)
					BlockAutoClose = false;

				//Try to close the door via the wiring
				PulseTryClose(interaction.Performer, overrideLogic: true);
			}
		}

		/// <summary>
		/// These two methods are called when the door is interacted with, either opened or closed.
		/// They're separated so that the modules can handle interactions differently when either open or closed.
		/// </summary>
		/// <param name="interaction">The HandApply interaction being used</param>
		public void ClosedInteraction(HandApply interaction)
		{
			// If there is nothing preventing the door from opening, try opening it
			if (TryInteraction(interaction))
			{
				//If we are opening a door where clicks disable auto close, we want to disable autoclose
				if (clickDisablesAutoClose)
					BlockAutoClose = true;

				//FIXME: Should use wiring, not whatever this is supposed to be
				PulseTryOpen(interaction.Performer, overrideLogic: true);
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
			//A Door can't be manipulated if its in motion, there is a firelock in the way, its in an input cooldown,
			// and when there is a special reason they can't be interacted with (Blast Doors)
			if (IsFireLockEngaged || allowInput == false || allowInteraction == false || isPerformingAction) return false;

			DoorInputCoolDown().Forget();

			//Iterate through all the modules and get any blocking states
			HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
			foreach (DoorModuleBase module in modulesList)
			{
				if (IsClosed)
					module.ClosedInteraction(interaction, states);
				else
					module.OpenInteraction(interaction, states);
			}

			// Forcing the door only cares about physical blocking states
			if (byForce)
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
		private bool TryInteraction()
		{
			//A Door can't be manipulated if its in motion, there is a firelock in the way, or is in an input cooldown
			if (IsFireLockEngaged || allowInput == false || isPerformingAction) return false;

			DoorInputCoolDown().Forget();

			HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
			foreach (var module in modulesList)
			{
				module.BumpingInteraction(originator, states);
			}

			// Forcing the door only cares about physical blocking states
			if (byForce)
			{
				if (states.Contains(DoorProcessingStates.PhysicallyPrevented) || states.Contains(DoorProcessingStates.Welded)) return false;

				byForce = false; //This is to prevent the "forced door" sound from playing
				return true;
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
		#endregion

		#region Status Checks

		/// <summary>
		/// Searches the door for a power module and returns whether the door is powered
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
		/// Checks if the door is functioning for purposes of remote access
		/// </summary>
		// <returns>True if the door is powered (or doesn't need power) and has an access module</returns>
		public bool CheckRemoteConnectivity()
		{
			var accessModule = GetComponentInChildren<AccessModule>();
			if (CheckPower() && accessModule != null) return true;

			return false;
		}

		/// <summary>
		/// Searches the door for a access module and returns whether the given entity has access
		/// </summary>
		// <returns>True if the entity has access</returns>
		public bool CheckAccess(GameObject performer)
		{
			foreach (var module in modulesList)
			{
				if (module is AccessModule accessModule)
				{
					return accessModule.CheckAccess(performer);
				}
			}
			return false;
		}

		/// <summary>
		/// Checks if door can be closed at this tile
		/// </summary>
		private bool CanCloseDoor()
		{
			// Door should close when the firelock closes
			if (IsFireLockEngaged)
				return true;

			// Otherwise, it shouldn't close if something is in the way or on any living thing
			return MatrixManager.IsPassableAtAllMatricesOneTile(worldPosition,
				isServer: true, includingPlayers: true, context: this.gameObject) &&
				MatrixManager.GetAt<LivingHealthMasterBase>(worldPosition, true).Any() == false;
		}
		#endregion

		#region Bumping

		/// <summary>
		/// Invoked by the server when a player bumps into the door, trying to open it
		/// </summary>
        /// 
		public void OnBump(GameObject inOriginator, GameObject client)
		{
			originator = inOriginator;
			HackingProcessBase.ImpulsePort(TryBump);
		}

		/// <summary>
		/// Handles the onBump event after making sure the wiring is connected
		/// </summary>
		private void TryBump()
		{
			//Only automatic doors that aren't firelocks/blastdoors open when bumped
			if (isAutomatic == false || allowInteraction == false) return;

			PulseTryOpen(originator);
		}

		#endregion

		#region Opening

		/// <summary>
		/// Checks all logic before opening the door.  This is the prefered open door method.
		/// </summary>
		/// <param name="inOriginator">The entity trying to open the door</param>
		/// <param name="bypassSoftware">If true this bypasses all access and software checks</param>
		/// <param name="overrideLogic">Completely skips all logic other than wiring and power, use cautiously!</param>
		public void PulseTryOpen(GameObject inOriginator = null, bool bypassSoftware = false, bool overrideLogic = false)
		{
			if (IsClosed == false || HasPower == false) return;

			originator = inOriginator;
			byForce = bypassSoftware;

			if (overrideLogic)
				HackingProcessBase.ImpulsePort(Open);

			else if (TryInteraction())
				Open();
		}

		/// <summary>
		/// Try to force the door open, caring only about physical impediments.
		///	This is for situations like prying the door with a crowbar.
		/// </summary>
		/// <returns>True if the door was forcable, useful for followup in AI and other code</returns>
		public bool TryForceOpen()
		{
			//Can't force open a door that is open
			if (IsClosed == false) return false;

			byForce = true;

			if (TryInteraction(null))
			{
				Open();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Opens the door if it can be opened, no questions asked.  Use PulseTryOpen instead unless you know what you are doing.
		/// </summary>
		public void Open()
		{
			if (!gameObject) return;  // probably destroyed by a shuttle crash

			WaitToAutoClose();

			UpdateGui();

			doorAnimator.LightsWork = !byForce;
			doorAnimator.PanelOpen = ConstructibleDoor != null && ConstructibleDoor.Panelopen;
			doorAnimator.SyncDoorStatus(doorAnimator.SyncDoorUpdateType, DoorAnimatorV2.DoorUpdateType.Open);

			//Play sound based on whether the door was forced or not
			if (byForce)
				soundController.ServerPlaySound(DoorSoundController.DoorSoundType.Forced);
			else
				soundController.ServerPlaySound(DoorSoundController.DoorSoundType.Open);
		}

		#endregion

		#region Closing

		/// <summary>
		/// Checks all logic before closing the door.  This is the prefered close door method.
		/// </summary>
		/// <param name="inOriginator">The entity trying to close the door</param>
		/// <param name="bypassSoftware">If true this bypasses all access and software checks</param>
		/// <param name="overrideLogic">Completely skips all logic other than wiring, use cautiously!</param>
		public void PulseTryClose(GameObject inOriginator = null, bool bypassSoftware = false, bool overrideLogic = false)
		{
			if (IsClosed || HasPower == false) return;
			originator = inOriginator;
			byForce = bypassSoftware;
			if (overrideLogic) HackingProcessBase.ImpulsePort(Close);
				
			else if (TryInteraction())
			{
                Close();
            } 

			else WaitToAutoClose();
		}

		/// <summary>
		/// Try to force the door open, caring only about physical impediments.
		///	This is for situations like prying the door with a crowbar.
		/// </summary>
		/// <returns>True if the door was forcable, useful for followup in AI and other code</returns>
		public bool TryForceClose()
		{
			//Can't force closed a door that is closed, is in motion, or has a firelock over it
			if (IsClosed == true) return false;

			byForce = true;

			if (TryInteraction(null))
			{
				Close();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Closes the door if it can be closed, no questions asked.  Use PulseTryClose instead unless you know what you are doing.
		/// </summary>
		public void Close()
		{
			//Ensure the door still exists (not destroyed by shuttle crash)
			if (gameObject == null) return;

			//Make sure the door isn't blocked or ignores whether its blocked
			if (ignorePassableChecks == false && CanCloseDoor() == false)
			{
				WaitToAutoClose();
				return;
			}

			UpdateGui();

			doorAnimator.LightsWork = !byForce;
			doorAnimator.PanelOpen = ConstructibleDoor != null && ConstructibleDoor.Panelopen;
			doorAnimator.SyncDoorStatus(doorAnimator.SyncDoorUpdateType, DoorAnimatorV2.DoorUpdateType.Close);

			//Play sound based on whether the door was forced or not
			if (byForce)
				soundController.ServerPlaySound(DoorSoundController.DoorSoundType.Forced);
			else
				soundController.ServerPlaySound(DoorSoundController.DoorSoundType.Close);
		}

		#endregion

		#region Animation Timing

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
			IsClosed = true;

			if (damageOnClose)
				ServerDamageOnClose();

			if (closingPushesEntities)
				ServerPushOnClose();
		}

		/// <summary>
		/// Invoked by doorAnimator when the animation for opening plays
		/// </summary>
		private void OnAnimationOpened()
		{
			IsClosed = false;
		}

		/// <summary>
		/// Invoked by doorAnimator once an animation finishes
		/// </summary>
		private void OnAnimationFinished()
		{
			isPerformingAction = false;

			// Check if the door is closing on something, and reopen it if so.
			// Only do this check if: we are the server, the door isn't allowed to close on things, 
			// the door is closing, and the door blocks all directions (eg airlocks)
			if (CustomNetworkManager.IsServer && ignorePassableChecks == false && CanCloseDoor() == false && IsClosed &&
				registerTile.OneDirectionRestricted == false && MatrixManager.IsPassableAtAllMatrices(worldPosition,
				worldPosition, isServer: true, includingPlayers: true, context: this.gameObject) == false)
			{
				//TODO: We could split the door animations in two halves and have this check happen in OnAnimationClosed() instead
				//and have the door reopen after half closing, for a more polished look to the close-on-player animation
				Open();
			}
		}

		#endregion

		#region Misc Functions
		
		private async UniTask DoorInputCoolDown()
		{
			allowInput = false;
			await UniTask.WaitForSeconds(INPUT_COOLDOWN);
			allowInput = true;
		}


		private void ServerDamageOnClose()
		{
			foreach (var healthBehaviour in MatrixManager.GetAt<LivingHealthMasterBase>(worldPosition, true))
			{
				healthBehaviour.ApplyDamageAll(gameObject, damageClosed, AttackType.Melee, DamageType.Brute);
			}
		}

		private void ServerPushOnClose()
        {
            
        }

		/// <summary>
        /// 
        /// </summary>
		private void WaitToAutoClose()
		{
			if (maxTimeOpen.Approx(-1) || CustomNetworkManager.IsServer == false) return;

			autoCloseTokenSource?.Cancel();
			autoCloseTokenSource?.Dispose();
			autoCloseTokenSource = new CancellationTokenSource();

			AutoCloseDoor(autoCloseTokenSource.Token).Forget();
		}

		private async UniTaskVoid AutoCloseDoor(CancellationToken cancelToken)
		{
			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(maxTimeOpen), cancellationToken: cancelToken);

				if (BlockAutoClose == false && isAutomatic && IsClosed == false)
					PulseTryClose(bypassSoftware: true);
			}
			catch{}
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
		[field: SerializeField] public bool CanRelink { get; set; } = true;
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