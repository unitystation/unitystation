using System;
using System.Collections.Generic;
using TMPro;
using UI.Core.Action;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Actions.V2.UI
{
	public class UIActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public ActionButtonData ActionData;
		public Image Background;
		[FormerlySerializedAs("AnimatedIconHandler")] public SpriteHandler iconHandler;

		[SerializeField] private Image cooldownOverlay;
		[SerializeField] private TMP_Text cooldownText;

		private bool isActivated = false;
		private Color defaultColor;

		private List<ActionManager> ControlledActionManagers = new List<ActionManager>();
		private ActionTooltip Tooltip => UIActionManager.Instance.TooltipInstance;
		private static readonly Vector3 tooltipOffset = new Vector3(-40, -60);

		public void Setup(ActionButtonData buttonData, bool isMindAction)
		{
			ActionData = buttonData;
			name = ActionData.ID;
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
			if (ActionData.AnimatedIconCatalogue == null || ActionData.AnimatedIconCatalogue.Count == 0) return;
			iconHandler.SetSpriteSO(ActionData.AnimatedIconCatalogue[0]);
		}

		private void TrySetCustomCursor()
		{
			if (ActionData.HasCustomCursor == false) return;
			if (ActionData.HasCustomCursorOffset)
			{
				MouseInputController.SetCursorTexture(ActionData.CursorTexture, ActionData.CursorOffset);
			}
			else
			{
				bool isCentered = ActionData.OffsetType == CursorOffsetType.Centered;
				MouseInputController.SetCursorTexture(ActionData.CursorTexture, isCentered);
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