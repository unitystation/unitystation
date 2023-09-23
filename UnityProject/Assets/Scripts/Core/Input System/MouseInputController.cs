using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Messages.Client.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using Weapons;
using Objects.Wallmounts;
using Player.Movement;
using Tilemaps.Behaviours.Layers;
using UI;
using UI.Action;
using Tiles;
using UI.Core.Action;

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
	protected float clickDuration;

	[Tooltip("Distance to travel from initial click position before a drag (of a MouseDraggable) is initiated.")]
	public float MouseDragDeadzone = 0.2f;

	//tracks the start position (vector which points from the center of currentDraggable) to compare with above
	protected Vector2 dragStartOffset;

	//when we click down on a draggable, stores it so we can check if we should click interact or drag interact
	protected MouseDraggable potentialDraggable;

	[Tooltip("Seconds to wait before trying to trigger an aim apply while mouse is being held. There is" +
	         " no need to re-trigger aim apply every frame and sometimes those triggers can be expensive, so" +
	         " this can be set to avoid that. It should be set low enough so that every AimApply interaction" +
	         " triggers frequently enough (for example, based on the fastest-firing gun).")]
	public float AimApplyInterval = 0.01f;

	//value used to check against the above while mouse is being held down.
	protected float secondsSinceLastAimApplyTrigger;

	private readonly Dictionary<Vector2, Tuple<Color, float>> RecentTouches =
		new Dictionary<Vector2, Tuple<Color, float>>();

	private readonly List<Vector2> touchesToDitch = new List<Vector2>();
	private MovementSynchronisation playerMove;
	private Rotatable playerDirectional;

	/// reference to the global lighting system, used to check occlusion
	private LightingSystem lightingSystem;

	public static readonly Vector3 sz = new Vector3(0.05f, 0.05f, 0.05f);

	public static Vector3 MouseWorldPosition => Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);

	/// <summary>
	/// currently triggering aimapply interactable - when mouse is clicked down this is set to the
	/// interactable that was triggered, then it is re-triggered continuously while the button is held,
	/// then set back to null when the button is released.
	/// </summary>
	protected IBaseInteractable<AimApply> triggeredAimApply;

	private void OnDrawGizmos()
	{
		if (touchesToDitch.Count > 0)
		{
			foreach (var touch in touchesToDitch)
			{
				RecentTouches.Remove(touch);
			}

			touchesToDitch.Clear();
		}

		if (RecentTouches.Count == 0)
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
			if (age >= MAX_AGE)
			{
				touchesToDitch.Add(info.Key);
			}
		}
	}

	public virtual void Start()
	{
		//for changing direction on click
		playerDirectional = gameObject.GetComponent<Rotatable>();
		playerMove = GetComponent<MovementSynchronisation>();
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}

	private void LateUpdate()
	{
		if (PlayerManager.LocalPlayerObject != this.gameObject) return;

		if (ControlAction.ThrowHold && UIManager.IsInputFocus == false)
		{
			if (UIManager.IsThrow == false)
			{
				if (KeyboardInputManager.Instance.CheckKeyAction(
					    KeyAction.ActionThrow,
					    KeyboardInputManager.KeyEventType.Down))
				{
					UIManager.Instance.actionControl.Throw();
				}
			}
			else
			{
				if (KeyboardInputManager.Instance.CheckKeyAction(
					    KeyAction.ActionThrow,
					    KeyboardInputManager.KeyEventType.Up))
				{
					UIManager.Instance.actionControl.Throw();
				}
			}

		}


		CheckMouseInput();
		CheckCursorTexture();
	}

	public virtual void CheckMouseInput()
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

			if (KeyboardInputManager.IsShiftPressed())
			{
				//like above, send shift-click request, then do nothing else.
				Inspect();
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
					if (((Vector2) currentOffset - dragStartOffset).magnitude > MouseDragDeadzone)
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

	protected void CheckInitiatePull()
	{
		//checks if there is anything in reach we can drag
		var topObject = MouseUtils.GetOrderedObjectsUnderMouse(null,
			go => go.GetComponent<UniversalObjectPhysics>() != null).FirstOrDefault();

		if (topObject != null)
		{
			UniversalObjectPhysics pushPull = null;

			// If the topObject has a PlayerMove, we check if he is buckled
			// The PushPull object we want in this case, is the chair/object on which he is buckled to
			if (topObject.TryGetComponent<MovementSynchronisation>(out var playerMove) &&
			    playerMove.BuckledToObject != null)
			{
				pushPull = playerMove.BuckledToObject.GetComponent<UniversalObjectPhysics>();
			}
			else
			{
				pushPull = topObject.GetComponent<UniversalObjectPhysics>();
			}

			if (pushPull != null)
			{
				pushPull.TryTogglePull();
			}
		}
	}

	public void CheckClickInteractions(bool includeAimApply)
	{
		if (CheckClick()) return;
		if (includeAimApply) CheckAimApply(MouseButtonState.PRESS);
	}

	// return the Gun component if there is a loaded gun in active hand, otherwise null.
	private Gun GetLoadedGunInActiveHand()
	{
		if (PlayerManager.LocalPlayerScript?.DynamicItemStorage?.GetActiveHandSlot() == null) return null;
		var item = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().Item;
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

	public void CheckHover()
	{
		//can only hover on things within FOV
		if (lightingSystem.enabled && !lightingSystem.IsScreenPointVisible(CommonInput.mousePosition))
		{
			if (lastHoveredThing)
			{
				lastHoveredThing.transform.SendMessageUpwards("OnHoverEnd", SendMessageOptions.DontRequireReceiver);
				transform.SendMessage("OnHoverEnd", SendMessageOptions.DontRequireReceiver);
			}

			lastHoveredThing = null;
			return;
		}

		var hit = MouseUtils.GetOrderedObjectsUnderMouse()?.FirstOrDefault();
		if (hit != null)
		{
			if (lastHoveredThing != hit)
			{
				if (lastHoveredThing)
				{
					lastHoveredThing.transform.SendMessageUpwards("OnHoverEnd", SendMessageOptions.DontRequireReceiver);
				}

				hit.transform.SendMessageUpwards("OnHoverStart", SendMessageOptions.DontRequireReceiver);
				transform.SendMessage("OnHoverStart", SendMessageOptions.DontRequireReceiver);
				lastHoveredThing = hit;
			}

			hit.transform.SendMessageUpwards("OnHover", SendMessageOptions.DontRequireReceiver);
			transform.SendMessage("OnHover", SendMessageOptions.DontRequireReceiver);
		}
		else if (lastHoveredThing)
		{
			lastHoveredThing.transform.SendMessageUpwards("OnHoverEnd", SendMessageOptions.DontRequireReceiver);
			lastHoveredThing = null;
		}
	}

	private void TrySlide()
	{
		if (PlayerManager.LocalPlayerScript.IsNormal == false ||
		    PlayerManager.LocalPlayerScript.playerHealth.ConsciousState != ConsciousState.CONSCIOUS)
			return;
		PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdSlideItem(Vector3Int.RoundToInt(MouseWorldPosition));
	}

	public bool CheckClick()
	{
		ChangeDirection();
		// currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return false;
		}

		bool ctrlClick = KeyboardInputManager.IsControlPressed();
		if (!ctrlClick)
		{
			if (UIActionManager.Instance.IsAiming)
			{
				UIActionManager.Instance.AimClicked(MouseWorldPosition);
				return true;
			}

			var handApplyTargets =
				MouseUtils.GetOrderedObjectsUnderMouse();

			// go through the stack of objects and call any interaction components we find
			foreach (GameObject applyTarget in handApplyTargets)
			{
				if (CheckHandApply(applyTarget)) return true;
			}

			// check empty space positional hand apply
			var mousePos = MouseUtils.MouseToWorldPos().RoundToInt();
			var posHandApply =
				PositionalHandApply.ByLocalPlayer(MatrixManager.AtPoint(mousePos, false).GameObject.transform.parent
					.gameObject);
			if (posHandApply.HandObject != null)
			{
				var handAppliables = posHandApply.HandObject.GetComponents<IBaseInteractable<PositionalHandApply>>()
					.Where(c => c != null && (c as MonoBehaviour).enabled);
				if (InteractionUtils.ClientCheckAndTrigger(handAppliables, posHandApply) != null) return true;
			}

			// If we're dragging something, try to move it.
			if (PlayerManager.LocalPlayerScript.ObjectPhysics.Pulling.HasComponent)
			{
				TrySlide();
				return false;
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
				.Where(c => c != null && c.enabled &&
				            (c is IBaseInteractable<HandApply> || c is IBaseInteractable<PositionalHandApply>));
			Loggy.LogTraceFormat("Checking HandApply / PositionalHandApply interactions from {0} targeting {1}",
				Category.Interaction, handApply.HandObject.name, target.name);

			foreach (var handAppliable in handAppliables.Reverse())
			{
				if (handAppliable is IBaseInteractable<HandApply> hap)
					//Technically PositionalHandApply Inherits from HandApply So it should work But it doesn't For some reason I don't know why, if it breaks Check this probably
				{
					if (hap.ClientCheckAndTrigger(handApply)) return true;
				}

				if (handAppliable is IBaseInteractable<PositionalHandApply> appliable)
				{
					if (appliable.ClientCheckAndTrigger(posHandApply)) return true;
				}
			}
		}

		//call the hand apply interaction methods on the target object if it has any
		var targetHandAppliables = handApply.TargetObject.GetComponents<MonoBehaviour>()
			.Where(c => c != null && c.enabled &&
			            (c is IBaseInteractable<HandApply> || c is IBaseInteractable<PositionalHandApply>));
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

	/// <summary>
	/// Used if you want to Force an interaction, Between Local character and certain Script ( Skips handApply.HandObject  )
	/// </summary>
	/// <param name="RelatedApply"></param>
	/// <param name="Target"></param>
	public static void CheckHandApply(IBaseInteractable<HandApply> targetHandAppliable, GameObject Target)
	{
		//call the used object's handapply interaction methods if it has any, for each object we are applying to
		var handApply = HandApply.ByLocalPlayer(Target);
		targetHandAppliable.ClientCheckAndTrigger(handApply);
	}

	protected bool CheckAimApply(MouseButtonState buttonState)
	{
		ChangeDirection();
		//currently there is nothing for ghosts to interact with, they only can change facing
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return false;
		}

		//can't do anything if we have no item in hand
		var handObj = PlayerManager.LocalPlayerScript.OrNull()?.DynamicItemStorage.OrNull()?.GetActiveHandSlot()?.Item;
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
	protected MouseDraggable GetDraggable()
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
					go.GetComponent<MouseDraggable>().CanBeginDrag(PlayerManager.LocalPlayerScript))
				.FirstOrDefault();
		if (draggable != null)
		{
			var dragComponent = draggable.GetComponent<MouseDraggable>();
			return dragComponent;
		}

		return null;
	}

	public static void Point()
	{
		var clickedObject = MouseUtils.GetOrderedObjectsUnderMouse(null, null).FirstOrDefault();
		if (!clickedObject)
			return;
		if (PlayerManager.LocalPlayerScript.IsGhost ||
		    PlayerManager.LocalPlayerScript.playerHealth.ConsciousState != ConsciousState.CONSCIOUS)
			return;
		if (Cooldowns.TryStartClient(PlayerManager.LocalPlayerScript, CommonCooldowns.Instance.Interaction) == false)
			return;

		if (clickedObject.TryGetComponent<NetworkedMatrix>(out var networkedMatrix))
		{
			clickedObject = networkedMatrix.MatrixSync.gameObject;
		}

		PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdPoint(clickedObject, MouseWorldPosition);
	}

	/// <summary>
	/// Fires if shift is pressed on click, initiates examine. Assumes inanimate object, but upgrades to checking health if living, and id if target has
	/// storage and an ID card in-slot.
	/// </summary>
	public void Inspect()
	{
		// Get clickedObject from mousepos
		var clickedObject = MouseUtils.GetOrderedObjectsUnderMouse(null, null).FirstOrDefault();

		// TODO Prepare and send requestexaminemessage
		// todo:  check if netid = 0.

		//Shift clicking on space created NRE
		if (!clickedObject) return;

		if (clickedObject.TryGetComponent<NetworkedMatrix>(out var networkedMatrix))
		{
			clickedObject = networkedMatrix.MatrixSync.gameObject;
		}

		RequestExamineMessage.Send(clickedObject.GetComponent<NetworkIdentity>().netId, MouseWorldPosition);
	}

	protected bool CheckAltClick()
	{
		if (KeyboardInputManager.IsAltActionKeyPressed())
		{
			//Check for items on the clicked position, and display them in the Item List Tab, if they're in reach
			//and not FOV occluded
			Vector3Int position = MouseWorldPosition.CutToInt();
			if (!lightingSystem.enabled || lightingSystem.IsScreenPointVisible(CommonInput.mousePosition))
			{
				if (PlayerManager.LocalPlayerScript.IsPositionReachable(position, false))
				{
					List<GameObject> objects = UITileList.GetItemsAtPosition(position);
					//remove hidden wallmounts
					objects.RemoveAll(obj =>
						obj.GetComponent<WallmountBehavior>() != null &&
						obj.GetComponent<WallmountBehavior>().IsHiddenFromLocalPlayer() ||
						obj.TryGetComponent(
							out NetworkedMatrix netMatrix)); // Test to see if station (or shuttle) itself.
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
					Loggy.LogFormat(
						$"Forcefully updated atmos at worldPos {position}/ localPos {localPos} of {matrix.Name}");
				});

				Chat.AddActionMsgToChat(PlayerManager.LocalPlayerObject, "Ping " + DateTime.Now.ToFileTimeUtc());
			}

			return true;
		}

		return false;
	}

	protected bool CheckThrow()
	{
		if (UIManager.IsThrow)
		{
			var currentSlot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
			if (currentSlot?.Item != null || PlayerManager.LocalPlayerScript.playerMove.Pulling.HasComponent)
			{
				var localTarget = MouseWorldPosition.ToLocal(playerMove.registerTile.Matrix);
				var vector = MouseWorldPosition - PlayerManager.LocalPlayerScript.transform.position;
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdThrow(localTarget, (int) UIManager.DamageZone,
					vector);

				//Disabling throw button
				UIManager.Action.Throw();
				return true;
			}
		}

		return false;
	}

	protected void ChangeDirection()
	{
		Vector3 playerPos;

		playerPos = transform.position;

		Vector2 dir = (MouseWorldPosition - playerPos).normalized;

		if (playerMove != null)
		{
			if (!EventSystem.current.IsPointerOverGameObject() && playerMove.AllowInput &&
			    playerMove.BuckledToObject == null)
			{
				playerDirectional.OrNull()?.SetFaceDirectionLocalVector(dir.RoundTo2Int());
			}
		}
		else
		{
			if (!EventSystem.current.IsPointerOverGameObject())
			{
				playerDirectional.OrNull()?.SetFaceDirectionLocalVector(dir.RoundTo2Int());
			}
		}
	}

	#region Cursor Textures

	[Header("Examine Cursor Settings")] [SerializeField]
	public MouseIconSo examineCursor;
	public MouseIconSo grabbingCursor;
	public MouseIconSo altInteractionCursor;

	public MouseIconSo HarmCursor;
	public MouseIconSo GrabCursor;
	public MouseIconSo DisarmCursor;

	public MouseIconSo ThrowCursor;

	private bool isShowingKeyComboCursor = false;
	private static Texture2D currentCursorTexture = null;
	private static Vector2 currentCursorOffset = Vector2.zero;

	private Intent previousIntent = Intent.Help;

	/// <summary>
	/// Sets the cursor's texture to the given texture.
	/// </summary>
	/// <param name="texture">The texture to use.</param>
	/// <param name="offset">The offset the texture should have. Used for aligning the texture to the click point.
	/// Relative to the texture size so 512x512 would mean a supplied vector of 128x128
	/// results in the texture's top left quadrant being the hotspot.</param>
	public static void SetCursorTexture(Texture2D texture, Vector2 offset)
	{
		if (currentCursorTexture == texture) return;

		Cursor.SetCursor(texture, offset, CursorMode.Auto);
		currentCursorTexture = texture;
		currentCursorOffset = offset;
	}

	/// <summary>
	/// Sets the cursor's texture to the given texture.
	/// </summary>
	/// <param name="texture">The texture to use.</param>
	/// <param name="centerTexture">If true, centers the texture relative to the click point. Else, top left is the click point.</param>
	public static void SetCursorTexture(Texture2D texture, bool centerTexture = true)
	{
		var hotspot = Vector2.zero;
		if (centerTexture)
		{
			hotspot = new Vector2(texture.height / 2, texture.width / 2);
		}

		SetCursorTexture(texture, hotspot);
	}

	/// <summary>
	/// Sets the cursor back to the system default.
	/// </summary>
	public static void ResetCursorTexture()
	{
		if (currentCursorTexture == null) return;

		Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
		currentCursorTexture = null;
		currentCursorOffset = Vector2.zero;
	}

	private void CheckCursorTexture()
	{
		if (isShowingKeyComboCursor == false && (KeyboardInputManager.IsShiftPressed() ||  KeyboardInputManager.IsControlPressed() ||  KeyboardInputManager.IsAltActionKeyPressed() || UIManager.IsThrow))
		{
			if (UIManager.IsThrow)
			{
				Cursor.SetCursor(ThrowCursor.Texture, ThrowCursor.Offset, CursorMode.Auto);
			}
			else if (KeyboardInputManager.IsControlPressed())
			{
				Cursor.SetCursor(grabbingCursor.Texture, grabbingCursor.Offset, CursorMode.Auto);
			}
			else if (KeyboardInputManager.IsShiftPressed())
			{
				Cursor.SetCursor(examineCursor.Texture, examineCursor.Offset, CursorMode.Auto);
			}
			else
			{
				Cursor.SetCursor(altInteractionCursor.Texture, altInteractionCursor.Offset, CursorMode.Auto);
			}

			isShowingKeyComboCursor = true;
			previousIntent = Intent.Help;
		}
		else if (isShowingKeyComboCursor && KeyboardInputManager.IsShiftPressed() == false && KeyboardInputManager.IsControlPressed() == false && KeyboardInputManager.IsAltActionKeyPressed() == false && UIManager.IsThrow == false)
		{
			Cursor.SetCursor(currentCursorTexture, currentCursorOffset, CursorMode.Auto);
			isShowingKeyComboCursor = false;
			previousIntent = Intent.Help;
		}

		if (currentCursorTexture == null && isShowingKeyComboCursor == false)
		{
			//Go back to intents
			if (UIManager.CurrentIntent != previousIntent )
			{
				switch (UIManager.CurrentIntent)
				{
					case Intent.Harm:
						Cursor.SetCursor(HarmCursor.Texture, HarmCursor.Offset, CursorMode.Auto);
						previousIntent = UIManager.CurrentIntent;
						break;
					case Intent.Disarm:
						Cursor.SetCursor(DisarmCursor.Texture, DisarmCursor.Offset, CursorMode.Auto);
						previousIntent = UIManager.CurrentIntent;
						break;
					case Intent.Grab:
						Cursor.SetCursor(GrabCursor.Texture, GrabCursor.Offset, CursorMode.Auto);
						previousIntent = UIManager.CurrentIntent;
						break;
					case Intent.Help:
						Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
						previousIntent = UIManager.CurrentIntent;
						break;
				}
			}
		}
	}

	#endregion Cursor Textures
}