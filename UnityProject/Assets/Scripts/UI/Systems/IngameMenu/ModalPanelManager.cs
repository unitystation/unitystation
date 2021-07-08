using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ModalPanelManager : MonoBehaviour {
	public GameObject ModalPanelObject;
	public Text ModalTitle;
	public Button Button1;
	public Button Button2;
	public Button Button3;

	public static ModalPanelManager Instance;
	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	/// <summary>
	/// Brings up a panel with a choice of two buttons.
	/// Automatically closes after a choice is made
	/// </summary>
	/// <param name="modalTitle">Title of the panel</param>
	/// <param name="button1Event">Function to run when button 1 pressed</param>
	/// <param name="button1Text">Text to display on button 1</param>
	/// <param name="button2Event">Function to run when button 2 pressed</param>
	/// <param name="button2Text">Text to display on button 2</param>
	/// <param name="closeEvent">Function to run when panel closes</param>
	public void Confirm (string modalTitle, UnityAction button1Event, string button1Text = "Yes", UnityAction button2Event = null, string button2Text = "Cancel", UnityAction closeEvent = null)
	{
		ModalPanelObject.SetActive(true);

		ModalTitle.text = modalTitle;

		Button1.onClick.RemoveAllListeners();
		Button1.onClick.AddListener(button1Event);
		Button1.onClick.AddListener(ClosePanel);
		Button1.GetComponentInChildren<Text>().text = button1Text;
		Button1.gameObject.SetActive(true);

		// If no function supplied for button2, just close window
		Button2.onClick.RemoveAllListeners();
		if (button2Event != null)
		{
			Button2.onClick.AddListener(button2Event);
		}
		Button2.onClick.AddListener(ClosePanel);
		Button2.GetComponentInChildren<Text>().text = button2Text;
		Button2.gameObject.SetActive(true);

		Button3.gameObject.SetActive(false);
	}

	/// <summary>
	/// Brings up a panel with some information and an ok button
	/// Automatically closes after button is clicked
	/// </summary>
	/// <param name="modalTitle">Title of the panel</param>
	/// <param name="button1Event">Function to run when button 1 pressed</param>
	/// <param name="button1Text">Text to display on button 1</param>
	public void Inform (string modalTitle, UnityAction button1Event = null, string button1Text = "Ok")
	{
		ModalPanelObject.SetActive(true);

		ModalTitle.text = modalTitle;

		Button1.onClick.RemoveAllListeners();

		if (button1Event != null)
		{
			Button1.onClick.AddListener(button1Event);
		}

		Button1.onClick.AddListener(ClosePanel);
		Button1.GetComponentInChildren<Text>().text = button1Text;
		Button1.gameObject.SetActive(true);

		Button2.gameObject.SetActive(false);

		Button3.gameObject.SetActive(false);
	}

	/// <summary>
	/// Brings up a panel with a choice of upto three buttons.
	/// Automatically closes after a choice is made.
	/// </summary>
	public void Choice (string modalTitle, UnityAction button1Event, string button1Text = "Yes", UnityAction button2Event = null, string button2Text = "No", UnityAction button3Event = null, string button3Text = "Cancel")
	{
		ModalPanelObject.SetActive(true);

		ModalTitle.text = modalTitle;

		Button1.onClick.RemoveAllListeners();
		Button1.onClick.AddListener(button1Event);
		Button1.onClick.AddListener(ClosePanel);
		Button1.GetComponentInChildren<Text>().text = button1Text;
		Button1.gameObject.SetActive(true);

		if (button2Event != null)
		{
			Button2.onClick.RemoveAllListeners();
			Button2.onClick.AddListener(button2Event);
			Button2.onClick.AddListener(ClosePanel);
			Button2.GetComponentInChildren<Text>().text = button2Text;
			Button2.gameObject.SetActive(true);
		}
		else
		{
			Button2.gameObject.SetActive(false);
		}

		if (button3Event != null)
		{
			Button3.onClick.RemoveAllListeners();
			Button3.onClick.AddListener(button3Event);
			Button3.onClick.AddListener(ClosePanel);
			Button2.GetComponentInChildren<Text>().text = button3Text;
			Button3.gameObject.SetActive(true);
		}
		else
		{
			Button3.gameObject.SetActive(false);
		}
	}

	void ClosePanel()
	{
		ModalPanelObject.SetActive(false);
	}
}
