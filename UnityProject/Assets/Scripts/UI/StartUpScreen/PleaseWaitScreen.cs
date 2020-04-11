using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PleaseWaitScreen : MonoBehaviour
{
	[SerializeField] private bool isStartScreen = false;
	[SerializeField] private LoadingScreen loadingScreen = null;

	private void OnEnable()
	{
		if (isStartScreen)
		{
			StartCoroutine(WaitForLoad());
		}
	}

	IEnumerator WaitForLoad()
	{
		loadingScreen.SetLoadBar(0f);
		yield return WaitFor.EndOfFrame;

		AsyncOperation AO = SceneManager.LoadSceneAsync(1);
		AO.allowSceneActivation = false;
		while(AO.progress < 0.9f)
		{
			loadingScreen.SetLoadBar(AO.progress);
			yield return WaitFor.EndOfFrame;
		}
		loadingScreen.SetLoadBar(1f);
		yield return WaitFor.EndOfFrame;
		AO.allowSceneActivation = true;
	}
}
