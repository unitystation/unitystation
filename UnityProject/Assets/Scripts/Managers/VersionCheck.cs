using System.Collections;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Initialisation;
using SecureStuff;
using Shared.Util;
using UnityEngine;
using UnityEngine.UI;
using Util;

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

		public static VersionCheck Instance => FindUtils.LazyFindObject(ref versionCheck);

		private void Start()
		{
			versionText.text = VERSION_NUMBER;
			//		StartCoroutine(CheckVersion());
		}

		private async Task CheckVersion()
		{
			string url = urlCheck + "?ver=" + VERSION_NUMBER;



			HttpRequestMessage r = new HttpRequestMessage(HttpMethod.Get,
				url);


			var response = await SafeHttpRequest.SendAsync(r);


			var stringy = await response.Content.ReadAsStringAsync();

			LoadManager.DoInMainThread(() =>
			{
				if (response.IsSuccessStatusCode == false || stringy == "")
				{
					errorWindow.SetActive(true);
				}
				else if (stringy == "1")
				{
					loginWindow.SetActive(true);
				}
				else
				{
					updateWindow.SetActive(true);
					yourVerText.text = VERSION_NUMBER;
					newVerText.text = stringy;
				}
			});
		}

		public void DownloadButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			SafeURL.Open("http://doobly.izz.moe/unitystation/");
			Application.Quit();
		}

		public void CheckAgain()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			errorWindow.SetActive(false);
			CheckVersion();
		}
	}
}
