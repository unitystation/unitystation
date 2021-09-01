using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UI.Core.Net;
using UnityEngine;
using Mirror;
using Core.Editor.Attributes;
using Messages.Client.NewPlayer;
using Messages.Server;
using Systems.Electricity;
using Systems.Interaction;
using Doors.Modules;
using HealthV2;


//TODO: Need to reimplement hacking with this system. Might be a nightmare, dk yet.
namespace Doors
{
	/// <summary>
	/// This is the master 'controller' for the door. It handles interactions by players and passes any interactions it need to to its components.
	/// </summary>
	public class DoorMasterController : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<AiActivate>, ICanOpenNetTab
	{
		#region inspector
		[SerializeField, PrefabModeOnly]
		[Tooltip("Toggle damaging any living entities caught in the door as it closes")]
		private bool damageOnClose = false;

		[SerializeField, PrefabModeOnly]
		[Tooltip("Amount of damage when closed on someone.")]
		private float damageClosed = 90;

		[SerializeField, PrefabModeOnly]
		[Tooltip("Does this door open automatically when you walk into it?")]
		private bool isAutomatic = true;

		[SerializeField, PrefabModeOnly]
		[Tooltip("Is this door designed no matter what is under neath it?")]
		private bool ignorePassableChecks = false;

		//Maximum time the door will remain open before closing itself.
		[SerializeField, PrefabModeOnly]
		[Tooltip("Time this door will wait until autoclosing")]
		private float maxTimeOpen = 5;

		[SerializeField, PrefabModeOnly]
		[Tooltip("Prevent the door from auto closing when opened.")]
		private bool blockAutoClose = false;

		private DoorAnimatorV2 doorAnimator;
		public DoorAnimatorV2 DoorAnimator => doorAnimator;

		private const float INPUT_COOLDOWN = 1f;

		#endregion

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
		public bool HasPower => APCPoweredDevice.IsOn(apc.State);


		private RegisterDoor registerTile;
		public RegisterDoor RegisterTile => registerTile;
		private SpriteRenderer spriteRenderer;

		private Matrix matrix => registerTile.Matrix;

		private List<DoorModuleBase> modulesList;
		public List<DoorModuleBase> ModulesList => modulesList;

		private APCPoweredDevice apc;
		public APCPoweredDevice Apc => apc;

		[PrefabModeOnly]
		[Tooltip("Does it have a glass window you can see trough?")]
		public bool isWindowedDoor;

		private int openLayer;
		private int openSortingLayer;
		private int closedLayer;
		private int closedSortingLayer;

		private void Awake()
		{
			if (isWindowedDoor == false)
			{
				closedLayer = LayerMask.NameToLayer("Door Closed");
			}
			else
			{
				closedLayer = LayerMask.NameToLayer("Windows");
			}
			closedSortingLayer = SortingLayer.NameToID("Doors Closed");
			openLayer = LayerMask.NameToLayer("Door Open");
			openSortingLayer = SortingLayer.NameToID("Doors Open");
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			registerTile = GetComponent<RegisterDoor>();
			modulesList = GetComponentsInChildren<DoorModuleBase>().ToList();
			apc = GetComponent<APCPoweredDevice>();
			doorAnimator = GetComponent<DoorAnimatorV2>();
			doorAnimator.AnimationFinished += OnAnimationFinished;
		}

		public override void OnStartClient()
		{
			DoorNewPlayer.Send(netId);
		}

		/// <summary>
		/// Used when player is joining, tells player to open the door if it is opened.
		/// </summary>
		public void UpdateNewPlayer(NetworkConnection playerConn)
		{
			if (IsClosed)
			{
				DoorUpdateMessage.Send(playerConn, gameObject, DoorUpdateType.Close, true);
			}
			else
			{
				DoorUpdateMessage.Send(playerConn, gameObject, DoorUpdateType.Open, true);
			}
		}

		/// <summary>
		/// Invoke this on server when player bumps into door to try to open it.
		/// </summary>
		public void Bump(GameObject byPlayer)
		{
			if (!isAutomatic || !allowInput)
			{
				return;
			}

			bool canOpen = true;
			HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
			foreach (var module in modulesList)
			{
				ModuleSignal signal = module.BumpingInteraction(byPlayer, states);

				if (!module.CanDoorStateChange() || signal == ModuleSignal.ContinueWithoutDoorStateChange)
				{
					canOpen = false;
				}

				if(signal == ModuleSignal.ContinueRegardlessOfOtherModulesStates)
				{
					//(Max): This is to prevent some modules breaking some door behavior and rendering them un-useable.
					//Only use this signal if you're module's logic is being interrupted by other
					//modules that are sending ContinueWithoutDoorStateChange as a signal.
					canOpen = true;
					break;
				}

				if (signal == ModuleSignal.SkipRemaining || signal == ModuleSignal.Break)
				{
					StartInputCoolDown();
					break;
				}
			}

			if (!isPerformingAction && canOpen && CheckStatusAllow(states))
			{
				TryOpen(byPlayer);
			}
			else if(HasPower == false)
			{
				Chat.AddExamineMsgFromServer(byPlayer, $"{gameObject.ExpensiveName()} is unpowered");
			}

			StartInputCoolDown();
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//When a player interacts with the door, we must first check with each module on what to do.
			//For instance, if one of the modules has locked the door, that module will want to prevent us from
			//opening the door.
			if (!IsClosed)
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
			HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
			bool canClose = true;
			foreach (DoorModuleBase module in modulesList)
			{
				ModuleSignal signal = module.OpenInteraction(interaction, states);

				if (!module.CanDoorStateChange() || signal == ModuleSignal.ContinueWithoutDoorStateChange)
				{
					canClose = false;
				}

				if (signal == ModuleSignal.SkipRemaining)
				{
					StartInputCoolDown();
					break;
				}

				if (signal == ModuleSignal.Break)
				{
					StartInputCoolDown();
					return;
				}
			}

			if (!isPerformingAction && canClose && CheckStatusAllow(states))
			{
				TryClose(interaction.Performer, OverrideLogic: true);
			}

			StartInputCoolDown();
		}

