using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace Doors
{
	//These are used by modules when signaling to the master controller what to do when looping through modules.
	public enum ModuleSignal
	{
		Continue, //Continue executing through modules.
		Break, //Prevent any further execution, including door masters own methods.
		SkipRemaining, //Skip the remaining modules, but continue with the door masters methods.
	}
//This is the master 'controller' for the door. It handles interactions by players and passes any interactions it need to to its components.
public class DoorMasterController : NetworkBehaviour, IPredictedCheckedInteractable<HandApply>
{
	public enum OpeningDirection
	{
		Horizontal,
		Vertical
	}

	//Whether or not users can interact with the door.
	private bool allowInput = true;

	public DoorType doorType;

	[Tooltip("Toggle damaging any living entities caught in the door as it closes")]
	public bool damageOnClose = false;

	[Tooltip("Amount of damage when closed on someone.")]
	public float damageClosed = 90;

	[Tooltip("Does this door open automatically when you walk into it?")]
	public bool IsAutomatic = true;

	[Tooltip("Is this door designed no matter what is under neath it?")]
	public bool ignorePassableChecks;

	//Maximum time the door will remain open before closing itself.
	public float maxTimeOpen = 5;

	[Tooltip("Prevent the door from auto closing when open.")]
	public bool BlockAutoClose;

	public bool IsClosed { get { return registerTile.IsClosed; } set { registerTile.IsClosed = value; } }

	private int closedLayer;
	private int closedSortingLayer;

	private int openLayer;
	private int openSortingLayer;

	public string openSFX = "AirlockOpen", closeSFX = "AirlockClose";

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

	[HideInInspector]
	public bool isPerformingAction;

	[Tooltip("Does it have a glass window you can see trough?")]
	public bool isWindowedDoor;

	[Tooltip("Does the door light animation only need 1 frame?")]
	public bool useSimpleLightAnimation = false;

	[Tooltip("Does the denied light animation only toggle 1 frame on and?")]
	public bool useSimpleDeniedAnimation = false;

	public OpeningDirection openingDirection;
	private RegisterDoor registerTile;
	private Matrix matrix => registerTile.Matrix;

	[HideInInspector]
	public SpriteRenderer spriteRenderer;

	private List<DoorModuleBase> modulesList;

	private void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (!registerTile) return;
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

		//Get out list of modules for use later.
		modulesList = GetComponents<DoorModuleBase>().ToList();
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

	}

	//These two methods are called when the door is interacted with, either opened or closed.
	//They're separated so that the modules can handle interactions differently when either open or closed.
	private void OpenInteraction(HandApply interaction)
	{
		foreach (DoorModuleBase module in modulesList)
		{
			ModuleSignal signal = module.OpenInteraction(interaction);

			if (signal == ModuleSignal.SkipRemaining)
			{
				break;
			}
			else if (signal == ModuleSignal.Break)
			{
				return;
			}
		}

		TryClose(interaction.Performer);
	}

	private void ClosedInteraction(HandApply interaction)
	{
		foreach (DoorModuleBase module in modulesList)
		{
			ModuleSignal signal = module.ClosedInteraction(interaction);

			if (signal == ModuleSignal.SkipRemaining)
			{
				break;
			}
			else if (signal == ModuleSignal.Break)
			{
				return;
			}
		}

		TryOpen(interaction.Performer);
	}

	//Nothing to predict.
	//This may change. Might be worth calling this on the modules on the door.
	public void ClientPredictInteraction(HandApply interaction) {}

	//Nothing to rollback.
	public void ServerRollbackClient(HandApply interaction) {}

	public bool WillInteract(HandApply interaction, NetworkSide side)
    {
	    if (!DefaultWillInteract.Default(interaction, side)) return false;
	    if (interaction.TargetObject != gameObject) return false;
	    if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false; // False to allow melee

	    return allowInput;
    }

    public void StartInputCoolDown()
    {
	    allowInput = false;
	    StartCoroutine(DoorInputCoolDown());
    }

    private IEnumerator DoorInputCoolDown()
    {
	    yield return WaitFor.Seconds(0.3f);
	    allowInput = true;
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
			    Open();
		    }
	    }

    }

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

    public void Close() {
	    if (!gameObject) return; // probably destroyed by a shuttle crash

	    IsClosed = true;

	    if ( !isPerformingAction ) {
		    DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Close );
		    if (damageOnClose)
		    {
			    ServerDamageOnClose();
		    }
	    }
    }

    public void Open(bool blockClosing = false)
    {
	    if (this == null || gameObject == null) return; // probably destroyed by a shuttle crash

	    if (!blockClosing)
	    {
		    ResetWaiting();
	    }
	    IsClosed = false;

	    if (!isPerformingAction)
	    {
		    DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Open );
	    }
    }

    public void TryOpen(GameObject originator)
    {

    }

    public void TryClose(GameObject originator = null)
    {
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

    private IEnumerator WaitUntilClose()
    {
	    // After the door opens, wait until it's supposed to close.
	    yield return WaitFor.Seconds(maxTimeOpen);
	    if (CustomNetworkManager.IsServer)
	    {
		    if (!BlockAutoClose)
		    {
			    TryClose();
		    }
	    }
    }

    IEnumerator BlockAutomaticClosing()
    {
	    BlockAutoClose = true;
	    yield return WaitFor.Seconds(maxTimeOpen+1);
	    BlockAutoClose = false;
    }

}

}