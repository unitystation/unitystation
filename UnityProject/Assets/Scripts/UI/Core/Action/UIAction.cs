using System;
using Logs;
using Systems.Spells;
using UI.Core.Action;
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

		public IAction iAction;
		private ActionData actionData;
		private static readonly Vector3 tooltipOffset = new Vector3(-40, -60);
		private ActionTooltip Tooltip => UIActionManager.Instance.TooltipInstance;
		private bool isMine = false;
		public ActionData ActionData => actionData;
		private Vector3 lastClickPosition = default;
		public Vector3 LastClickPosition => lastClickPosition;

		private void Awake()
		{
			Loggy.LogError($"MADE {GetInstanceID()}");
		}

		#region Lifecycle

		public void SetUp(IActionGUI action)
		{
			gameObject.SetActive(true);
			iAction = action;
			if(iAction is Spell spell)
			{
				Loggy.LogError($"MADE {spell.Ugg()}"); //SEE IF iAction IS THE CLIENT SIDE OR SERVER SIDE INSTANCE, oh this is client side and we reg to this that might be the issue
			}

			actionData = action.ActionData;
			actionData.OwningUIAction = this;
			if (actionData == null)
			{
				Loggy.LogWarningFormat("UIAction {0}: action data is null!", Category.UserInput, iAction);
				return;
			}

			if (actionData.Sprites.Count > 0)
			{
				IconFront.SetCatalogue(actionData.Sprites, 0, networked: false);

				//Turn off raycasting if we have a background so the tooltip will show if mouse over IconFront
				GetComponent<Image>().raycastTarget = actionData.Backgrounds.Count == 0;
			}
			if (actionData.Backgrounds.Count > 0)
			{
				IconBackground.SetCatalogue(actionData.Backgrounds, 0, networked: false);
			}

			if(iAction is UnattachedIActionGUI unattachedIActionGUI)
			{
				unattachedIActionGUI.OnAttachedPlayer();
			}
		}
		//copy pasta funny but im too lazy to do anything about it right now
		public void SetUpMulti(IActionGUIMulti action, ActionData newActionData)
		{
			gameObject.SetActive(true);
			iAction = action;

			actionData = newActionData;
			actionData.OwningUIAction = this;
			if (actionData == null)
			{
				Loggy.LogWarningFormat("UIAction {0}: action data is null!", Category.UserInput, iAction);
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
			if (this == null) return;
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
				Toggle(true);
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
			if (actionData.CallOnClient)
			{
				if (iAction is IActionGUI iActionGUI)
				{
					iActionGUI.CallActionClient();
				}
				else if (iAction is IActionGUIMulti iActionGUIMulti)
				{
					iActionGUIMulti.CallActionClient(actionData);
				}
			}

			// Sending a message to server asking to run serverside code.
			// TODO: Doesn't support click positions yet.
			// Once it does, consider moving the spell's server request to rely on this here instead, if possible.
			if (actionData.CallOnServer == false) return;

			if (iAction is IServerActionGUI)
			{
				if (iAction is UIActionScriptableObject actionSO)
				{
					RequestGameActionSO.Send(actionSO);
				}
				else
				{
					RequestGameAction.Send(iAction as IServerActionGUI);
				}
			}
			else if (iAction is IServerActionGUIMulti)
			{
				if (iAction is UIActionScriptableObject actionSO)
				{
					RequestGameActionSO.Send(actionSO);
				}
				else
				{
					RequestGameAction.Send(iAction as IServerActionGUIMulti, actionData);
				}
			}
		}

		public void Toggle(bool ForceDisable = false)
		{
			if (UIActionManager.Instance.HasActiveAction && UIActionManager.Instance.ActiveAction != this)
			{
				UIActionManager.Instance.ActiveAction.Toggle(true); // Toggle off whatever other action was active.
			}

			// The currently active action is this, so toggle it off.
			if (UIActionManager.Instance.HasActiveAction)
			{
				if(!ForceDisable && ActionData.StaySelectedOnUse) return;
				ToggleOff();
			}
			else
			{
				ToggleOn();
			}
		}

		public event System.Action OnToggleOff;

		public void ToggleOff()
		{
			OnToggleOff?.Invoke();
			Loggy.LogError("THE");
			IconFront.SetSpriteSO(actionData.Sprites[0], networked: false);
			UIActionManager.Instance.ActiveAction = null;

			if (actionData.HasCustomCursor)
			{
				MouseInputController.ResetCursorTexture();
			}
		}

		public void ToggleOn()
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

		private void OnDestroy()
		{
			OnToggleOff = null;
		}
	}
}
