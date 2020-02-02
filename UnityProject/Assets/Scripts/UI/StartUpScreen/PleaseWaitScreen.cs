using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PleaseWaitScreen : MonoBehaviour
{
	[SerializeField] private bool isStartScreen;

	private void OnEnable()
	{
		if (isStartScreen)
		{
			StartCoroutine(WaitForLoad());
		}
	}

	IEnumerator WaitForLoad()
	{
		yield return WaitFor.EndOfFrame;

		AsyncOperation AO = SceneManager.LoadSceneAsync(1);
		AO.allowSceneActivation = false;
		while(AO.progress < 0.9f)
		{
			yield return WaitFor.EndOfFrame;
		}
		yield return WaitFor.EndOfFrame;
		AO.allowSceneActivation = true;
	}
}
