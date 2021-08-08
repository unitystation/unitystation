using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Systems.Interaction;


namespace Systems.Ai
{
	/// <summary>
	/// Main entry point for handling all Ai input events
	/// </summary>
	public class AiMouseInputController : MouseInputController, IPlayerControllable
	{
		private AiPlayer aiPlayer;

		private bool moveCoolDown;

		public override void Start()
		{
			aiPlayer = GetComponent<AiPlayer>();
			base.Start();
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

			if (CommonInput.GetMouseButtonDown(0))
			{

				if (KeyboardInputManager.IsControlPressed() && KeyboardInputManager.IsShiftPressed())
				{
					CheckForInteractions(AiActivate.ClickTypes.CtrlShiftClick);
					return;
				}

				//check ctrl+click for dragging
				if (KeyboardInputManager.IsControlPressed())
				{
					CheckForInteractions(AiActivate.ClickTypes.CtrlClick);
					return;
				}

				if (KeyboardInputManager.IsShiftPressed())
				{
					//like above, send shift-click request, then do nothing else.
					//Inspect();
					CheckForInteractions(AiActivate.ClickTypes.ShiftClick);
					return;
				}

				if (KeyboardInputManager.IsAltPressed())
				{
					CheckForInteractions(AiActivate.ClickTypes.AltClick);
					return;
				}

				CheckForInteractions(AiActivate.ClickTypes.NormalClick);
			}
			else
			{
				CheckHover();
			}
		}

		private void CheckForInteractions(AiActivate.ClickTypes clickType)
		{
			var handApplyTargets = MouseUtils.GetOrderedObjectsUnderMouse();

			//go through the stack of objects and call AiActivate interaction components we find
			foreach (GameObject applyTarget in handApplyTargets)
			{
				var behaviours = applyTarget.GetComponents<IBaseInteractable<AiActivate>>()
					.Where(mb => mb != null && (mb as MonoBehaviour).enabled);

				var aiActivate = new AiActivate(gameObject, null, applyTarget, Intent.Help, clickType);
				InteractionUtils.ClientCheckAndTrigger(behaviours, aiActivate);
			}
		}

		public void ReceivePlayerMoveAction(PlayerAction moveActions)
		{
			if(moveActions.moveActions.Length == 0) return;

			if(UIManager.IsInputFocus) return;

			if (moveCoolDown) return;
			moveCoolDown = true;

			StartCoroutine(CoolDown());

			aiPlayer.MoveCameraByKey(PlayerAction.GetMoveAction(moveActions.Direction()));
		}

		private IEnumerator CoolDown()
		{
			yield return WaitFor.Seconds(.3f);
			moveCoolDown = false;
		}
	}
}
