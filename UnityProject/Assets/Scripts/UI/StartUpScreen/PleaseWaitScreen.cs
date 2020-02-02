using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class PleaseWaitScreen : MonoBehaviour
{
	[SerializeField] private Image spinningImage;
	[SerializeField] private List<Sprite> possibleSprites;
	[SerializeField] private bool isStartScreen;
	[SerializeField] private Vector3 rotationSpeed;

	private void OnEnable()
	{
		if (isStartScreen)
		{
			StartCoroutine(WaitForLoad());
		}
		SetRandomSpinImage();
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

	void SetRandomSpinImage()
	{
		var randomIndex = Random.Range(0, possibleSprites.Count);
		spinningImage.sprite = possibleSprites[randomIndex];
	}

	void Update()
	{
		spinningImage.transform.Rotate(rotationSpeed * Time.deltaTime);
	}
}
