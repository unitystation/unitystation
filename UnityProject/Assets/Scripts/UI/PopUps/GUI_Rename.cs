using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Rename : NetTab
{

	public InputField textField;
	public ContentSizeFitter contentSizeFitter;

	private const int MAX_NAME_LENGTH = 42;

	public override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(WaitForProvider());
		textField.interactable = false;
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
	}
	
	public void CloseDialog()
	{
		ControlTabs.CloseTab(Type, Provider);
	}

	public void OnEditStart()
	{
		textField.interactable = true;
		textField.ActivateInputField();

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
		var customName = textField.text;
		if (customName.Length > MAX_NAME_LENGTH)
		{
			customName = customName.Substring(0, MAX_NAME_LENGTH);
		}

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestRename(Provider.gameObject, customName);
		UIManager.IsInputFocus = false;
	}

	public void OnTextValueChange()
	{
		//Only way to refresh it to get it to do its job (unity bug):
		contentSizeFitter.enabled = false;
		contentSizeFitter.enabled = true;
		if (!textField.placeholder.enabled)
		{
			CheckLine();
		}
	}

	private void CheckLine()
	{
		Canvas.ForceUpdateCanvases();
		if (textField.text.Length > MAX_NAME_LENGTH)
		{
			var sub = textField.text.Substring(0, MAX_NAME_LENGTH);
			textField.text = sub;
		}

		textField.text = Regex.Replace(textField.text, @"\r\n?|\n", "");
	}
}