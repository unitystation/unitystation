using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Messages.Client.NewPlayer;
using Messages.Server;
using AddressableReferences;
using ScriptableObjects;
using Systems.Interaction;
using HealthV2;
using Objects.Wallmounts;
using Shared.Systems.ObjectConnection;
using UnityEngine.Events;


namespace Doors
{
	public class DoorController : NetworkBehaviour, IMultitoolSlaveable, ICheckedInteractable<AiActivate>
	{
		public enum OpeningDirection
		{
			Horizontal,
			Vertical
		}

		public enum PressureLevel
		{
			Safe,
			Caution,
			Warning
		}

		private int closedLayer;
		private int closedSortingLayer;

		public AddressableAudioSource openSFX;

		public AddressableAudioSource closeSFX;

		private IEnumerator coWaitOpened;
		private IEnumerator coBlockAutomaticClosing;
		[Tooltip("how many sprites in the main door animation")]
		public int doorAnimationSize = 6;
		public DoorAnimator doorAnimator;
		[Tooltip("first frame of the light animation")]
		public int DoorDeniedSpriteOffset = 12;
		[Tooltip("first frame of the door Cover/window animation")]
		public int DoorCoverSpriteOffset;
		private int doorDirection;
		[Tooltip("first frame of the light animation")]
		public int DoorLightSpriteOffset;
		[Tooltip("first frame of the door animation")]
		public int DoorSpriteOffset;
		[SerializeField] [Tooltip("SpriteRenderer which is toggled when welded. Existence is equivalent to weldability of door.")]
		private SpriteRenderer weldOverlay = null;
		[SerializeField] private Sprite weldSprite = null;

		public bool IsWeldable => (weldOverlay != null);

		public bool isEmagged;

		public DoorType doorType;

		[Tooltip("Toggle damaging any living entities caught in the door as it closes")]
		public bool damageOnClose = false;

		[Tooltip("Amount of damage when closed on someone.")]
		public float damageClosed = 90;

		[Tooltip("Is this door designed no matter what is under neath it?")]
		public bool ignorePassableChecks;

		[Tooltip("Does this door open automatically when you walk into it?")]
		public bool IsAutomatic = true;

		[NonSerialized] public UnityEvent OnDoorClose = new UnityEvent();

		[NonSerialized] public UnityEvent OnDoorOpen = new UnityEvent();

		public bool enableDisabledCollider = false;

		/// <summary>
		/// Makes registerTile door closed state accessible
		/// </summary>
		public bool IsClosed { get { return registerTile.IsClosed; } set { registerTile.IsClosed = value; } }
		[SyncVar(hook = nameof(SyncIsWelded))]
		[HideInInspector] private bool isWelded = false;
		/// <summary>
		/// Is door welded shut?
		/// </summary>
		public bool IsWelded => isWelded;
		[HideInInspector] public bool isPerformingAction;
		[Tooltip("Does it have a glass window you can see trough?")] public bool isWindowedDoor;
		[Tooltip("Does the door light animation only need 1 frame?")] public bool useSimpleLightAnimation = false;
		[Tooltip("Does the denied light animation only toggle 1 frame on and?")] public bool useSimpleDeniedAnimation = false;
		public float maxTimeOpen = 5;
		private int openLayer;
		private int openSortingLayer;

		[Tooltip("Toggle whether door checks for pressure differences to warn about space wind")]
		public bool enablePressureWarning = false;
		// Upper yellow emergency access lights - 22 for glass door and space ship, 12 for exterior airlocks. 25 for standard airlocks.
		[Tooltip("First frame of the door pressure light animation")]
		public int DoorPressureSpriteOffset = 25;
		// After pressure alert issued, time until it will display the alert again instead of opening.
		private readonly int pressureWarningCooldown = 5;
		private readonly int pressureThresholdCaution = 30; // kPa, both thresholds arbitrarily chosen
		private readonly int pressureThresholdWarning = 120;
		private bool pressureWarnActive = false;

		public PressureLevel CurrentPressureLevel { get; private set; } = PressureLevel.Safe;

		public OpeningDirection openingDirection;
		private RegisterDoor registerTile;
		private Matrix matrix => registerTile.Matrix;

		private TileChangeManager tileChangeManager;

		private AccessRestrictions accessRestrictions;
		public AccessRestrictions AccessRestrictions {
			get {
				if (accessRestrictions == false)
				{
					accessRestrictions = GetComponent<AccessRestrictions>();
				}
				return accessRestrictions;
			}
		}

		private SpriteRenderer spriteRenderer;

		private float inputDelay = 0.5f;
		private float delayStartTime = 0;
		private float delayStartTimeTryOpen = 0;

		private void Awake()
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (registerTile != null) return;
			if (isWindowedDoor == false)
			{
				closedLayer = LayerMask.NameToLayer("Door Closed");
			}
			else
			{
				closedLayer = LayerMask.NameToLayer("Windows");
			}
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			closedSortingLayer = SortingLayer.NameToID("Doors Closed");
			openSortingLayer = SortingLayer.NameToID("Doors Open");
			openLayer = LayerMask.NameToLayer("Door Open");
			registerTile = gameObject.GetComponent<RegisterDoor>();
			tileChangeManager = GetComponentInParent<TileChangeManager>();
		}

