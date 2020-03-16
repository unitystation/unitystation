using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GUI class to show a simple, multipurpose text field dialog
/// </summary>
public class GUI_TextInputDialog : MonoBehaviour
{
	[SerializeField]
	private InputField textField = null;
	[SerializeField]
	private ContentSizeFitter contentSizeFitter = null;
	[SerializeField]
	private Text labelText = null;

	private Action<string> callback;

	/// <summary>
	/// Shows a dialog box.
	/// </summary>
	/// <param name="description">Used for the label above the input field</param>
	/// <param name="callback">Callback method which takes the user submitted string input as an argument</param>
	public void ShowDialog(string description, Action<string> callback)
	{
		if (this.gameObject.activeInHierarchy) //Don't show a new dialog if there is still one active
			return;

		textField.text = "";
		this.gameObject.SetActive(true);
		labelText.text = description;
		this.callback = callback;
	}
	
	public void CloseDialog()
	{
		this.gameObject.SetActive(false);
	}

	public void OnEditStart()
	{
		textField.interactable = true;
		textField.ActivateInputField();

		UIManager.IsInputFocus = true;
		UIManager.PreventChatInput = true;
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
		callback(textField.text);
		//UIManager.IsInputFocus = false;
		UIManager.PreventChatInput = false;
		CloseDialog();
	}

	public void OnTextValueChange()
	{
		//Only way to refresh it to get it to do its job (unity bug):
		contentSizeFitter.enabled = false;
		contentSizeFitter.enabled = true;
	}
}