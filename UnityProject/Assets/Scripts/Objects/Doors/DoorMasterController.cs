using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Doors.Modules;
using Mirror;
using UnityEngine;
using Systems.Electricity;

//TODO: Need to reimplement hacking with this system. Might be a nightmare, dk yet.
namespace Doors
{
	/// <summary>
	/// This is the master 'controller' for the door. It handles interactions by players and passes any interactions it need to to its components.
	/// </summary>
	public class DoorMasterController : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		#region inspector
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
		[Tooltip("Is this door designed no matter what is under neath it?")]
		private bool ignorePassableChecks = false;

		//Maximum time the door will remain open before closing itself.
		[SerializeField][Tooltip("Time this door will wait until autoclosing")]
		private float maxTimeOpen = 5;

		[SerializeField]
		[Tooltip("Prevent the door from auto closing when opened.")]
		private bool blockAutoClose = false;

		private DoorAnimatorV2 doorAnimator;
		public DoorAnimatorV2 DoorAnimator => doorAnimator;

		private const float INPUT_COOLDOWN = 1f;

		#endregion

		public bool IsClosed
		{
			get => registerTile.IsClosed;
			private set => registerTile.IsClosed = value;
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

		private Matrix matrix => registerTile.Matrix;
		private List<DoorModuleBase> modulesList;
		private APCPoweredDevice apc;
		public APCPoweredDevice Apc => apc;


		private void Awake()
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			registerTile = GetComponent<RegisterDoor>();

			//Get out list of modules for use later.
			modulesList = GetComponentsInChildren<DoorModuleBase>().ToList();

			//Our APC powered device.
			apc = GetComponent<APCPoweredDevice>();

			doorAnimator = GetComponent<DoorAnimatorV2>();

			doorAnimator.AnimationFinished += OnAnimationFinished;
		}

		public override void OnStartClient()
		{
			EnsureInit();
			DoorNewPlayer.Send(netId);
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

			foreach (var module in modulesList)
			{
				ModuleSignal signal = module.BumpingInteraction(byPlayer);

				if (!module.CanDoorStateChange() || signal == ModuleSignal.ContinueWithoutDoorStateChange)
				{
					canOpen = false;
				}

				if (signal == ModuleSignal.SkipRemaining || signal == ModuleSignal.Break)
				{
					StartInputCoolDown();
					break;
				}
			}

			if (!isPerformingAction && canOpen)
			{
				TryOpen(byPlayer);
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
			bool canClose = true;
			foreach (DoorModuleBase module in modulesList)
			{
				ModuleSignal signal = module.OpenInteraction(interaction);

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

			if (!isPerformingAction && canClose)
			{
				TryClose(interaction.Performer);
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
			foreach (DoorModuleBase module in modulesList)
			{
				ModuleSignal signal = module.ClosedInteraction(interaction);

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

			if (!isPerformingAction && canOpen)
			{
				TryOpen(interaction.Performer);
			}
		}

		public void TryOpen(GameObject originator = null, bool blockClosing = false)
		{
			if (IsClosed && !isPerformingAction && HasPower)
			{
				Open(blockClosing);
			}
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

		public void TryClose(GameObject originator = null, bool force = false)
		{
			// Sliding door is not passable according to matrix
			if(!isPerformingAction &&
				(ignorePassableChecks || matrix.CanCloseDoorAt( registerTile.LocalPositionServer, true )) &&
				HasPower || force)
			{
				Close();
			}
			else
			{
				ResetWaiting();
			}
		}

		public void Close()
		{
			if (!gameObject) return; // probably destroyed by a shuttle crash

			IsClosed = true;

			StartCoroutine(doorAnimator.PlayClosingAnimation(lights: HasPower));

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
			StartCoroutine(doorAnimator.PlayOpeningAnimation(lights: HasPower));

			if (!isPerformingAction)
			{
				DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Open );
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
			foreach ( LivingHealthBehaviour healthBehaviour in matrix.Get<LivingHealthBehaviour>(registerTile.LocalPositionServer, true) )
			{
				healthBehaviour.ApplyDamage(gameObject, damageClosed, AttackType.Melee, DamageType.Brute);
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
	}
}