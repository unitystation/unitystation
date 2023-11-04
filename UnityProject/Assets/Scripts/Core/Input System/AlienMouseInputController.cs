using System;
using Logs;
using Systems.Antagonists;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;
using UnityEngine.EventSystems;

public class AlienMouseInputController : MouseInputController
{
	private AlienPlayer alienPlayer;
	private UI_Alien alienUi;

	private AlienClicks currentClick = AlienClicks.None;
	public AlienClicks CurrentClick => currentClick;

	public enum AlienClicks
	{
		None,
		AcidSpit,
		NeurotoxinSpit
	}

	private void Awake()
	{
		alienPlayer = GetComponent<AlienPlayer>();
	}

	public override void Start()
	{
		base.Start();

		alienUi = UIManager.Instance.panelHudBottomController.AlienUI;
	}

	public void SetClickType(AlienClicks newClick)
	{
		currentClick = newClick;
	}

	public override void CheckMouseInput()
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

		//If right click or middle mouse button then try spit
		if (CommonInput.GetMouseButtonDown(1) || KeyboardInputManager.IsMiddleMouseButtonPressed())
		{
			//Check for spit clicks
			if (DoAlienClicks()) return;
		}

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
		else if (CommonInput.GetMouseButton(0))
		{
			//mouse button being held down.
			//increment the time since they initially clicked the mouse
			clickDuration += Time.deltaTime;

			//If we are possibly dragging and have exceeded the drag distance, initiate the drag
			if (potentialDraggable != null)
			{
				if (UIManager.CurrentIntent != Intent.Harm && UIManager.CurrentIntent != Intent.Disarm)
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

	private bool DoAlienClicks()
	{
		if (currentClick == AlienClicks.None) return false;

		if (alienPlayer.ValidateProjectile() == false) return false;

		var aimApplyInfo = AimApply.ByLocalPlayer(MouseButtonState.PRESS);

		switch (currentClick)
		{
			case AlienClicks.AcidSpit:
				alienPlayer.ClientTryAcidSpit(aimApplyInfo);
				return true;
			case AlienClicks.NeurotoxinSpit:
				alienPlayer.ClientTryNeurotoxinSpit(aimApplyInfo);
				return true;
			default:
				Loggy.LogError($"Unexpected case: {currentClick.ToString()}");
				return false;
		}

		return false;
	}
}