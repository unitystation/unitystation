using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System;
using US13.Core.Sprite_Handler;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Player;

namespace US13.Actions.V2.UI
{
	public class UIActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public ActionButtonData ActionData;
		public Image Background;
		[FormerlySerializedAs("AnimatedIconHandler")] public SpriteHandler iconHandler;

		public NetworkIdentity Owner { get; private set; }

		[SerializeField] private Image cooldownOverlay;
		[SerializeField] private TMP_Text cooldownText;

		private bool isActivated = false;
		private Color defaultColor;

		private List<ActionManager> ControlledActionManagers = new List<ActionManager>();
		private ActionTooltip Tooltip => UIActionManager.Instance.TooltipInstance;
		private static readonly Vector3 tooltipOffset = new Vector3(-40, -60);

		public void Setup(ActionButtonData buttonData, bool isMindAction, NetworkIdentity owner)
		{
			ActionData = buttonData;
			name = ActionData.ID;
			Owner = owner;
			if (ActionData.CooldownTime >= 0.085f) UpdateManager.Add(UpdateCooldown, 1.25f);
			if (isMindAction)
			{
				Background.color = Color.cyan;
			}
			defaultColor = Background.color;
			RefreshManagers();
			TrySetIcon();
		}

		private void RefreshManagers()
		{
			ControlledActionManagers.Clear();
			if (PlayerManager.LocalPlayerScript.PlayerButtonedActions) ControlledActionManagers.Add(PlayerManager.LocalPlayerScript.PlayerButtonedActions);
			if (PlayerManager.LocalPlayerScript.PlayerButtonedMindActions) ControlledActionManagers.Add(PlayerManager.LocalPlayerScript.PlayerButtonedMindActions);
		}

		private void OnDestroy()
		{
			if (isActivated)
			{
				MouseInputController.ResetCursorTexture();
				UpdateManager.Remove(CallbackType.UPDATE, HandleActivatedActionTrigger);
			}
			if (ActionData.CooldownTime >= 0.085f) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateCooldown);
		}

		public void OnButtonClicked()
		{
			RefreshManagers();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (ActionData.CanUseWhileGhosting == false && PlayerManager.LocalMindScript.isGhosting)
			{
				Chat.AddExamineMsg(PlayerManager.LocalPlayerObject, "Cannot use action while you're a ghost.");
				return;
			}

			if (ActionData.Type == ActionType.Activated)
			{
				HandleActivatedAction();
				return;
			}

			TriggerAction();
			UpdateCooldown();
			UpdateCooldown();
		}

		private void HandleActivatedAction()
		{
			isActivated = !isActivated;
			Background.color = isActivated ? Color.red : defaultColor;

			if (isActivated && ActionData.HasCustomCursor)
			{
				TrySetCustomCursor();
				UpdateManager.Add(CallbackType.UPDATE, HandleActivatedActionTrigger);
			}
			else
			{
				UpdateManager.Remove(CallbackType.UPDATE, HandleActivatedActionTrigger);
				MouseInputController.ResetCursorTexture();
			}
		}

		private void HandleActivatedActionTrigger()
		{
			if (Input.GetMouseButtonDown(0) == false) return;
			TriggerAction();
			HandleActivatedAction(); // Deactivate after use
			UpdateCooldown();
		}

		private void TriggerAction()
		{
			switch (ActionData.TriggerType)
			{
				case ActionTriggerType.ServerOnly:
					foreach (var owned in ControlledActionManagers)
					{
						owned.CmdTriggerAction(ActionData.ID, MouseUtils.MouseToWorldPos());
					}

					break;
				case ActionTriggerType.ClientOnly:
					foreach (var owned in ControlledActionManagers)
					{
						owned.TriggerClientAction(ActionData.ID, MouseUtils.MouseToWorldPos());
					}

					break;
				case ActionTriggerType.Both:
				default:
					foreach (var owned in ControlledActionManagers)
					{
						owned.CmdTriggerAction(ActionData.ID, MouseUtils.MouseToWorldPos());
						owned.TriggerClientAction(ActionData.ID, MouseUtils.MouseToWorldPos());
					}
					break;
			}
		}

		private void TrySetIcon()
		{
			if (TryGetIconFromTrackingObject()) return;
			if (ActionData.AnimatedIconCatalogue == null || ActionData.AnimatedIconCatalogue.Count == 0) return;
			iconHandler.SetSpriteSO(ActionData.AnimatedIconCatalogue[0]);
		}

		private bool TryGetIconFromTrackingObject()
		{
			if (ActionData.TryToGrabSpritesFromRelatedObject == false) return false;
			if (ActionData.ObjectRelatedToThisAction == null) return false;
			var handlers = ActionData.ObjectRelatedToThisAction.GetComponentsInChildren<SpriteHandler>();
			if (handlers == null || handlers.Length == 0) return false;
			SpriteDataSO sprite = null;
			foreach (var handler in handlers)
			{
				var currentSpriteSo = handler.GetCurrentSpriteSO();
				if (currentSpriteSo == null) continue;
				if (currentSpriteSo.Variance.Count == 0) continue;
				sprite = currentSpriteSo;
				break;
			}
			if (sprite == null) return false;
			iconHandler.SetSpriteSO(sprite);
			return true;
		}

		private void TrySetCustomCursor()
		{
			if (ActionData.HasCustomCursor == false || ActionData.CursorTexture?.Variance[0] == null) return;
			if (ActionData.HasCustomCursorOffset)
			{
				MouseInputController.SetCursorTexture(ActionData.CursorTexture.Variance[0].Frames[0].sprite.texture, ActionData.CursorOffset);
			}
			else
			{
				bool isCentered = ActionData.OffsetType == CursorOffsetType.Centered;
				MouseInputController.SetCursorTexture(ActionData.CursorTexture.Variance[0].Frames[0].sprite.texture, isCentered);
			}
		}

		private void UpdateCooldown()
		{
			if (ActionData == null || ActionData.CooldownTime <= 0.085f) return;

			var remainingTime = 0f;
			foreach (var remain in ControlledActionManagers)
			{
				var timeFound = remain.GetRemainingCooldown(ActionData.ID);
				if (timeFound > remainingTime)
				{
					remainingTime = timeFound;
				}
			}

			if (remainingTime > 0)
			{
				cooldownOverlay.gameObject.SetActive(true);
				float fillAmount = remainingTime / ActionData.CooldownTime;
				if (cooldownOverlay) cooldownOverlay.fillAmount = fillAmount;
				cooldownText.text = remainingTime.ToString("F1");
			}
			else
			{
				if (cooldownOverlay) cooldownOverlay.gameObject.SetActive(false);
				cooldownText.text = string.Empty;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			Tooltip.gameObject.SetActive(true);
			Tooltip.transform.position = transform.position + tooltipOffset;
			Tooltip.ApplyActionData(ActionData);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Tooltip.gameObject.SetActive(false);
		}
	}
}