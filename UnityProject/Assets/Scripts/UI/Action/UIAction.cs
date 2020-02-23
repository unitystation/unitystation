using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAction : MonoBehaviour
{
	public SpriteSheetAndData DefaultIconBackground;
	public SpriteHandler IconBackground;
	public SpriteHandler IconFront;

	public IActionGUI iActionGUI;

	public void SetUp(IActionGUI _iActionGUI)
	{
		this.gameObject.SetActive(true);
		iActionGUI = _iActionGUI;
		IconFront.SetInfo(_iActionGUI.ActionData.Sprites);
		if (_iActionGUI.ActionData.Backgrounds.Count > 0) {
			IconBackground.SetInfo(_iActionGUI.ActionData.Backgrounds);
		}
	}

	public void Pool()
	{
		IconBackground.ChangeSpriteVariant(0);
		IconFront.ChangeSpriteVariant(0);
		IconBackground.SetInfo(DefaultIconBackground.Data);
		IconFront.PushClear();
		this.gameObject.SetActive(false);
	}

	public void ButtonPress()
	{
		SoundManager.Play("Click01");
		if (iActionGUI.ActionData.CallOnClient)
		{
			iActionGUI.CallActionClient();
		}

		if (iActionGUI.ActionData.CallOnServer) {
			if (iActionGUI is IServerActionGUI) {
				if (iActionGUI is UIActionScriptableObject)
				{
					var iIActionScriptableObject = iActionGUI as UIActionScriptableObject;
					RequestGameActionSO.Send(iIActionScriptableObject);
				}
				else
				{ 
					RequestGameAction.Send(iActionGUI as IServerActionGUI);
				}
			}
		}
	}
}
