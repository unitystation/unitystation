using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class DoorController : NetworkBehaviour, IServerSpawn
{
	//public bool isWindowed = false;
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
	public string openSFX = "AirlockOpen", closeSFX = "AirlockClose";

	private IEnumerator coWaitOpened;
	[Tooltip("how many sprites in the main door animation")] public int doorAnimationSize = 6;
	public DoorAnimator doorAnimator;
	[Tooltip("first frame of the light animation")] public int DoorDeniedSpriteOffset = 12;
	[Tooltip("first frame of the door Cover/window animation")] public int DoorCoverSpriteOffset;
	private int doorDirection;
	[Tooltip("first frame of the light animation")] public int DoorLightSpriteOffset;
	[Tooltip("first frame of the door animation")] public int DoorSpriteOffset;
	[SerializeField] [Tooltip("SpriteRenderer which is toggled when welded. Existence is equivalent to weldability of door.")] private SpriteRenderer weldOverlay = null;
	[SerializeField] private Sprite weldSprite = null;
	/// <summary>
	/// Is door weldedable?
	/// </summary>
	public bool IsWeldable => (weldOverlay != null);

	public DoorType doorType;

	[Tooltip("Toggle damaging any living entities caught in the door as it closes")]
	public bool damageOnClose = false;

	[Tooltip("Amount of damage when closed on someone.")]
	public float damageClosed = 90;

	[Tooltip("Is this door designed no matter what is under neath it?")]
	public bool ignorePassableChecks;

	[Tooltip("Does this door open automatically when you walk into it?")]
	public bool IsAutomatic = true;

	/// <summary>
	/// Makes registerTile door closed state accessible
	/// </summary>
	public bool IsClosed { get { return registerTile.IsClosed; } set { registerTile.IsClosed = value; } }
	[SyncVar(hook = nameof(SyncIsWelded))]
	[HideInInspector]private bool isWelded = false;
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
	private int pressureWarningCooldown = 5;
	private int pressureThresholdCaution = 30; // kPa, both thresholds arbitrarily chosen
	private int pressureThresholdWarning = 120;
	private bool pressureWarnActive = false;
	[HideInInspector] public PressureLevel pressureLevel = PressureLevel.Safe;

	public OpeningDirection openingDirection;
	private RegisterDoor registerTile;
	private Matrix matrix => registerTile.Matrix;

	private TileChangeManager tileChangeManager;

	private AccessRestrictions accessRestrictions;
	public AccessRestrictions AccessRestrictions {
		get {
			if ( !accessRestrictions ) {
				accessRestrictions = GetComponent<AccessRestrictions>();
			}
			return accessRestrictions;
		}
	}

	[HideInInspector] public SpriteRenderer spriteRenderer;


	private HackingProcessBase hackingProcess;
	public HackingProcessBase HackingProcess => hackingProcess;

	private bool isHackable;
	public bool IsHackable => isHackable;

	private bool hackingLoaded;

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
		if (!isWindowedDoor)
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

		hackingProcess = GetComponent<HackingProcessBase>();
		isHackable = hackingProcess != null;
		hackingLoaded = false;
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
	public void OnAnimationFinished()
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
		if (CustomNetworkManager.IsServer && IsClosed && !registerTile.OneDirectionRestricted && !ignorePassableChecks)
		{
			if (!MatrixManager.IsPassableAt(registerTile.WorldPositionServer, registerTile.WorldPositionServer,
				isServer: true, includingPlayers: true, context: this.gameObject))
			{
				//something is in the way, open back up
				//set this field to false so open command will actually work
				isPerformingAction = false;
				ServerOpen();
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
			ServerTryClose();
		}
	}

	//3d sounds
	public void PlayOpenSound()
	{
		if (openSFX != null)
		{
			// Need to play this sound as global - this will ignore muffle effect
			SoundManager.PlayAtPosition(openSFX, registerTile.WorldPosition, gameObject, polyphonic: true, isGlobal: true);
		}
	}

	public void PlayCloseSound()
	{
		if (closeSFX != null)
		{
			SoundManager.PlayAtPosition(closeSFX, registerTile.WorldPosition, gameObject, polyphonic: true, isGlobal: true);
		}
	}

	public void ServerTryClose()
	{
		if (Time.time < delayStartTimeTryOpen + inputDelay) return;

		delayStartTimeTryOpen = Time.time;

		// Sliding door is not passable according to matrix
		if( !IsClosed && !isPerformingAction && (ignorePassableChecks || matrix.CanCloseDoorAt( registerTile.LocalPositionServer, true ) || doorType == DoorType.sliding) )
		{
			if (isHackable && hackingLoaded)
			{
				HackingNode onShouldClose = hackingProcess.GetNodeWithInternalIdentifier("OnShouldClose");
				onShouldClose.SendOutputToConnectedNodes();
			}
			ServerClose();
		}
		else
		{
			ResetWaiting();
		}
	}

	public void ServerClose() {
		if (gameObject == null) return; // probably destroyed by a shuttle crash
		if (Time.time < delayStartTime + inputDelay) return;

		delayStartTime = Time.time;

		IsClosed = true;

		if (isHackable && hackingLoaded)
		{
			HackingNode onDoorClosed = hackingProcess.GetNodeWithInternalIdentifier("OnDoorOpened");
			onDoorClosed.SendOutputToConnectedNodes();
		}

		if ( !isPerformingAction ) {
			DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Close );
			if (damageOnClose)
			{
				ServerDamageOnClose();
			}
		}
	}

	public void ServerTryOpen(GameObject Originator)
	{

		if (Time.time < delayStartTimeTryOpen + inputDelay) return;

		delayStartTimeTryOpen = Time.time;

		if (isWelded)
		{
			Chat.AddExamineMsgFromServer(Originator, "This door is welded shut.");
			return;
		}
		if (AccessRestrictions != null)
		{
			if (!AccessRestrictions.CheckAccess(Originator))
			{
				if (IsClosed && !isPerformingAction)
				{
					if (isHackable && hackingLoaded)
					{
						HackingNode onIDRejected = hackingProcess.GetNodeWithInternalIdentifier("OnIDRejected");
						onIDRejected.SendOutputToConnectedNodes(Originator);
					}
					else
					{
						ServerAccessDenied();
					}
					return;
				}
			}
		}

		if (IsClosed && !isPerformingAction)
		{
			if (!pressureWarnActive && DoorUnderPressure())
			{
				if (isHackable && hackingLoaded)
				{
					HackingNode shouldDoPressureWarn = hackingProcess.GetNodeWithInternalIdentifier("ShouldDoPressureWarning");
					shouldDoPressureWarn.SendOutputToConnectedNodes(Originator);
				}
				else
				{
					ServerPressureWarn();
				}
			}
			else
			{
				if (isHackable && hackingLoaded)
				{
					HackingNode onShouldOpen = hackingProcess.GetNodeWithInternalIdentifier("OnShouldOpen");
					onShouldOpen.SendOutputToConnectedNodes(Originator);
				}
				else
				{
					ServerOpen();
				}
			}
		}
	}


	private void ServerAccessDenied() {
		if ( !isPerformingAction ) {
			DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.AccessDenied );
		}
	}

	public void ServerOpen()
	{
		if (this == null || gameObject == null) return; // probably destroyed by a shuttle crash
		if (Time.time < delayStartTime + inputDelay) return;

		delayStartTime = Time.time;

		ResetWaiting();
		IsClosed = false;

		if (isHackable && hackingLoaded)
		{
			HackingNode onDoorOpened = hackingProcess.GetNodeWithInternalIdentifier("OnDoorOpened");
			onDoorOpened.SendOutputToConnectedNodes();
		}

		if (!isPerformingAction)
		{
			DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Open );
		}
	}

	public void ServerTryWeld()
	{
		if (!isPerformingAction && (weldOverlay != null))
		{
			ServerWeld();
		}
	}

	public void ServerWeld()
	{
		if (this == null || gameObject == null) return; // probably destroyed by a shuttle crash
		if (!isPerformingAction)
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
		tileChangeManager.RemoveTile(registerTile.LocalPositionServer, LayerType.Walls);
		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, registerTile.WorldPositionServer, count: 4);
		Despawn.ServerSingle(gameObject);
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
		if (maxTimeOpen == -1) return;

		if (coWaitOpened != null)
		{
			StopCoroutine(coWaitOpened);
			coWaitOpened = null;
		}

		coWaitOpened = WaitUntilClose();
		StartCoroutine(coWaitOpened);
	}

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
		if (!enablePressureWarning)
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
		if (!upMetaNode.IsOccupied || !downMetaNode.IsOccupied)
		{
			vertPressureDiff = Math.Abs(upMetaNode.GasMix.Pressure - downMetaNode.GasMix.Pressure);
		}
		if (!leftMetaNode.IsOccupied || !rightMetaNode.IsOccupied)
		{
			horzPressureDiff = Math.Abs(leftMetaNode.GasMix.Pressure - rightMetaNode.GasMix.Pressure);
		}

		// Set pressureLevel according to the pressure difference found.
		if (vertPressureDiff >= pressureThresholdWarning || horzPressureDiff >= pressureThresholdWarning)
		{
			pressureLevel = PressureLevel.Warning;
			return true;
		}
		else if (vertPressureDiff >= pressureThresholdCaution || horzPressureDiff >= pressureThresholdCaution)
		{
			pressureLevel = PressureLevel.Caution;
			return true;
		}
		else
		{
			pressureLevel = PressureLevel.Safe;
			return false;
		}
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
		if (!IsClosed)
		{
			DoorUpdateMessage.Send(playerConn, gameObject, DoorUpdateType.Open, true);
		}
	}

	private void ServerElectrocute(GameObject obj)
	{
		float r = UnityEngine.Random.value;
		if (r < 0.45)
		{
			PlayerScript ply = obj.GetComponent<PlayerScript>();
			if (ply != null)
			{
				hackingProcess.HackingGUI.RemovePlayer(ply.gameObject);
				TabUpdateMessage.Send(ply.gameObject, hackingProcess.HackingGUI.Provider, NetTabType.HackingPanel, TabAction.Close);
				var playerLHB = obj.GetComponent<LivingHealthBehaviour>();
				var electrocution = new Electrocution(9080, registerTile.WorldPositionServer, "wire");
				if (playerLHB != null) playerLHB.Electrocute(electrocution);
			}
		}

	}

	public void LinkHackNodes()
	{

		HackingNode openDoor = hackingProcess.GetNodeWithInternalIdentifier("OpenDoor");
		openDoor.AddToInputMethods(ServerOpen);

		HackingNode closeDoor = hackingProcess.GetNodeWithInternalIdentifier("CloseDoor");
		closeDoor.AddToInputMethods(ServerClose);

		HackingNode beginOpenProcedure = hackingProcess.GetNodeWithInternalIdentifier("BeginOpenProcedure");
		beginOpenProcedure.AddToInputMethods(ServerTryOpen);

		HackingNode beginCloseProcedure = hackingProcess.GetNodeWithInternalIdentifier("BeginCloseProcedure");
		beginCloseProcedure.AddToInputMethods(ServerTryClose);

		HackingNode onAttemptOpen = hackingProcess.GetNodeWithInternalIdentifier("OnAttemptOpen");
		onAttemptOpen.AddConnectedNode(beginOpenProcedure);

		HackingNode onAttemptClose = hackingProcess.GetNodeWithInternalIdentifier("OnAttemptClose");
		onAttemptClose.AddConnectedNode(beginCloseProcedure);

		HackingNode onShouldOpen = hackingProcess.GetNodeWithInternalIdentifier("OnShouldOpen");
		onShouldOpen.AddWireCutCallback(ServerElectrocute);
		onShouldOpen.AddConnectedNode(openDoor);

		HackingNode onShouldClose = hackingProcess.GetNodeWithInternalIdentifier("OnShouldClose");
		onShouldClose.AddWireCutCallback(ServerElectrocute);
		onShouldClose.AddConnectedNode(closeDoor);

		HackingNode acceptID = hackingProcess.GetNodeWithInternalIdentifier("AcceptId");

		HackingNode rejectID = hackingProcess.GetNodeWithInternalIdentifier("RejectID");
		rejectID.AddToInputMethods(ServerAccessDenied);

		HackingNode onIDRejected = hackingProcess.GetNodeWithInternalIdentifier("OnIDRejected");
		onIDRejected.AddConnectedNode(rejectID);

		HackingNode doPressureWarning = hackingProcess.GetNodeWithInternalIdentifier("DoPressureWarning");
		doPressureWarning.AddToInputMethods(ServerPressureWarn);

		HackingNode shouldDoPressureWarning = hackingProcess.GetNodeWithInternalIdentifier("ShouldDoPressureWarning");
		shouldDoPressureWarning.AddConnectedNode(doPressureWarning);

		HackingNode onDoorOpened = hackingProcess.GetNodeWithInternalIdentifier("OnDoorOpened");

		HackingNode onDoorClosed = hackingProcess.GetNodeWithInternalIdentifier("OnDoorClosed");

		HackingNode powerIn = hackingProcess.GetNodeWithInternalIdentifier("PowerIn");

		HackingNode powerOut = hackingProcess.GetNodeWithInternalIdentifier("PowerOut");
		powerOut.AddConnectedNode(powerIn);
		powerOut.AddWireCutCallback(ServerElectrocute);

		HackingNode dummyIn = hackingProcess.GetNodeWithInternalIdentifier("DummyIn");

		HackingNode dummyOut = hackingProcess.GetNodeWithInternalIdentifier("DummyOut");
		dummyOut.AddConnectedNode(dummyIn);

		hackingLoaded = true;

	}

	public void OnSpawnServer(SpawnInfo info)
	{

	}

}
