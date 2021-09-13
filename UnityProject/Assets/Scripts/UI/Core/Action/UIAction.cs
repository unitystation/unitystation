using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Action
{
	public class UIAction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public SpriteDataSO DefaultIconBackground;
		public SpriteHandler IconBackground;
		public SpriteHandler IconFront;
		public Transform CooldownOpacity;
		public Text CooldownNumber;

		public IActionGUI iActionGUI;
		private ActionData actionData;
		private static readonly Vector3 tooltipOffset = new Vector3(-40, -60);
		private ActionTooltip Tooltip => UIActionManager.Instance.TooltipInstance;
		private bool isMine = false;
		public ActionData ActionData => actionData;
		private Vector3 lastClickPosition = default;
		public Vector3 LastClickPosition => lastClickPosition;

		#region Lifecycle

		public void SetUp(IActionGUI action)
		{
			gameObject.SetActive(true);
			iActionGUI = action;

			actionData = iActionGUI.ActionData;
			if (actionData == null)
			{
				Logger.LogWarningFormat("UIAction {0}: action data is null!", Category.UserInput, iActionGUI);
				return;
			}

			if (actionData.Sprites.Count > 0)
			{
				IconFront.SetCatalogue(actionData.Sprites, 0, networked: false);
			}
			if (actionData.Backgrounds.Count > 0)
			{
				IconBackground.SetCatalogue(actionData.Backgrounds, 0, networked: false);
			}
		}

		public void Pool()
		{
			IconBackground.Empty(true, false);
			IconFront.Empty(true, false);
			IconBackground.SetSpriteSO(DefaultIconBackground, networked: false);
			gameObject.SetActive(false);
		}

		private void OnDisable()
		{
			if (isMine)
			{
				Tooltip.gameObject.SetActive(false);
				isMine = false;
			}
		}

		#endregion Lifecycle

		public void ButtonPress()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			if (actionData.IsToggle)
			{
				Toggle();
				return;
			}

			UseAction();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			Tooltip.gameObject.SetActive(true);
			Tooltip.transform.position = transform.position + tooltipOffset;
			Tooltip.ApplyActionData(actionData);
			isMine = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Tooltip.gameObject.SetActive(false);
			isMine = false;
		}

		public void RunActionWithClick(Vector3 clickPosition)
		{
			Toggle(); // Toggle button off after it is used. Could be removed if this is not desired.
			lastClickPosition = clickPosition;
			UseAction();
		}

		private void UseAction()
		{
			// Calling clientside code. If this is a spell,
			// then the spell will call the server to run on the server itself.
			// Not sure why we don't call the spell serverside from here, too...
			// The spell's server request can provide a click position.
			if (iActionGUI.ActionData.CallOnClient)
			{
				iActionGUI.CallActionClient();
			}

			// Sending a message to server asking to run serverside code.
			// TODO: Doesn't support click positions yet.
			// Once it does, consider moving the spell's server request to rely on this here instead, if possible.
			if (iActionGUI.ActionData.CallOnServer == false) return;
			if (iActionGUI is IServerActionGUI)
			{
				if (iActionGUI is UIActionScriptableObject actionSO)
				{
					RequestGameActionSO.Send(actionSO);
				}
				else
				{
					RequestGameAction.Send(iActionGUI as IServerActionGUI);
				}
			}
		}

		private void Toggle()
		{
			if (UIActionManager.Instance.HasActiveAction && UIActionManager.Instance.ActiveAction != this)
			{
				UIActionManager.Instance.ActiveAction.Toggle(); // Toggle off whatever other action was active.
			}

			// The currently active action is this, so toggle it off.
			if (UIActionManager.Instance.HasActiveAction)
			{
				ToggleOff();
			}
			else
			{
				ToggleOn();
			}
		}

		private void ToggleOff()
		{
			IconFront.SetSpriteSO(actionData.Sprites[0], networked: false);
			UIActionManager.Instance.ActiveAction = null;

			if (actionData.HasCustomCursor)
			{
				MouseInputController.ResetCursorTexture();
			}
		}

		private void ToggleOn()
		{
			IconFront.SetSpriteSO(actionData.ActiveSprite, networked: false);
			UIActionManager.Instance.ActiveAction = this;

			TrySetCustomCursor();
		}

		private void TrySetCustomCursor()
		{
			if (actionData.HasCustomCursor == false) return;

			if (actionData.HasCustomCursorOffset)
			{
				MouseInputController.SetCursorTexture(actionData.CursorTexture, actionData.CursorOffset);
			}
			else
			{
				bool isCentered = actionData.OffsetType == CursorOffsetType.Centered;
				MouseInputController.SetCursorTexture(actionData.CursorTexture, isCentered);
			}
		}
	}
}
