using System;
using UnityEngine;
using UnityEngine.UI;

namespace Actions.V2.UI
{
	public class UIActionButton : MonoBehaviour
	{
		public ActionButtonData ActionData;
		public Image Icon;
		public SpriteHandler AnimatedIconHandler;

		public void Setup(ActionButtonData buttonData)
		{
			ActionData = buttonData;
			name = ActionData.ID;
			TrySetCustomCursor();
			TrySetIcon();
		}

		public void OnButtonClicked()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			switch (ActionData.TriggerType)
			{
				case ActionTriggerType.ServerOnly:
					PlayerManager.LocalPlayerScript.PlayerButtonedActions.CmdTriggerAction(ActionData.ID);
					break;
				case ActionTriggerType.ClientOnly:
					PlayerManager.LocalPlayerScript.PlayerButtonedActions.TriggerClientAction(ActionData.ID);
					break;
				case ActionTriggerType.Both:
					PlayerManager.LocalPlayerScript.PlayerButtonedActions.CmdTriggerAction(ActionData.ID);
					PlayerManager.LocalPlayerScript.PlayerButtonedActions.TriggerClientAction(ActionData.ID);
					break;
				default:
					PlayerManager.LocalPlayerScript.PlayerButtonedActions.CmdTriggerAction(ActionData.ID);
					break;
			}
		}

		private void TrySetIcon()
		{
			if (ActionData.AnimatedIcon != null)
			{
				AnimatedIconHandler.SetSpriteSO(ActionData.AnimatedIcon);
				return;
			}
			if (ActionData.Icon != null)
			{
				Icon.sprite = ActionData.Icon;
			}
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
	}
}