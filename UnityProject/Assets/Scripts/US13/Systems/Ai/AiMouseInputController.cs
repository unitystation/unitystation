using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using US13.Core.Input_System;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Player;
using US13.UI.Systems;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace US13.Systems.Ai
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

				//check ctrl+click interactions
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

				if (KeyboardInputManager.IsAltActionKeyPressed())
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

				var aiActivate = new AiActivate(gameObject, null, applyTarget, Intent.Help,aiPlayer.PlayerScript.Mind , clickType);
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

			aiPlayer.MoveCameraByKey(moveActions.Direction());
		}

		private IEnumerator CoolDown()
		{
			yield return WaitFor.Seconds(.05f);
			moveCoolDown = false;
		}
	}
}