		/// <summary>
		/// These two methods are called when the door is interacted with, either opened or closed.
		/// They're separated so that the modules can handle interactions differently when either open or closed.
		/// </summary>
		/// <param name="interaction"></param>
		public void ClosedInteraction(HandApply interaction)
		{
			bool canOpen = true;
			HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
			foreach (DoorModuleBase module in modulesList)
			{
				ModuleSignal signal = module.ClosedInteraction(interaction, states);

				if (!module.CanDoorStateChange() || signal == ModuleSignal.ContinueWithoutDoorStateChange)
				{
					canOpen = false;
				}


				if (signal == ModuleSignal.SkipRemaining)
				{
					break;
				}

				if (signal == ModuleSignal.Break)
				{
					return;
				}
			}

			if (!isPerformingAction && (canOpen) && CheckStatusAllow(states))
			{
				TryOpen(interaction.Performer);
			}
			else if(HasPower == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} is unpowered");
			}
		}

		public bool CheckStatusAllow(HashSet<DoorProcessingStates> states)
		{
			if (states.Contains(DoorProcessingStates.SoftwarePrevented))
			{
				return states.Contains(DoorProcessingStates.SoftwareHacked);
			}
			else
			{
				return true;
			}
		}

		public void TryOpen(GameObject originator, bool blockClosing = false)
		{
			if(IsClosed == false || isPerformingAction) return;

			if(HasPower == false)
			{
				Chat.AddExamineMsgFromServer(originator, $"{gameObject.ExpensiveName()} is unpowered");
				return;
			}

			Open(blockClosing);
		}

		/// <summary>
		/// Try to force the door open regardless of access/internal fuckery.
		/// Purely check to see if there is something physically restraining the door from being opened such as a weld or door bolts.
		///	This would be in situations like as prying the door with a crowbar.
		/// </summary>
		public void TryForceOpen()
		{
			if (!IsClosed) return; //Can't open if we are open. Figures.

			foreach (DoorModuleBase module in modulesList)
			{
				if (!module.CanDoorStateChange())
				{
					return;
				}
			}

			Open();
		}

		/// <summary>
		/// Try to force the door closed regardless of access/internal fuckery.
		/// Purely check to see if there is something physically restraining the door from being closed such as a weld or door bolts.
		/// </summary>
		public void TryForceClose()
		{
			if (IsClosed) return; //Can't close if we are close. Figures.

			foreach (DoorModuleBase module in modulesList)
			{
				if (!module.CanDoorStateChange())
				{
					return;
				}
			}

			Close();
		}

		public void TryClose(GameObject originator = null, bool force = false, bool OverrideLogic = false)
		{
			// Sliding door is not passable according to matrix
			if(!isPerformingAction &&
				(ignorePassableChecks || matrix.CanCloseDoorAt( registerTile.LocalPositionServer, true )) &&
				(HasPower || force ) )

			{
				if (OverrideLogic)
				{
					Close();
				}
				else
				{
					HashSet<DoorProcessingStates> states = new HashSet<DoorProcessingStates>();
					bool canClose = true;
					foreach (DoorModuleBase module in modulesList)
					{
						ModuleSignal signal = module.OpenInteraction(null, states);

						if (!module.CanDoorStateChange() || signal == ModuleSignal.ContinueWithoutDoorStateChange)
						{
							canClose = false;
						}

						if (signal == ModuleSignal.SkipRemaining)
						{
							break;
						}

						if (signal == ModuleSignal.Break)
						{
							ResetWaiting();
							return;
						}
					}

					if (!isPerformingAction && canClose && CheckStatusAllow(states))
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

			if(HasPower == false && originator != null)
			{
				Chat.AddExamineMsgFromServer(originator, $"{gameObject.ExpensiveName()} is unpowered");
			}
		}

		public void Close()
		{
			if (!gameObject) return; // probably destroyed by a shuttle crash

			IsClosed = true;
			UpdateGui();

			if (isPerformingAction)
			{
				return;
			}

			DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Close );

			if (damageOnClose)
			{
				ServerDamageOnClose();
			}
		}

		public void Open(bool blockClosing = false)
		{
			if (!this || !gameObject) return; // probably destroyed by a shuttle crash

			if (!blockClosing)
			{
				ResetWaiting();
			}

			IsClosed = false;
			UpdateGui();

			if (!isPerformingAction)
			{
				DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Open );
			}
		}

