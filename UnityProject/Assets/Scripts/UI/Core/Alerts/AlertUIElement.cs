using System.Collections;
using System.Collections.Generic;
using Learning;
using Learning.ProtipObjectTypes;
using UnityEngine;

public class AlertUIElement : MonoBehaviour
{
	public AlertSO AlertSO;
	public SpriteHandler SpriteHandler;

	//Hidden by
	public List<AlertUIElement> HiddenByAction;
	public bool StateChangeThisUpdate = false;

	public UI_HoverTooltip UI_HoverTooltip;

	public void Initialise()
	{
		// Subscribe to the OnActionAdded and OnActionRemoved events of the UIActionManager
		ClientAlertManager.Instance.OnActionShown += HandleActionShown;
		ClientAlertManager.Instance.OnActionHidden += HandleActionHidden;
		// Check if this UIAction should be hidden at start
		SpriteHandler.SetSpriteSO(AlertSO.AssociatedSprite);
		UI_HoverTooltip.hoverName = AlertSO.HoverToolTip;
		CheckIfHidden();
	}

	private void OnDestroy()
	{
		// Unsubscribe from the OnActionAdded and OnActionRemoved events when this UIAction is destroyed
		ClientAlertManager.Instance.OnActionShown -= HandleActionShown;
		ClientAlertManager.Instance.OnActionHidden -= HandleActionHidden;
	}

	private void HandleActionShown(AlertUIElement addedAction)
	{
		// Check if the added action is in the hiddenByActions list
		if (AlertSO.DoNotShowIfPresent.Contains(addedAction.AlertSO))
		{
			// If it is, hide this UIAction
			HiddenByAction.Add(addedAction);
			Hide();
		}
	}

	private void HandleActionHidden(AlertUIElement removedAction)
	{
		// Check if the removed action is in the hiddenActions list
		if (HiddenByAction.Contains(removedAction))
		{
			// If it is, remove it from the list and show this UIAction
			HiddenByAction.Remove(removedAction);
			if (HiddenByAction.Count == 0)
			{
				Show();
			}
		}
	}

	private void CheckIfHidden()
	{
		bool hide = false;
		// Check if any of the hiddenByActions are currently active in the UI
		foreach (var addedAction in ClientAlertManager.Instance.RegisteredAlerts)
		{
			// Check if the added action is in the hiddenByActions list
			if (AlertSO.DoNotShowIfPresent.Contains(addedAction.AlertSO))
			{
				// If it is, hide this UIAction
				HiddenByAction.Add(addedAction);
				hide = true;
				Hide();
			}
		}

		if (hide == false)
		{
			Show();
		}
	}

	private void Show()
	{
		if (StateChangeThisUpdate)
		{
			Logger.LogError($"state changed too many times in one frame, potential infinite loop with {AlertSO.name} settings");
		}
		StateChangeThisUpdate = true;
		ClientAlertManager.Instance.HidingAction(this);


		if (AlertSO.PlayerProtip != null)
		{
			TriggerTip(AlertSO.PlayerProtip);
		}

		// Set this UIAction to be active and visible
		gameObject.SetActive(true);
	}

	private void Hide()
	{
		if (StateChangeThisUpdate)
		{
			Logger.LogError($"state changed too many times in one frame, potential infinite loop with {AlertSO.name} settings");
		}
		StateChangeThisUpdate = true;

		ClientAlertManager.Instance.ShowingAction(this);

		// Set this UIAction to be inactive and invisible
		gameObject.SetActive(false);
	}

	protected void TriggerTip(ProtipSO protipSo)
	{
		PlayerManager.LocalPlayerObject.GetComponentInChildren<ProtipObjectOnHealthStateChange>()
			?.StandardTrigger(protipSo);
	}
}