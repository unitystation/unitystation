using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// Main entry point for handling all input events
/// </summary>
public class MouseInputController : MonoBehaviour
{
	private const float MAX_AGE = 2f;

	/// <summary>
	///     The cooldown before another action can be performed
	/// </summary>
	private float CurrentCooldownTime;

	/// <summary>
	///     The minimum time limit between each action
	/// </summary>
	private float InputCooldownTimer = 0.01f;

	[Tooltip("When mouse button is pressed down and held for longer than this duration, we will" +
	         " not perform a click on mouse up.")]
	public float MaxClickDuration = 1f;
	//tracks how long we've had the button down
	private float clickDuration;

	[Tooltip("Distance to travel from initial click position before a drag (of a MouseDraggable) is initiated.")]
	public float MouseDragDeadzone = 0.2f;
	//tracks the start position (vector which points from the center of currentDraggable) to compare with above
	private Vector2 dragStartOffset;
	//when we click down on a draggable, stores it so we can check if we should click interact or drag interact
	private MouseDraggable potentialDraggable;

	[Tooltip("Seconds to wait before trying to trigger an aim apply while mouse is being held. There is" +
	         " no need to re-trigger aim apply every frame and sometimes those triggers can be expensive, so" +
	         " this can be set to avoid that. It should be set low enough so that every AimApply interaction" +
	         " triggers frequently enough (for example, based on the fastest-firing gun).")]
	public float AimApplyInterval = 0.01f;
	//value used to check against the above while mouse is being held down.
	private float secondsSinceLastAimApplyTrigger;

	private readonly Dictionary<Vector2, Tuple<Color, float>> RecentTouches = new Dictionary<Vector2, Tuple<Color, float>>();
	private readonly List<Vector2> touchesToDitch = new List<Vector2>();
	private LayerMask layerMask;
	private ObjectBehaviour objectBehaviour;
	private PlayerMove playerMove;
	private UserControlledSprites playerSprites;
	/// reference to the global lighting system, used to check occlusion
	private LightingSystem lightingSystem;

	public static readonly Vector3 sz = new Vector3(0.05f, 0.05f, 0.05f);

