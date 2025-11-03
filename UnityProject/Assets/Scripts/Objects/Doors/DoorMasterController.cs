using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdminCommands;
using UnityEngine;
using Mirror;
using UI.Objects;
using UI.Core.Net;
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


		private BoltsModule bolts;
		public BoltsModule Bolts => bolts;
		private ElectrifiedDoorModule electrifyModule;
		public ElectrifiedDoorModule ElectrifyModule => electrifyModule;
		public event Action UpdateGUIEvent;

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

			bolts = GetComponentInChildren<BoltsModule>();
			electrifyModule = GetComponentInChildren<ElectrifiedDoorModule>();

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
			HackingProcessBase.RegisterPort(AIConnection, this.GetType());
		}
		#endregion

		#region Core Functionality
		/// <summary>
		/// Invoke this on server when player clicks a door to interact with it
		/// </summary>
		public void ServerPerformInteraction(HandApply interaction)
		{
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
			if (CheckInteractionAllowed() == false || allowInteraction == false) return false;

			DoorInputCoolDown().Forget();

			//Iterate through all the modules with the handapply and get any blocking states
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
			if (CheckInteractionAllowed() == false) return false;

			DoorInputCoolDown().Forget();

			HashSet<DoorProcessingStates> states = GetStates();

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
		/// This gets the blocking states for a non-handapply interaction with the door
		/// </summary>
		public HashSet<DoorProcessingStates> GetStates()
		{
			HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
			foreach (var module in modulesList)
			{
				module.BumpingInteraction(originator, states);
			}
			return states;
		}

		/// <summary>
		/// Checks for any flag that blocks the door from being interacted with
		/// </summary>
		public bool CheckInteractionAllowed()
		{
			if (allowInput == false || isPerformingAction) return false;

			if (IsFireLockEngaged)
			{
				//We need to make sure the firelock wasn't destroyed
				if (MatrixManager.GetAt<FireLock>(worldPosition, true).Any()) return false;

				IsFireLockEngaged = false;
			}

			return true;
		}

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

			if (overrideLogic || TryInteraction())
				HackingProcessBase.ImpulsePort(Open);
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

			if (overrideLogic || TryInteraction())
			{
				//We want to send the impulse no matter what the close port is connected to for hacking shennanigans
				HackingProcessBase.ImpulsePort(Close);

				//But if the cable isn't closing the door, we do also want it to autoclose whenever the cable is reconnected
				if (HackingProcessBase.HasConnection(Close) == false)
					WaitToAutoClose();
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

				byForce = false;
				return true;
			}
			byForce = false;
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
			UpdateGUI();

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
			UpdateGUI();
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

		/// <summary>
		/// Prevents players from spamming doors
		/// </summary>
		private async UniTaskVoid DoorInputCoolDown()
		{
			allowInput = false;
			await UniTask.WaitForSeconds(INPUT_COOLDOWN);
			allowInput = true;
		}

		/// <summary>
		/// Deals damage to entities when the door closes on them
		/// </summary>
		private void ServerDamageOnClose()
		{
			foreach (var healthBehaviour in MatrixManager.GetAt<LivingHealthMasterBase>(worldPosition, true))
			{
				healthBehaviour.ApplyDamageAll(gameObject, damageClosed, AttackType.Melee, DamageType.Brute);
			}
		}

		/// <summary>
		/// Pushes entites when the door closes on them
		/// </summary>
		private void ServerPushOnClose()
		{
			//TODO: This needs to be implemented still
		}

		/// <summary>
		/// Handles the automatic door closing feature
		/// </summary>
		private void WaitToAutoClose()
		{
			if (maxTimeOpen.Approx(-1) || CustomNetworkManager.IsServer == false) return;

			autoCloseTokenSource?.Cancel();
			autoCloseTokenSource?.Dispose();
			autoCloseTokenSource = new CancellationTokenSource();

			AutoCloseDoor(autoCloseTokenSource.Token).Forget();
		}

		/// <summary>
		/// The timer for the automatic door closing
		/// </summary>
		private async UniTaskVoid AutoCloseDoor(CancellationToken cancelToken)
		{
			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(maxTimeOpen), cancellationToken: cancelToken);

				if (BlockAutoClose == false && isAutomatic && IsClosed == false)
					PulseTryClose(bypassSoftware: true);
			}
			catch { }
		}

		/// <summary>
		/// Toggles whether the door should autoclose
		/// </summary>
		public void ToggleBlockAutoClose(bool newState)
		{
			BlockAutoClose = newState;
		}
		#endregion

		#region AI and NetTab

		/// <summary>
		/// Sets how the AI player can interact with the door by clicking on it
		/// </summary>
		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			//Normal click should open door UI instead
			if (interaction.ClickType == AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		/// <summary>
		/// Handles the AI player's click interactions
		/// </summary>
		public void ServerPerformInteraction(AiActivate interaction)
		{
			if (interaction.ClickType == AiActivate.ClickTypes.ShiftClick)
				ToggleOpenDoor(interaction.Performer);

			if (interaction.ClickType == AiActivate.ClickTypes.CtrlClick)
				ToggleBoltDoor(interaction.Performer);
		}

		/// <summary>
		/// Confirms there is nothing stopping the AI from interacting with the door
		/// </summary>
		private bool CheckCanAIInteract(GameObject performer, bool isOpenNetTab = false)
		{
			// All the normal reasons a door can't be interacted with, skipped if we are just opening the net tab
			if (isOpenNetTab == false && CheckInteractionAllowed() == false) return false;

			// The door needs power
			if (HasPower == false)
			{
				Chat.AddExamineMsgFromServer(performer, $"The {DoorName} is unpowered");
				return false;
			}

			// The AI needs to be wired in
			// Hacking shennanigans note: every time the AI tries to interact with the door in any way the wire will pulse
			if (CanAIInteract() == false)
			{
				Chat.AddExamineMsgFromServer(performer, $"You can't connect to the {DoorName}");
				return false;
			}

			return true;
		}

		/// <summary>
		/// This action exists so that it can have a wire running to it in HackingProcessBase
		/// Don't call this! Call CanAIInteract() to test if the AI is connected to the door.
		/// </summary>
		public void AIConnection() { }

		/// <summary>
		/// Handles the AI opening and closing the door
		/// </summary>
		public void ToggleOpenDoor(GameObject performer)
		{
			if (CheckCanAIInteract(performer) == false) return;

			if (IsClosed)
			{
				//TODO: AI does not have access, if that changes also change this to go off of connected player;
				PulseTryOpen(bypassSoftware: true);

				// Tells the AI if the door is miswired. We pulse the door anyway in case of hacking shennanigans, 
				// but the AI player should get some feedback as to why the door didn't open when they clicked
				if (HackingProcessBase.HasConnection(Open) == false)
					Chat.AddExamineMsgFromServer(performer, $"The {DoorName} is wired incorrectly and wont open");
				else
				{
					// Otherwise we tell the ai if it was physically blocked from opening
					HashSet<DoorProcessingStates> states = GetStates();
					if (states.Contains(DoorProcessingStates.PhysicallyPrevented) || states.Contains(DoorProcessingStates.Welded))
						Chat.AddExamineMsgFromServer(performer, $"The {DoorName} tries to move but something is physically preventing it");
				}
			}
			else
			{
				//TODO: AI does not have access, if that gets fix change this to PulseTryOpen(performer);
				PulseTryClose(bypassSoftware: true);

				// Tells the AI if the door is miswired. We pulse the door anyway in case of hacking shennanigans, 
				// but the AI player should get some feedback as to why the door didn't close when they clicked
				if (HackingProcessBase.HasConnection(Close) == false)
					Chat.AddExamineMsgFromServer(performer, $"The {DoorName} is wired incorrectly and wont close");
				else
				{
					// Otherwise we tell the ai if it was physically blocked from opening
					HashSet<DoorProcessingStates> states = GetStates();
					if (states.Contains(DoorProcessingStates.PhysicallyPrevented) || states.Contains(DoorProcessingStates.Welded))
						Chat.AddExamineMsgFromServer(performer, $"The {DoorName} tries to move but something is physically preventing it");
				}
			}

			UpdateGUI();
		}


		/// <summary>
		/// Handles the AI bolting and unbolting the door
		/// </summary>
		public void ToggleBoltDoor(GameObject performer)
		{
			if (CheckCanAIInteract(performer) == false) return;

			if (bolts != null)
			{
				bolts.PulseToggleBolts();

				if (HackingProcessBase.HasConnection(bolts.ToggleBolts) == false)
					Chat.AddExamineMsgFromServer(performer, $"The {DoorName} is wired incorrectly and you can't access the bolts mechanism");

				UpdateGUI();
			}
			else
				Chat.AddExamineMsgFromServer(performer, $"The {DoorName} doesn't have bolts to drop");
		}

		/// <summary>
		/// Handles the AI turning the electrification on or off
		/// </summary>
		public void ToggleSafetyDoor(GameObject performer)
		{
			if (CheckCanAIInteract(performer) == false) return;

			if (electrifyModule != null)
			{
				electrifyModule.ToggleElectrocutionInput();

				if (HackingProcessBase.HasConnection(electrifyModule.ToggleElectrocution) == false)
					Chat.AddExamineMsgFromServer(performer, $"The {DoorName} is wired incorrectly and you can't access the safety mechanism");

				UpdateGUI();
			}
			else
				Chat.AddExamineMsgFromServer(performer, $"The {DoorName} can't be electrified");
		}

		/// <summary>
		/// Allows the player  to open a net tab.  AI is not permitted to open the hacking interface
		/// </summary>
		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			bool isAi = playerObject.GetComponent<PlayerScript>().PlayerType == PlayerTypes.Ai;

			//Block Ai from hacking UI but allow normal player
			if (netTabType == NetTabType.HackingPanel) return isAi == false;

			//Block normal player from Ai door controlling UI
			if (isAi == false) return false;

			return CheckCanAIInteract(playerObject, isOpenNetTab: true);
		}

		/// <summary>
		/// Updates the GUI_Airlock net tab
		/// </summary>
		public void UpdateGUI()
		{
			UpdateGUIEvent?.Invoke();
		}

		/// <summary>
		/// Confirms the AI is connected via the HackingProcessBase wiring
		/// </summary>
		public bool CanAIInteract()
		{
			if (HasPower == false) return false;
			HackingProcessBase.ImpulsePort(AIConnection);
			return HackingProcessBase.HasConnection(AIConnection);
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

			if (master is DoorSwitch doorSwitch) doorSwitch.NewAddDoorControllerFromScene(this);

			else if (master is StatusDisplay statusDisplay) statusDisplay.NewLinkDoor(this);
		}

		#endregion

		#region Admin Functions
		public RightClickableResult GenerateRightClickOptions()
		{
			if (KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions,
				KeyboardInputManager.KeyEventType.Hold) == false) return null;

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
				if (bolts != null) options.AddAdminElement("Toggle Bolts", AdminToggleBolt);
			}

			if (PlayerList.HasTAGClient(TAG.ADMIN_ELECTRIFIE_DOOR))
			{
				add = true;
				if (electrifyModule != null) options.AddAdminElement("Toggle Electrify", AdminToggleElectrify);
			}

			if (add) return options;

			else return null;

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