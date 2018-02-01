using UI;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Info : MonoBehaviour
{
	public GameObject buttonPrefab;

	public static Color banColor = new Color(255,103,103);
	public static Color infoColor = new Color(200,200,200);
	public Text infoText;
	public Text title;

	public void BtnOk()
	{
		SoundManager.Play("Click01");
		UIManager.Instance.GetComponent<ControlDisplays>().infoWindow.SetActive(false);
	}

	public void EndEditOnEnter()
	{
		if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
		{
			BtnOk();
		}
	}

	public void Show(string info, string titleText = "")
	{
		Show(info, infoColor, titleText);
	}

	public void Show(string info, Color infoColor, string titleText = "")
	{
//		UIManager.Instance.GetComponent<ControlDisplays>().jobSelectWindow.SetActive()
		
	}

}