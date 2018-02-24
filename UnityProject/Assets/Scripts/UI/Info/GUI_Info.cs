using UI;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Info : MonoBehaviour
{
	public GameObject buttonPrefab;

	private static readonly Color banColor = new Color(255f / 255f, 103f / 255f, 103f / 255f);
	private static readonly Color infoColor = new Color(200f / 255f, 200f / 255f, 200f / 255f);
	public Text infoText;
	public Text title;

	public void BtnOk()
	{
		SoundManager.Play("Click01");
		UIManager.Display.infoWindow.SetActive(false);
	}

	public void EndEditOnEnter()
	{
		if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
		{
			BtnOk();
		}
	}

//	public void Show(string info, string titleText = "")
//	{
//		Show(info, infoColor, titleText);
//	}

	public void Show(string info, bool bwoink, string titleText = "")
	{
		infoText.text = info;
		infoText.color = bwoink ? banColor : infoColor;
		title.text = string.IsNullOrEmpty(titleText) ? "Info" : titleText;
		UIManager.Display.infoWindow.SetActive(true);
		SoundManager.Play("Bwoink", 1, 1);
	}

}