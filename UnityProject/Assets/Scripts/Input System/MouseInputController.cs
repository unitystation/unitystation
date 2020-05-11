using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Main entry point for handling all input events
/// </summary>
public class MouseInputController : MonoBehaviour
{
	private const float MAX_AGE = 2f;

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
	private PlayerMove playerMove;
	private Directional playerDirectional;
	/// reference to the global lighting system, used to check occlusion
	private LightingSystem lightingSystem;

	public static readonly Vector3 sz = new Vector3(0.05f, 0.05f, 0.05f);

	private Vector3 MouseWorldPosition => Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);

	/// <summary>
	/// currently triggering aimapply interactable - when mouse is clicked down this is set to the
	/// interactable that was triggered, then it is re-triggered continuously while the button is held,
	/// then set back to null when the button is released.
	/// </summary>
	private IBaseInteractable<AimApply> triggeredAimApply;

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
		playerDirectional = gameObject.GetComponent<Directional>();
		playerMove = GetComponent<PlayerMove>();
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}

	void LateUpdate()
	{
		CheckMouseInput();
	}

	private void CheckMouseInput()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			//don't do any game world interactions if we are over the UI
			return;
		}

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

			//check ctrl+click for dragging
			if (KeyboardInputManager.IsControlPressed())
			{
				//even if we didn't drag anything, nothing else should happen
				CheckInitiatePull();
				return;
			}

			if  (KeyboardInputManager.IsShiftPressed())
			{
				//like above, send shift-click request, then do nothing else.
				CheckShiftClick();
				return;
			}

            //check alt click and throw, which doesn't have any special logic. For alt clicks, continue normally.
            CheckAltClick();
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
				if (!(UIManager.CurrentIntent == Intent.Harm) && !(UIManager.CurrentIntent == Intent.Disarm))
				{
					var currentOffset = MouseWorldPosition - potentialDraggable.transform.position;
					if (((Vector2)currentOffset - dragStartOffset).magnitude > MouseDragDeadzone)
					{
						potentialDraggable.BeginDrag();
						potentialDraggable = null;
					}
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

	private void CheckInitiatePull()
	{
		//checks if there is anything in reach we can drag
		var topObject = MouseUtils.GetOrderedObjectsUnderMouse(null,
			go => go.GetComponent<PushPull>() != null).FirstOrDefault();

		if (topObject != null)
		{
			PushPull pushPull = null;

			// If the topObject has a PlayerMove, we check if he is buckled
			// The PushPull object we want in this case, is the chair/object on which he is buckled to
			if (topObject.TryGetComponent<PlayerMove>(out var playerMove) && playerMove.IsBuckled)
			{
				pushPull = playerMove.BuckledObject.GetComponent<PushPull>();
			}
			else
			{
				pushPull = topObject.GetComponent<PushPull>();
			}

			if (pushPull != null)
			{
				pushPull.TryPullThis();
			}
		}
	}

	private void CheckClickInteractions(bool includeAimApply)
	{
		if (CheckClick()) return;
		if (includeAimApply) CheckAimApply(MouseButtonState.PRESS);
	}

	//return the Gun component if there is a loaded gun in active hand, otherwise null.
	private Gun GetLoadedGunInActiveHand()
	{
		if (UIManager.Instance == null || UIManager.Hands == null || UIManager.Hands.CurrentSlot == null) return null;
		var item = UIManager.Hands.CurrentSlot.Item;
		if (item != null)
		{
			var gun = item.GetComponent<Gun>();
			if (gun != null && gun.CurrentMagazine != null && gun.CurrentMagazine.ClientAmmoRemains > 0)
			{
				return gun;
			}
		}

		return null;
	}

	private GameObject lastHoveredThing;

	private void CheckHover()
	{
		//can only hover on things within FOV
		if (lightingSystem.enabled && !lightingSystem.IsScreenPointVisible(CommonInput.mousePosition))
		{
			if (lastHoveredThing)
			{
				lastHoveredThing.transform.SendMessageUpwards("OnHoverEnd", SendMessageOptions.DontRequireReceiver);
			}

			lastHoveredThing = null;
			return;
		}

		var hit = MouseUtils.GetOrderedObjectsUnderMouse().FirstOrDefault();
		if (hit != null)
		{
			if (lastHoveredThing != hit)
			{
				if (lastHoveredThing)
				{
					lastHoveredThing.transform.SendMessageUpwards("OnHoverEnd", SendMessageOptions.DontRequireReceiver);
				}
				hit.transform.SendMessageUpwards("OnHoverStart", SendMessageOptions.DontRequireReceiver);

				lastHoveredThing = hit;
			}

			hit.transform.SendMessageUpwards("OnHover", SendMessageOptions.DontRequireReceiver);
		}
	}

	private bool CheckClick()
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
				MouseUtils.GetOrderedObjectsUnderMouse();

			//go through the stack of objects and call any interaction components we find
			foreach (GameObject applyTarget in handApplyTargets)
			{
				if (CheckHandApply(applyTarget)) return true;
			}
			//check empty space positional hand apply
			var posHandApply = PositionalHandApply.ByLocalPlayer(null);
			if (posHandApply.HandObject != null)
			{
				var handAppliables = posHandApply.HandObject.GetComponents<IBaseInteractable<PositionalHandApply>>()
					.Where(c => c != null && (c as MonoBehaviour).enabled);
				if (InteractionUtils.ClientCheckAndTrigger(handAppliables, posHandApply) != null) return true;
			}
		}

		return false;
	}

	private bool CheckHandApply(GameObject target)
	{
		//call the used object's handapply interaction methods if it has any, for each object we are applying to
		var handApply = HandApply.ByLocalPlayer(target);
		var posHandApply = PositionalHandApply.ByLocalPlayer(target);

		//if handobj is null, then its an empty hand apply so we only need to check the receiving object
		if (handApply.HandObject != null)
		{
			//get all components that can handapply or PositionalHandApply
			var handAppliables = handApply.HandObject.GetComponents<MonoBehaviour>()
				.Where(c => c != null && c.enabled && (c is IBaseInteractable<HandApply> || c is IBaseInteractable<PositionalHandApply>));
			Logger.LogTraceFormat("Checking HandApply / PositionalHandApply interactions from {0} targeting {1}",
				Category.Interaction, handApply.HandObject.name, target.name);

			foreach (var handAppliable in handAppliables.Reverse())
			{
				if (handAppliable is IBaseInteractable<HandApply>)
				{
					var hap = handAppliable as IBaseInteractable<HandApply>;
					if (hap.ClientCheckAndTrigger(handApply)) return true;
				}
				else
				{
					var hap = handAppliable as IBaseInteractable<PositionalHandApply>;
					if (hap.ClientCheckAndTrigger(posHandApply)) return true;
				}
			}
		}

		//call the hand apply interaction methods on the target object if it has any
		var targetHandAppliables = handApply.TargetObject.GetComponents<MonoBehaviour>()
			.Where(c => c != null && c.enabled && (c is IBaseInteractable<HandApply> || c is IBaseInteractable<PositionalHandApply>));
		foreach (var targetHandAppliable in targetHandAppliables.Reverse())
		{
			if (targetHandAppliable is IBaseInteractable<HandApply>)
			{
				var hap = targetHandAppliable as IBaseInteractable<HandApply>;
				if (hap.ClientCheckAndTrigger(handApply)) return true;
			}
			else
			{
				var hap = targetHandAppliable as IBaseInteractable<PositionalHandApply>;
				if (hap.ClientCheckAndTrigger(posHandApply)) return true;
			}
		}

		return false;
	}

	private bool CheckAimApply(MouseButtonState buttonState)
	{
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
			var comps = handObj.GetComponents<IBaseInteractable<AimApply>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			var triggered = InteractionUtils.ClientCheckAndTrigger(comps, aimApplyInfo);
			if (triggered != null)
			{
				triggeredAimApply = triggered;
				secondsSinceLastAimApplyTrigger = 0;
				return true;
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
					if (triggeredAimApply.ClientCheckAndTrigger(aimApplyInfo))
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
		//currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return null;
		}

		var draggable =
			MouseUtils.GetOrderedObjectsUnderMouse(null, go =>
					go.GetComponent<MouseDraggable>() != null &&
					go.GetComponent<MouseDraggable>().enabled &&
					go.GetComponent<MouseDraggable>().CanBeginDrag(PlayerManager.LocalPlayer))
				.FirstOrDefault();
		if (draggable != null)
		{
			var dragComponent = draggable.GetComponent<MouseDraggable>();
			return dragComponent;
		}

		return null;
	}

	/// <summary>
	/// Fires if shift is pressed on click, initiates examine. Assumes inanimate object, but upgrades to checking health if living, and id if target has
	/// storage and an ID card in-slot.
	/// </summary>
	private void CheckShiftClick()
	{
		// Get clickedObject from mousepos
		var clickedObject = MouseUtils.GetOrderedObjectsUnderMouse(null, null).FirstOrDefault();

		// TODO Prepare and send requestexaminemessage
		// todo:  check if netid = 0.
		RequestExamineMessage.Send(clickedObject.GetComponent<NetworkIdentity>().netId, MouseWorldPosition);
	}

	private bool CheckAltClick()
	{
		if (KeyboardInputManager.IsAltPressed())
		{
			//Check for items on the clicked position, and display them in the Item List Tab, if they're in reach
			//and not FOV occluded
			Vector3Int position = MouseWorldPosition.CutToInt();
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
			if (CustomNetworkManager.IsServer)
			{
				MatrixManager.ForMatrixAt(position, true, (matrix, localPos) =>
				{
					matrix.SubsystemManager.UpdateAt(localPos);
					Logger.LogFormat($"Forcefully updated atmos at worldPos {position}/ localPos {localPos} of {matrix.Name}");
				});

				Chat.AddLocalMsgToChat("Ping "+DateTime.Now.ToFileTimeUtc(), (Vector3) position, PlayerManager.LocalPlayer );
			}
			return true;
		}
		return false;
	}

	private bool CheckThrow()
	{
		if (UIManager.IsThrow)
		{
			var currentSlot = UIManager.Hands.CurrentSlot;
			if (currentSlot.Item == null)
			{
				return false;
			}
			Vector3 targetPosition = MouseWorldPosition;
			targetPosition.z = 0f;

			//using transform position instead of registered position
			//so target is still correct when lerping on a matrix (since registered world position is rounded)
			Vector3 targetVector = targetPosition - PlayerManager.LocalPlayer.transform.position;

			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdThrow(currentSlot.NamedSlot,
				targetVector, (int)UIManager.DamageZone);

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

		if (!EventSystem.current.IsPointerOverGameObject() && playerMove.allowInput && !playerMove.IsBuckled)
		{
			playerDirectional.FaceDirection(Orientation.From(dir));
		}
	}
}