		public void BoxCollToggleOn()
		{
			IsClosed = true;
			SetLayer(closedLayer);
			spriteRenderer.sortingLayerID = closedSortingLayer;
		}

		public void BoxCollToggleOff()
		{
			IsClosed = false;
			SetLayer(openLayer);
			spriteRenderer.sortingLayerID = openSortingLayer;
		}

		private void SetLayer(int layer)
		{
			gameObject.layer = layer;
			foreach (Transform child in transform)
			{
				child.gameObject.layer = layer;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!allowInput ||
			    !DefaultWillInteract.Default(interaction, side) ||
			    interaction.TargetObject != gameObject)
			{
				return false;
			}

			//jaws of life
			if (interaction.HandObject != null &&
			    Validations.HasItemTrait(interaction.HandObject.gameObject, CommonTraits.Instance.CanPryDoor))
			{
				return true;
			}
			//crowbars
			if (interaction.HandObject != null &&
			    Validations.HasItemTrait(interaction.HandObject.gameObject, CommonTraits.Instance.Crowbar))
			{
				return true;
			}
			//screwdrivers
			if (interaction.HandObject != null &&
			    Validations.HasItemTrait(interaction.HandObject.gameObject, CommonTraits.Instance.Screwdriver))
			{
				return true;//TODO check if clicking on panel region
			}
			//welders
			if (interaction.HandObject != null &&
			    Validations.HasUsedActiveWelder(interaction))
			{
				return true;
			}

			//TODO add pins here//TODO check if clicking on pins region

			if (interaction.HandObject && interaction.Intent == Intent.Harm)
			{
				return false; // False to allow melee
			}

			return true;
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
			//set this field to false so open command will actually work
			isPerformingAction = false;
			Open();
		}

		private void ServerDamageOnClose()
		{
			foreach (var healthBehaviour in matrix.Get<LivingHealthMasterBase>(registerTile.LocalPositionServer, true) )
			{
				healthBehaviour.ApplyDamageAll(gameObject, damageClosed, AttackType.Melee, DamageType.Brute);
			}
		}

		private void ResetWaiting()
		{
			if (maxTimeOpen == -1)
			{
				return;
			}

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
			if (CustomNetworkManager.IsServer &&
			    !blockAutoClose &&
			    isAutomatic &&
			    HasPower)
			{
				TryClose();
			}
		}

		public void ToggleBlockAutoClose(bool newState)
		{
			blockAutoClose = newState;
		}

		#region Ai interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			//Normal click should open door UI instead
			if (interaction.ClickType == AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			if (HasPower == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Door is unpowered");
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
					TryForceClose();
				}

				return;
			}

			//Toggle bolts
			if (interaction.ClickType == AiActivate.ClickTypes.CtrlClick)
			{
				foreach (var module in modulesList)
				{
					if(module is BoltsModule bolts)
					{
						//Toggle bolts
						bolts.SetBoltsState(!bolts.BoltsDown);
						return;
					}
				}
			}
		}

		#endregion

		#region Airlock UI

		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			//Only checking airlock, so when hacking UI reimplemented this check wont happen
			//Return true so it doesnt block those checks
			//TODO block Ai from hacking UI
			if (netTabType != NetTabType.Airlock) return true;

			if (HasPower == false)
			{
				Chat.AddExamineMsgFromServer(playerObject, "Door is unpowered");
				return false;
			}

			//Only allow AI to open airlock control UI
			return playerObject.GetComponent<PlayerScript>().PlayerState == PlayerScript.PlayerStates.Ai;
		}

		public void UpdateGui()
		{
			var peppers = NetworkTabManager.Instance.GetPeepers(gameObject, NetTabType.Airlock);
			if(peppers.Count == 0) return;

			List<ElementValue> valuesToSend = new List<ElementValue>();

			valuesToSend.Add(new ElementValue() { Id = "OpenLabel", Value = Encoding.UTF8.GetBytes(IsClosed ? "Closed" : "Open") });

			foreach (var module in modulesList)
			{
				if(module is BoltsModule bolts)
				{
					valuesToSend.Add(new ElementValue() { Id = "BoltLabel", Value = Encoding.UTF8.GetBytes(bolts.BoltsDown ? "Bolted" : "Unbolted") });
				}

				if (module is ElectrifiedDoorModule electric)
				{
					valuesToSend.Add(new ElementValue() { Id = "ShockStateLabel", Value = Encoding.UTF8.GetBytes(electric.IsElectrecuted ? "DANGER" : "SAFE") });
				}
			}

			// Update all UI currently opened.
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Airlock, TabAction.Update, valuesToSend.ToArray());
		}

		#endregion
	}
}