		public override void OnStartClient()
		{
			EnsureInit();
			SyncIsWelded(isWelded, isWelded);
			DoorNewPlayer.Send(netId);
		}

		/// <summary>
		/// Invoked by doorAnimator once a door animation finishes
		/// </summary>
		public void OnAnimationFinished(bool isClosing = false)
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
			if (isClosing && CustomNetworkManager.IsServer && registerTile.OneDirectionRestricted == false && ignorePassableChecks == false)
			{
				if (MatrixManager.IsPassableAtAllMatrices(registerTile.WorldPositionServer, registerTile.WorldPositionServer,
					isServer: true, includingPlayers: true, context: this.gameObject) == false)
				{
					//something is in the way, open back up
					//set this field to false so open command will actually work
					isPerformingAction = false;
					Open();
				}
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

		private IEnumerator WaitUntilClose()
		{
			// After the door opens, wait until it's supposed to close.
			yield return WaitFor.Seconds(maxTimeOpen);
			if (CustomNetworkManager.IsServer)
			{
				if (BlockAutoClose == false)
				{
					CloseSignal();
				}
			}
		}

		// 3d sounds
		public void PlayOpenSound()
		{
			if (openSFX != null)
			{
				// Need to play this sound as global - this will ignore muffle effect
				_ = SoundManager.PlayAtPosition(openSFX, registerTile.WorldPosition, gameObject, polyphonic: true, isGlobal: true);
			}
		}

		public void PlayCloseSound()
		{
			if (closeSFX != null)
			{
				_ = SoundManager.PlayAtPosition(closeSFX, registerTile.WorldPosition, gameObject, polyphonic: true, isGlobal: true);
			}
		}

		public void CloseSignal()
		{
			TryClose();
		}

		public void TryClose()
		{
			if (isEmagged) return;

			if (Time.time < delayStartTimeTryOpen + inputDelay) return;

			delayStartTimeTryOpen = Time.time;

			if (IsClosed) return;

			// Sliding door is not passable according to matrix
			if (isPerformingAction == false && (ignorePassableChecks || matrix.CanCloseDoorAt(registerTile.LocalPositionServer, true) || doorType == DoorType.sliding))
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
			// Check if this is null... it is possible for this to be null in Unity - gameObject reference can generate an NRE.
			if (this == null || gameObject == null) return; // probably destroyed by a shuttle crash
			if (Time.time < delayStartTime + inputDelay) return;

			delayStartTime = Time.time;

			IsClosed = true;
			OnDoorClose?.Invoke();
			if (enableDisabledCollider)
			{
				this.GetComponent<Collider2D>().enabled = true;
			}

			if (isPerformingAction == false)
			{
				DoorUpdateMessage.SendToAll(gameObject, DoorUpdateType.Close);
				if (damageOnClose)
				{
					ServerDamageOnClose();
				}
			}
		}

		private void ServerAccessDenied()
		{
			if (isPerformingAction == false)
			{
				DoorUpdateMessage.SendToAll(gameObject, DoorUpdateType.AccessDenied);
			}
		}

		public void MobTryOpen(GameObject originator)
		{
			if (IsClosed == false || isPerformingAction) return;

			if (isEmagged)
			{
				TryOpen(originator);
				return;
			}

			if (AccessRestrictions != null && AccessRestrictions.CheckAccess(originator) == false)
			{
				ServerAccessDenied();
				return;
			}

			TryOpen(originator);
		}

		public void TryOpen(GameObject originator = null, bool blockClosing = false)
		{
			if (Time.time < delayStartTimeTryOpen + inputDelay && isEmagged == false) return;

			delayStartTimeTryOpen = Time.time;

			if (isWelded)
			{
				if (originator)
					Chat.AddExamineMsgFromServer(originator, "This door is welded shut.");
				return;
			}
			if (IsClosed && isPerformingAction == false)
			{
				if (pressureWarnActive == false && DoorUnderPressure() && isEmagged == false)
				{
					ServerPressureWarn();
				}
				else
				{
					Open(blockClosing);
				}
			}
		}

		public void Open(bool blockClosing = false)
		{
			if (this == null || gameObject == null) return; // probably destroyed by a shuttle crash
			if (Time.time < delayStartTime + inputDelay) return;

			delayStartTime = Time.time;
			if (blockClosing == false)
			{
				ResetWaiting();
			}
			IsClosed = false;
			OnDoorOpen?.Invoke();
			
			if (enableDisabledCollider)
			{
				this.GetComponent<Collider2D>().enabled = false;
			}

			if (isPerformingAction == false)
			{
				DoorUpdateMessage.SendToAll(gameObject, DoorUpdateType.Open);
			}
		}

		public void ServerTryWeld()
		{
			if (isPerformingAction == false && (weldOverlay != null))
			{
				ServerWeld();
			}
		}

		public void ServerWeld()
		{
			if (this == null || gameObject == null) return; // probably destroyed by a shuttle crash
			if (isPerformingAction == false)
			{
				SyncIsWelded(isWelded, !isWelded);
			}
		}

		private void SyncIsWelded(bool _wasWelded, bool _isWelded)
		{
			isWelded = _isWelded;
			if (weldOverlay != null) // if door is weldable
			{
				weldOverlay.sprite = isWelded ? weldSprite : null;
			}
		}

		public void ServerDisassemble(HandApply interaction)
		{
			tileChangeManager.MetaTileMap.RemoveTileWithlayer(registerTile.LocalPositionServer, LayerType.Walls);
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, registerTile.WorldPositionServer, count: 4);
			_ = Despawn.ServerSingle(gameObject);
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
			if (maxTimeOpen == -1) return;

			if (coWaitOpened != null)
			{
				StopCoroutine(coWaitOpened);
				coWaitOpened = null;
			}

			coWaitOpened = WaitUntilClose();
			StartCoroutine(coWaitOpened);
		}

