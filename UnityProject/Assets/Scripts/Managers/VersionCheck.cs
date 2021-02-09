using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
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
		var get_curVersion = new UnityWebRequest(url);
		yield return get_curVersion.SendWebRequest();

		if (get_curVersion.isNetworkError | get_curVersion.isHttpError | get_curVersion.downloadHandler.text == "")
		{
			errorWindow.SetActive(true);
		}
		else if (get_curVersion.downloadHandler.text == "1")
		{
			//			Logger.Log("Is up to date");
			loginWindow.SetActive(true);
		}
		else
		{
			//			Logger.Log("Update required to: Version " + get_curVersion.text);
			updateWindow.SetActive(true);
			yourVerText.text = VERSION_NUMBER;
			newVerText.text = get_curVersion.downloadHandler.text;
		}
	}

	public void DownloadButton()
	{
		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		Application.OpenURL("http://doobly.izz.moe/unitystation/");
		Application.Quit();
	}

	public void CheckAgain()
	{
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
		errorWindow.SetActive(false);
		StartCoroutine(CheckVersion());
	}
}