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

	//used so that drag interaction cannot be started until the mouse button is up after a click interaction
	private bool canDrag = true;

	private readonly Dictionary<Vector2, Tuple<Color, float>> RecentTouches = new Dictionary<Vector2, Tuple<Color, float>>();
	private readonly List<Vector2> touchesToDitch = new List<Vector2>();
	private LayerMask layerMask;
	private ObjectBehaviour objectBehaviour;
	private PlayerMove playerMove;
	private UserControlledSprites playerSprites;
	/// reference to the global lighting system, used to check occlusion
	private LightingSystem lightingSystem;

	public static readonly Vector3 sz = new Vector3(0.05f, 0.05f, 0.05f);

	private Vector3 MousePosition => Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);

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

		if (!canDrag && CommonInput.GetMouseButtonUp(0))
		{
			//reset candrag on buttonup
			canDrag = true;
		}
		if (CommonInput.GetMouseButtonDown(0))
		{
			var clicked = CheckAltClick();
			if (!clicked)
			{
				clicked = CheckThrow();
			}
			if (!clicked)
			{
				clicked = CheckClick();
			}
			if (!clicked)
			{
				clicked = CheckClickV2();
			}

			if (clicked)
			{
				//wait until mouseup to allow drag interaction again
				canDrag = false;
			}
		}
		else if (CommonInput.GetMouseButton(0))
		{
			//mouse being held down / dragged
			if (!CheckDrag() && canDrag)
			{
				CheckDragV2();
			}
		}
		else
		{
			CheckHover();
		}
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

	//note - bool is now returned to indicate the CheckClickV2 should be skipped if an interacton occurs in
	//this version of the method.
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
				return InteractHands(false);
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
	/// Checks for a click within the interaction framework v2. Until everything is moved over to V2,
	/// this will have to be used alongside the old one.
	/// </summary>
	private bool CheckClickV2()
	{
		//currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return false;
		}

		bool ctrlClick = KeyboardInputManager.IsControlPressed();
		if (!ctrlClick)
		{
			var handApplyTargets =
				MouseUtils.GetOrderedObjectsUnderMouse(layerMask, go => go.GetComponent<RegisterTile>() != null)
					//get the root gameobject of the dropped-on sprite renderer
					.Select(sr => sr.GetComponentInParent<RegisterTile>().gameObject)
					//only want distinct game objects even if we hit multiple renderers on one object.
					.Distinct();
			//object in hand
			var handObj = UIManager.Hands.CurrentSlot.Item;

			//go through the stack of objects and call any drop components we find
			foreach (GameObject applyTarget in handApplyTargets)
			{
				HandApply info = new HandApply(PlayerManager.LocalPlayer, handObj, applyTarget.gameObject);
				//call the used object's handapply interaction methods if it has any, for each object we are applying to
				//if handobj is null, then its an empty hand apply so we only need to check the receiving object
				if (handObj != null)
				{
					foreach (IInteractable<HandApply> handApply in handObj.GetComponents<IInteractable<HandApply>>())
					{
						var result = handApply.Interact(info);
						if (result.SomethingHappened)
						{
							//we're done checking, something happened
							return true;
						}
					}
				}

				//call the hand apply interaction methods on the target object if it has any
				foreach (IInteractable<HandApply> handApply in applyTarget.GetComponents<IInteractable<HandApply>>())
				{
					var result = handApply.Interact(info);
					if (result.SomethingHappened)
					{
						//something happened, done checking
						return true;
					}
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Handles events that should happen while mouse is being held down (but not when it is initially clicked down)
	/// </summary>
	private bool CheckDrag()
	{
		//if we found nothing at all to click on try to use whats in our hands (might be shooting at someone in space)
		var hit = RayHitInteract(true);
		if (hit)
		{
			return true;
		}
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			return InteractHands(true);
		}

		return false;
	}

	private void CheckDragV2()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			//currently UI is not a part of interaction framework V2
			return;
		}
		//currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return;
		}

		var draggable =
			MouseUtils.GetOrderedObjectsUnderMouse(layerMask, go =>
					go.GetComponent<MouseDraggable>() != null &&
					go.GetComponent<MouseDraggable>().CanBeginDrag(PlayerManager.LocalPlayer))
				//get the root gameobject of the draggable
				.Select(sr => sr.GetComponentInParent<MouseDraggable>().gameObject)
				//only want distinct game objects even if we hit multiple renderers on one object.
				.Distinct()
				.FirstOrDefault();
		if (draggable != null)
		{
			//start dragging the first draggable we found
			draggable.GetComponent<MouseDraggable>().BeginDrag();
		}
	}


	private bool CheckAltClick()
	{
		if (KeyboardInputManager.IsAltPressed())
		{
			//Check for items on the clicked position, and display them in the Item List Tab, if they're in reach
			//and not FOV occluded
			Vector3 position = MousePosition;
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
			Vector3 position = MousePosition;
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

		Vector2 dir = (MousePosition - playerPos).normalized;

		if (!EventSystem.current.IsPointerOverGameObject() && playerMove.allowInput)
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

		Vector3 mousePosition = MousePosition;

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

					if (Interact(_renderer.transform, mousePosition, isDrag))
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
		return Interact(_transform, _transform.position, isDrag);
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
	public bool Interact(Transform _transform, Vector3 position, bool isDrag)
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
			if (meleeTrigger != null && !isDrag)
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
					bool interacted = TryInputTrigger( position, isDrag, inputTrigger );
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
							if (isDrag)
							{
								interacted = playerInteractions.TriggerDrag(position);
							}
							else
							{
								interacted = playerInteractions.Trigger(position);
							}
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
				if (closet && Camera2DFollow.followControl.target == closet.transform && !isDrag)
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
		else if ( inputTrigger && objectBehaviour.visibleState && TryInputTrigger( position, isDrag, inputTrigger ) )
		{
			return true;
		}

		//if we are holding onto an item like a gun attempt to shoot it if we were not in range to trigger anything
		return InteractHands(isDrag);
	}

	/// <summary>
	/// Tries to trigger InputTrigger
	/// </summary>
	private static bool TryInputTrigger( Vector3 position, bool isDrag, InputTrigger inputTrigger )
	{
		return isDrag ? inputTrigger.TriggerDrag( position ) : inputTrigger.Trigger( position );
	}

	private bool InteractHands(bool isDrag)
	{
		if (UIManager.Hands.CurrentSlot.Item != null && objectBehaviour.visibleState)
		{
			InputTrigger inputTrigger = UIManager.Hands.CurrentSlot.Item.GetComponent<InputTrigger>();

			if (inputTrigger != null)
			{
				bool interacted = false;
				var interactPosition = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
				if (isDrag)
				{
					interacted = inputTrigger.TriggerDrag(interactPosition);
				}
				else
				{
					interacted = inputTrigger.Trigger(interactPosition);
				}
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
		playerSprites.ChangeAndSyncPlayerDirection(Orientation.From(dir));
	}
}