		private void CancelWaiting()
		{
			if (coBlockAutomaticClosing != null)
			{
				StopCoroutine(coBlockAutomaticClosing);
				coBlockAutomaticClosing = null;
			}
			coBlockAutomaticClosing = BlockAutomaticClosing();
			StartCoroutine(coBlockAutomaticClosing);
		}

		IEnumerator BlockAutomaticClosing()
		{
			BlockAutoClose = true;
			yield return WaitFor.Seconds(maxTimeOpen + 1);
			BlockAutoClose = false;
		}

		public bool BlockAutoClose;

		IEnumerator PressureWarnDelay()
		{
			yield return WaitFor.Seconds(pressureWarningCooldown);
			pressureWarnActive = false;
		}

		private void ServerPressureWarn()
		{
			DoorUpdateMessage.SendToAll(gameObject, DoorUpdateType.PressureWarn);
			pressureWarnActive = true;
			StartCoroutine(PressureWarnDelay());
		}

		/// <summary>
		///  Checks each side of the door, returns true if not considered safe and updates pressureLevel.
		///  Used to allow the player to be made aware of the pressure difference for safety.
		/// </summary>
		/// <returns></returns>
		private bool DoorUnderPressure()
		{
			if (enablePressureWarning == false)
			{
				// Pressure warning system is disabled, so pretend everything is fine.
				return false;
			}

			// Obtain the adjacent tiles to the door.
			var upMetaNode = MatrixManager.GetMetaDataAt(registerTile.WorldPositionServer + Vector3Int.up);
			var downMetaNode = MatrixManager.GetMetaDataAt(registerTile.WorldPositionServer + Vector3Int.down);
			var leftMetaNode = MatrixManager.GetMetaDataAt(registerTile.WorldPositionServer + Vector3Int.left);
			var rightMetaNode = MatrixManager.GetMetaDataAt(registerTile.WorldPositionServer + Vector3Int.right);

			// Only find the pressure comparison if both opposing sides are atmos. passable.
			// If both sides are not atmos. passable, then we don't care about the pressure difference.
			var vertPressureDiff = 0.0;
			var horzPressureDiff = 0.0;
			if (upMetaNode.IsOccupied == false || downMetaNode.IsOccupied == false)
			{
				vertPressureDiff = Math.Abs(upMetaNode.GasMix.Pressure - downMetaNode.GasMix.Pressure);
			}
			if (leftMetaNode.IsOccupied == false || rightMetaNode.IsOccupied == false)
			{
				horzPressureDiff = Math.Abs(leftMetaNode.GasMix.Pressure - rightMetaNode.GasMix.Pressure);
			}

			// Set pressureLevel according to the pressure difference found.
			if (vertPressureDiff >= pressureThresholdWarning || horzPressureDiff >= pressureThresholdWarning)
			{
				CurrentPressureLevel = PressureLevel.Warning;
				return true;
			}

			if (vertPressureDiff >= pressureThresholdCaution || horzPressureDiff >= pressureThresholdCaution)
			{
				CurrentPressureLevel = PressureLevel.Caution;
				return true;
			}

			CurrentPressureLevel = PressureLevel.Safe;
			return false;
		}

		#region UI Mouse Actions

		public void OnHoverStart()
		{
			if (gameObject.IsAtHiddenPos()) return;
			UIManager.SetToolTip = doorType + " Door";
		}

		public void OnHoverEnd()
		{
			UIManager.SetToolTip = "";
		}

		#endregion

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

		#region Multitool Interaction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.DoorButton;

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
				doorSwitch.AddDoorControllerFromScene(this);
			}
			else if (master is StatusDisplay statusDisplay)
			{
				statusDisplay.LinkDoor(this);
			}
		}

		#endregion

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			//Try open/close
			if (interaction.ClickType == AiActivate.ClickTypes.ShiftClick)
			{
				if (IsClosed)
				{
					TryOpen();
				}
				else
				{
					TryClose();
				}
			}
		}

		#endregion
	}
}
