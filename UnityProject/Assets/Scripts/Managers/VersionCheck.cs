using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Core
{
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

		public static VersionCheck Instance {
			get {
				if (versionCheck == false)
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
				loginWindow.SetActive(true);
			}
			else
			{
				updateWindow.SetActive(true);
				yourVerText.text = VERSION_NUMBER;
				newVerText.text = get_curVersion.downloadHandler.text;
			}
		}

		public void DownloadButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			Application.OpenURL("http://doobly.izz.moe/unitystation/");
			Application.Quit();
		}

		public void CheckAgain()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			errorWindow.SetActive(false);
			StartCoroutine(CheckVersion());
		}
	}
}