	private Vector3 MouseWorldPosition => Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);

	/// <summary>
	/// currently triggering aimapply interactable - when mouse is clicked down this is set to the
	/// interactable that was triggered, then it is re-triggered continuously while the button is held,
	/// then set back to null when the button is released.
	/// </summary>
	private IInteractable<AimApply> triggeredAimApply;

	private void OnDrawGizmos()
	{

		if ( touchesToDitch.Count > 0 )
		{
			foreach ( var touch in touchesToDitch )
			{
				RecentTouches.Remove( touch );
			}
			touchesToDitch.Clear();
		}

		if ( RecentTouches.Count == 0 )
		{
			return;
		}

		float time = Time.time;
		foreach (var info in RecentTouches)
		{
			float age = time - info.Value.Item2;
			Color tempColor = info.Value.Item1;
			tempColor.a = Mathf.Clamp(MAX_AGE - age, 0f, 1f);
			Gizmos.color = tempColor;
			Gizmos.DrawCube(info.Key, sz);
			if ( age >= MAX_AGE )
			{
				touchesToDitch.Add( info.Key );
			}
		}

	}

	private void Start()
	{
		//for changing direction on click
		playerSprites = gameObject.GetComponent<UserControlledSprites>();
		playerMove = GetComponent<PlayerMove>();
		objectBehaviour = GetComponent<ObjectBehaviour>();

		lightingSystem = Camera.main.GetComponent<LightingSystem>();

		//Do not include the Default layer! Assign your object to one of the layers below:
		layerMask = LayerMask.GetMask("Furniture", "Walls", "Windows", "Machines", "Players", "Items", "Door Open", "Door Closed", "WallMounts",
			"HiddenWalls", "Objects", "Matrix");
	}

	void LateUpdate()
	{
		CheckMouseInput();
	}

	private void CheckMouseInput()
	{
		if (UIManager.IsMouseInteractionDisabled)
		{
			//still allow tooltips
			CheckHover();
			return;
		}

		//do we have a loaded gun
		var loadedGun = GetLoadedGunInActiveHand();

		if (CommonInput.GetMouseButtonDown(0))
		{
			//check the alt click and throw, which doesn't have any special logic
			if (CheckAltClick()) return;
			if (CheckThrow()) return;

			if (loadedGun != null)
			{
				//if we are on harm intent with loaded gun,
				//don't do anything else, just shoot (trigger the AimApply).
				if (UIManager.CurrentIntent == Intent.Harm)
				{
					CheckAimApply(MouseButtonState.PRESS);
				}
				else
				{
					//proceed to normal click interaction
					CheckClickInteractions(true);
				}
			}
			else
			{
				//we don't have a loaded gun
				//Are we over something draggable?
				var draggable = GetDraggable();
				if (draggable != null)
				{
					//We are over a draggable. We need to wait to see if the user
					//tries to drag the object or lifts the mouse.
					potentialDraggable = draggable;
					dragStartOffset = MouseWorldPosition - potentialDraggable.transform.position;
					clickDuration = 0;
				}
				else
				{
					//no possibility of dragging something, proceed to normal click logic
					CheckClickInteractions(true);
				}

			}
		}
		else if (CommonInput.GetMouseButton(0))
		{
			//mouse button being held down.
			//increment the time since they initially clicked the mouse
			clickDuration += Time.deltaTime;

			//If we are possibly dragging and have exceeded the drag distance, initiate the drag
			if (potentialDraggable != null)
			{
				var currentOffset = MouseWorldPosition - potentialDraggable.transform.position;
				if (((Vector2) currentOffset - dragStartOffset).magnitude > MouseDragDeadzone)
				{
					potentialDraggable.BeginDrag();
					potentialDraggable = null;
				}
			}

			//continue to trigger the aim apply if it was initially triggered
			CheckAimApply(MouseButtonState.HOLD);

		}
		else if (CommonInput.GetMouseButtonUp(0))
		{
			//mouse button is lifted.
			//If we were waiting for mouseup to trigger a click, trigger it if we're still within
			//the duration threshold
			if (potentialDraggable != null)
			{
				if (clickDuration < MaxClickDuration)
				{
					//we are lifting the mouse, so AimApply should not be performed but other
					//clicks can.
					CheckClickInteractions(false);
				}

				clickDuration = 0;
				potentialDraggable = null;
			}

			//no more triggering of the current aim apply
			triggeredAimApply = null;
			secondsSinceLastAimApplyTrigger = 0;
		}
		else
		{
			CheckHover();
		}
	}

	private void CheckClickInteractions(bool includeAimApply)
	{
		if (CheckClickV2()) return;
		if (CheckClick()) return;
		if (includeAimApply) CheckAimApply(MouseButtonState.PRESS);
	}

	//return the Gun component if there is a loaded gun in active hand, otherwise null.
	private Gun GetLoadedGunInActiveHand()
	{
		var item = UIManager.Hands.CurrentSlot.Item;
		if (item != null)
		{
			var gun = item.GetComponent<Gun>();
			if (gun != null && gun.CurrentMagazine != null && gun.CurrentMagazine.ammoRemains > 0)
			{
				return gun;
			}
		}

		return null;
	}

	private Renderer lastHoveredThing;
	private static readonly Type TilemapType = typeof( TilemapRenderer );

	private void CheckHover()
	{
		Renderer hitRenderer;
		if (RayHit(false, out hitRenderer))
		{
			if (!hitRenderer)
			{
				return;
			}

			if (lastHoveredThing != hitRenderer)
			{
				if (lastHoveredThing)
				{
					lastHoveredThing.transform.SendMessageUpwards("OnHoverEnd", SendMessageOptions.DontRequireReceiver);
				}
				hitRenderer.transform.SendMessageUpwards("OnHoverStart", SendMessageOptions.DontRequireReceiver);

				lastHoveredThing = hitRenderer;
			}

			hitRenderer.transform.SendMessageUpwards("OnHover", SendMessageOptions.DontRequireReceiver);
		}
	}

	private bool CheckClick()
	{
		//currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			ChangeDirection();
			return false;
		}

		bool ctrlClick = KeyboardInputManager.IsControlPressed();
		if (!ctrlClick)
		{
			//change the facingDirection of player on click
			ChangeDirection();

			if (RayHitInteract(false))
			{
				return true;
			}

			//if we found nothing at all to click on try to use whats in our hands (might be shooting at someone in space)
			if (!EventSystem.current.IsPointerOverGameObject())
			{
				return InteractHands();
			}

			return false;
		}
		else
		{
			Renderer hitRenderer;
			if (RayHit(false, out hitRenderer))
			{
				if (!hitRenderer)
				{
					return true;
				}
				hitRenderer.transform.SendMessageUpwards("OnCtrlClick", SendMessageOptions.DontRequireReceiver);
				return true;
			}
			//we always return true for a ctrl click in the current system - this will not always be the case
			return true;
		}
	}

	/// <summary>
	/// Checks for a click within the interaction framework v2, which can trigger HandApply as well
	/// as AimApply interactions. Until everything is moved over to V2,
	/// this will have to be used alongside the old one.
	/// </summary>
	private bool CheckClickV2()
	{
		ChangeDirection();
		//currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return false;
		}

		bool ctrlClick = KeyboardInputManager.IsControlPressed();
		if (!ctrlClick)
		{
			var handApplyTargets =
				MouseUtils.GetOrderedObjectsUnderMouse(layerMask);

			//go through the stack of objects and call any interaction components we find
			foreach (GameObject applyTarget in handApplyTargets)
			{
				//check for positionalHandApply first since it is more specific
				PositionalHandApply interaction = PositionalHandApply.ByLocalPlayer(applyTarget.gameObject);
				if (CheckPositionalHandApply(interaction)) return true;
				if (CheckHandApply(interaction)) return true;
			}
		}

		return false;
	}

	private bool CheckPositionalHandApply(PositionalHandApply interaction)
	{
		//call the used object's handapply interaction methods if it has any, for each object we are applying to
        //if handobj is null, then its an empty hand apply so we only need to check the receiving object
        if (interaction.UsedObject != null)
        {
        	foreach (IInteractable<PositionalHandApply> handApply in interaction.UsedObject.GetComponents<IInteractable<PositionalHandApply>>())
        	{
        		var result = handApply.Interact(interaction);
        		if (result.StopProcessing)
        		{
        			//we're done checking, something happened
        			return true;
        		}
        	}
        }

        //call the hand apply interaction methods on the target object if it has any
        foreach (IInteractable<PositionalHandApply> handApply in interaction.TargetObject.GetComponents<IInteractable<PositionalHandApply>>())
        {
        	var result = handApply.Interact(interaction);
        	if (result.StopProcessing)
        	{
        		//something happened, done checking
        		return true;
        	}
        }

        return false;
	}

	private bool CheckHandApply(HandApply interaction)
	{
		//call the used object's handapply interaction methods if it has any, for each object we are applying to
		//if handobj is null, then its an empty hand apply so we only need to check the receiving object
		if (interaction.UsedObject != null)
		{
			foreach (IInteractable<HandApply> handApply in interaction.UsedObject.GetComponents<IInteractable<HandApply>>())
			{
				var result = handApply.Interact(interaction);
				if (result.StopProcessing)
				{
					//we're done checking, something happened
					return true;
				}
			}
		}

		//call the hand apply interaction methods on the target object if it has any
		foreach (IInteractable<HandApply> handApply in interaction.TargetObject.GetComponents<IInteractable<HandApply>>())
		{
			var result = handApply.Interact(interaction);
			if (result.StopProcessing)
			{
				//something happened, done checking
				return true;
			}
		}

		return false;
	}

	private bool CheckAimApply(MouseButtonState buttonState)
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			//don't do aim apply while over UI
			return false;
		}
		ChangeDirection();
		//currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return false;
		}

		//can't do anything if we have no item in hand
		var handObj = UIManager.Hands.CurrentSlot.Item;
		if (handObj == null)
		{
			triggeredAimApply = null;
			secondsSinceLastAimApplyTrigger = 0;
			return false;
		}

		var aimApplyInfo = AimApply.ByLocalPlayer(buttonState);
		if (buttonState == MouseButtonState.PRESS)
		{
			//it's being clicked down
			triggeredAimApply = null;
			//Checks for aim apply interactions which can trigger
			foreach (var aimApply in handObj.GetComponents<IInteractable<AimApply>>())
			{
				var result = aimApply.Interact(aimApplyInfo);
				if (result == InteractionControl.STOP_PROCESSING)
				{
					triggeredAimApply = aimApply;
					secondsSinceLastAimApplyTrigger = 0;
					return true;
				}
			}
		}
		else
		{
			//it's being held
			//if we are already triggering an AimApply, keep triggering it based on the AimApplyInterval
			if (triggeredAimApply != null)
			{
				secondsSinceLastAimApplyTrigger += Time.deltaTime;
				if (secondsSinceLastAimApplyTrigger > AimApplyInterval)
				{
					if (triggeredAimApply.Interact(aimApplyInfo) == InteractionControl.STOP_PROCESSING)
					{
						//only reset timer if it was actually triggered
						secondsSinceLastAimApplyTrigger = 0;
					}
				}

				//no matter what the result, we keep trying to trigger it until mouse is released.
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if there is a MouseDraggable under current mouse position. Returns it if so. Doesn't initiate
	/// the drag.
	/// </summary>
	/// <returns>draggable found, null if none found</returns>
	private MouseDraggable GetDraggable()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			//currently UI is not a part of interaction framework V2
			return null;
		}
		//currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return null;
		}

		var draggable =
			MouseUtils.GetOrderedObjectsUnderMouse(layerMask, go =>
					go.GetComponent<MouseDraggable>() != null &&
					go.GetComponent<MouseDraggable>().CanBeginDrag(PlayerManager.LocalPlayer))
				.FirstOrDefault();
		if (draggable != null)
		{
			var dragComponent = draggable.GetComponent<MouseDraggable>();
			return dragComponent;
		}

		return null;
	}


	private bool CheckAltClick()
	{
		if (KeyboardInputManager.IsAltPressed())
		{
			//Check for items on the clicked position, and display them in the Item List Tab, if they're in reach
			//and not FOV occluded
			Vector3 position = MouseWorldPosition;
			position.z = 0f;
			if (!lightingSystem.enabled || lightingSystem.IsScreenPointVisible(CommonInput.mousePosition))
			{
				if (PlayerManager.LocalPlayerScript.IsInReach(position, false))
				{
					List<GameObject> objects = UITileList.GetItemsAtPosition(position);
					//remove hidden wallmounts
					objects.RemoveAll(obj =>
						obj.GetComponent<WallmountBehavior>() != null &&
						obj.GetComponent<WallmountBehavior>().IsHiddenFromLocalPlayer());
					LayerTile tile = UITileList.GetTileAtPosition(position);
					ControlTabs.ShowItemListTab(objects, tile, position);
				}
			}

			UIManager.SetToolTip = $"clicked position: {Vector3Int.RoundToInt(position)}";
			return true;
		}
		return false;
	}
	private bool CheckThrow()
	{
		//Ignore throw if pointer is hovering over GUI
		if (EventSystem.current.IsPointerOverGameObject())
		{
			return false;
		}

		if (UIManager.IsThrow)
		{
			var currentSlot = UIManager.Hands.CurrentSlot;
			if (!currentSlot.CanPlaceItem())
			{
				return false;
			}
			Vector3 position = MouseWorldPosition;
			position.z = 0f;
			UIManager.CheckStorageHandlerOnMove(currentSlot.Item);
			currentSlot.Clear();
			//				Logger.Log( $"Requesting throw from {currentSlot.eventName} to {position}" );
			PlayerManager.LocalPlayerScript.playerNetworkActions
				.CmdRequestThrow(currentSlot.eventName, position, (int)UIManager.DamageZone);

			//Disabling throw button
			UIManager.Action.Throw();
			return true;
		}
		return false;
	}

	private void ChangeDirection()
	{
		Vector3 playerPos;

		playerPos = transform.position;

		Vector2 dir = (MouseWorldPosition - playerPos).normalized;

		if (!EventSystem.current.IsPointerOverGameObject() && playerMove.allowInput && !playerMove.IsRestrained)
		{
			playerSprites.ChangeAndSyncPlayerDirection(Orientation.From(dir));
		}
	}

	/// <summary>
	/// Checks the gameobjects the mouse is over and triggers interaction for the highest object that has an interaction
	/// to perform.
	/// </summary>
	/// <param name="isDrag">is this during (but not at the very start of) a drag?</param>
	/// <returns>true iff there was a hit that caused an interaction</returns>
	private bool RayHitInteract(bool isDrag)
	{
		Renderer hitRenderer;
		return RayHit(isDrag, out hitRenderer, true);
	}

	/// <summary>
	/// Checks the gameobjects the mouse is over and triggers interaction for the highest object that has an interaction
	/// to perform.
	/// </summary>
	/// <param name="isDrag">is this during (but not at the very start of) a drag?</param>
	/// <param name="hitRenderer">renderer of the gameobject that had an interaction</param>
	/// <param name="interact">true iff there was an interaction that occurred</param>
	/// <returns>true iff there was a hit that caused an interaction</returns>
	private bool RayHit(bool isDrag, out Renderer hitRenderer, bool interact = false)
	{
		hitRenderer = null;

		Vector3 mousePosition = MouseWorldPosition;

		// Sample the FOV mask under current mouse position.
		if (lightingSystem.enabled && lightingSystem.IsScreenPointVisible(CommonInput.mousePosition) == false)
		{
			return false;
		}

		RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, Vector2.zero, 10f, layerMask);

		//collect all the sprite renderers
		List<Renderer> renderers = new List<Renderer>();

		foreach (RaycastHit2D hit in hits)
		{
			Transform objectTransform = hit.collider.gameObject.transform;
			Renderer _renderer = IsHit(objectTransform);
			if (_renderer != null)
			{
				renderers.Add(_renderer);
			}
		}
		bool isInteracting = false;
		//check which of the sprite renderers we hit and pixel checked is the highest
		if (renderers.Count > 0)
		{
			foreach ( Renderer _renderer in renderers.OrderByDescending(r => r.GetType() == TilemapType ? 0 : 1)
													 .ThenByDescending(r => SortingLayer.GetLayerValueFromID(r.sortingLayerID))
													 .ThenByDescending(r => r.sortingOrder))
			{
				// If the ray hits a FOVTile, we can continue down (don't count it as an interaction)
				// Matrix is the base Tilemap layer. It is used for matrix detection but gets in the way
				// of player interaction
				if (!_renderer.sortingLayerName.Equals("FieldOfView"))
				{
					hitRenderer = _renderer;

					if (!interact)
					{
						break;
					}

					if (Interact(_renderer.transform, mousePosition))
					{
						isInteracting = true;
						break;
					}
				}
			}
		}

		//Do interacts below: (This is because if a ray returns true but there is no interaction, check click
		//will not continue with the Interact call so we have to make sure it does below):
		if (interact && !isInteracting)
		{
			//returning false then calls InteractHands from check click:
			return false;
		}

		//check if we found nothing at all
		return hits.Any();
	}

	private Renderer IsHit(Transform _transform)
	{
		TilemapRenderer tilemapRenderer = _transform.GetComponent<TilemapRenderer>();

		if (tilemapRenderer)
		{
			return tilemapRenderer;
		}

		return MouseUtils.IsPixelHit(_transform);
	}

	/// <summary>
	/// Check for and process (if the interaction should occur) an interaction on the gameobject with the given transform.
	/// </summary>
	/// <param name="_transform">transform to check the interaction of</param>
	/// <param name="isDrag">is this during (but not the very start of) a drag?</param>
	/// <returns>true iff an interaction occurred</returns>
	public bool Interact(Transform _transform, bool isDrag)
	{
		return Interact(_transform, _transform.position);
	}


	/// <summary>
	/// Checks for the various interactions that can occur and delegates to the appropriate trigger classes.
	/// Note that only one interaction is allowed to occur in this method - the first time any trigger returns true
	/// (indicating that interaction logic has occurred), the method returns.
	/// </summary>
	/// <param name="_transform">transform to check the interaction of</param>
	/// <param name="position">position the interaction is taking place</param>
	/// <param name="isDrag">is this during (but not at the very start of) a drag?</param>
	/// <returns>true iff an interaction occurred</returns>
	public bool Interact(Transform _transform, Vector3 position)
	{
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return false;
		}

		//check the actual transform for an input trigger and if there is none, check the parent
		InputTrigger inputTrigger = _transform.GetComponentInParent<InputTrigger>();

		//attempt to trigger the things in range we clicked on
		var localPlayer = PlayerManager.LocalPlayerScript;
		if (localPlayer.IsInReach(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition), false) || localPlayer.IsHidden)
		{
			//Check for melee triggers first. If a melee interaction occurs, stop checking for any further interactions
			MeleeTrigger meleeTrigger = _transform.GetComponentInParent<MeleeTrigger>();
			//no melee action happens due to a drag
			if (meleeTrigger != null)
			{
				if (meleeTrigger.MeleeInteract(gameObject, UIManager.Hands.CurrentSlot.eventName))
				{
					return true;
				}
			}

			if (inputTrigger)
			{
				if (objectBehaviour.visibleState)
				{
					bool interacted = TryInputTrigger( position, inputTrigger );
					if (interacted)
					{
						return true;
					}

					//FIXME currently input controller only uses the first InputTrigger found on an object
					/////// some objects have more then 1 input trigger, like players for example
					/////// below is a solution that should be removed when InputController is refactored
					/////// to support multiple InputTriggers on the target object
					if (inputTrigger.gameObject.layer == 8)
					{
						//This is a player. Attempt to use the player based inputTrigger
						P2PInteractions playerInteractions = inputTrigger.gameObject.GetComponent<P2PInteractions>();
						if (playerInteractions != null)
						{
							interacted = playerInteractions.Trigger(position);
						}
					}
					if (interacted)
					{
						return true;
					}
				}
				//Allow interact with cupboards we are inside of!
				ClosetControl closet = inputTrigger.GetComponent<ClosetControl>();
				//no closet interaction happens when dragging
				if (closet && Camera2DFollow.followControl.target == closet.transform)
				{

					if (inputTrigger.Trigger(position))
					{
						return true;
					}
				}
			}

			return false;
		}
		//Still try triggering inputTrigger even if it's outside mouse reach
		//(for things registered on tile within range but having parts outside of it)
		else if ( inputTrigger && objectBehaviour.visibleState && TryInputTrigger( position, inputTrigger ) )
		{
			return true;
		}

		//if we are holding onto an item like a gun attempt to shoot it if we were not in range to trigger anything
		return InteractHands();
	}

	/// <summary>
	/// Tries to trigger InputTrigger
	/// </summary>
	private static bool TryInputTrigger( Vector3 position, InputTrigger inputTrigger )
	{
		return inputTrigger.Trigger( position );
	}

	private bool InteractHands()
	{
		if (UIManager.Hands.CurrentSlot.Item != null && objectBehaviour.visibleState)
		{
			InputTrigger inputTrigger = UIManager.Hands.CurrentSlot.Item.GetComponent<InputTrigger>();

			if (inputTrigger != null)
			{
				bool interacted = false;
				var interactPosition = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
				interacted = inputTrigger.Trigger(interactPosition);
				if (interacted)
				{
					return true;
				}
			}
		}

		return false;
	}

	public void OnMouseDownDir(Vector2 dir)
	{
		if (!playerMove.IsRestrained)
			playerSprites.ChangeAndSyncPlayerDirection(Orientation.From(dir));
	}
}