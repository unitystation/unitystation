using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VersionCheck : MonoBehaviour
{
	private const string VERSION_NUMBER = "0.1.3";
	private const string urlCheck = "http://doobly.izz.moe/unitystation/checkversion.php";
	private static VersionCheck versionCheck;
	public GameObject errorWindow;

	public GameObject loginWindow;
	public Text newVerText;
	public GameObject updateWindow;

	public Text versionText;
	public Text yourVerText;

	public static VersionCheck Instance
	{
		get
		{
			if (!versionCheck)
			{
				versionCheck = FindObjectOfType<VersionCheck>();
			}
			return versionCheck;
		}
	}

	private void Start()
	{
		versionText.text = VERSION_NUMBER;
		//		StartCoroutine(CheckVersion());
	}

	private IEnumerator CheckVersion()
	{
		string url = urlCheck + "?ver=" + VERSION_NUMBER;
		WWW get_curVersion = new WWW(url);
		yield return get_curVersion;

		if (get_curVersion.text == "1")
		{
			//			Debug.Log("Is up to date");
			loginWindow.SetActive(true);
		}
		else if (get_curVersion.text == "")
		{
			errorWindow.SetActive(true);
		}
		else
		{
			//			Debug.Log("Update required to: Version " + get_curVersion.text);
			updateWindow.SetActive(true);
			yourVerText.text = VERSION_NUMBER;
			newVerText.text = get_curVersion.text;
		}
	}

	public void DownloadButton()
	{
		SoundManager.Play("Click01", 1, 1, 0);

		Application.OpenURL("http://doobly.izz.moe/unitystation/");
		Application.Quit();
	}

	public void CheckAgain()
	{
		SoundManager.Play("Click01", 1, 1, 0);
		errorWindow.SetActive(false);
		StartCoroutine(CheckVersion());
	}
}