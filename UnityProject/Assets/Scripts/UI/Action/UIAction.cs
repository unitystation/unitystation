using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIAction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public SpriteDataSO DefaultIconBackground;
	public SpriteHandler IconBackground;
	public SpriteHandler IconFront;

	public IActionGUI iActionGUI;
	private ActionData actionData;
	private static readonly Vector3 tooltipOffset = new Vector3(-40, -60);
	private ActionTooltip tooltip => UIActionManager.Instance.TooltipInstance;
	private bool isMine = false;

	public void SetUp(IActionGUI action)
	{
		this.gameObject.SetActive(true);
		iActionGUI = action;

		actionData = iActionGUI.ActionData;
		if (actionData == null)
		{
			Logger.LogWarningFormat("UIAction {0}: action data is null!", Category.UIAction, iActionGUI );
			return;
		}

		IconFront.SetCatalogue(actionData.Sprites,0, NetWork: false);
		if (actionData.Backgrounds.Count > 0) {
			IconBackground.SetCatalogue(actionData.Backgrounds,0 ,NetWork: false);
		}
	}

	public void Pool()
	{
		IconBackground.ChangeSpriteVariant(0, false);
		IconFront.ChangeSpriteVariant(0, false);
		IconBackground.SetSpriteSO(DefaultIconBackground, Network : false);
		IconFront.PushClear(false);
		this.gameObject.SetActive(false);
	}

	public void ButtonPress()
	{
		SoundManager.Play("Click01");
		//calling clientside code
		if (iActionGUI.ActionData.CallOnClient)
		{
			iActionGUI.CallActionClient();
		}

		//sending a message to server asking to run serverside code
		if (iActionGUI.ActionData.CallOnServer) {
			if (iActionGUI is IServerActionGUI) {
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
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		tooltip.gameObject.SetActive(true);
		tooltip.transform.position = transform.position + tooltipOffset;
		tooltip.ApplyActionData(actionData);
		isMine = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		tooltip.gameObject.SetActive(false);
		isMine = false;
	}

	private void OnDisable()
	{
		if (isMine)
		{
			tooltip.gameObject.SetActive(false);
			isMine = false;
		}
	}
}
