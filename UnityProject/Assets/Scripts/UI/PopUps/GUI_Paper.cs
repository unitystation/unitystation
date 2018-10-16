using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Paper : NetTab
{
	public InputField textField;
	public ContentSizeFitter contentSizeFitter;

	public override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(WaitForProvider());
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return YieldHelper.EndOfFrame;
		}
		RefreshText();
	}

	public override void RefreshTab()
	{
		RefreshText();
		base.RefreshTab();
	}

	public void RefreshText()
	{
		if (Provider != null)
		{
			textField.text = Provider.GetComponent<Paper>().PaperString;
		}
	}

	public void ClosePaper()
	{
		ControlTabs.CloseTab(Type, Provider);
	}

	public void OnEditStart()
	{
		UIManager.IsInputFocus = true;
		CheckForInput();
	}

	//Safety measure:
	private async void CheckForInput()
	{
		await Task.Delay(500);
		if (!textField.isFocused)
		{
			UIManager.IsInputFocus = false;
		}
	}

	//Request an edit from server:
	public void OnTextEditEnd()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestPaperEdit(Provider.gameObject, textField.text);
		UIManager.IsInputFocus = false;
	}

	public void OnTextValueChange()
	{
		//Only way to refresh it to get it to do its job (unity bug):
		contentSizeFitter.enabled = false;
		contentSizeFitter.enabled = true;
		CheckLineLimit();
	}

	private void CheckLineLimit()
	{
		Canvas.ForceUpdateCanvases();
		if (textField.textComponent.cachedTextGenerator.lineCount > 20)
		{	
			var sub = textField.text.Substring(0, textField.text.Length - 1);
			textField.text = sub;
		}
	}
}