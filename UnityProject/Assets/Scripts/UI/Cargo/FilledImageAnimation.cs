using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FilledImageAnimation : MonoBehaviour
{
	[SerializeField]
	private Image imageToFade = null;
	[SerializeField]
	private AnimationCurve fadeCurve = null;
	[SerializeField]
	private float fadeDuration = .3f;
	[SerializeField]
	private float waitDuration = .15f;
	private bool fading = false;

	public void Fade()
	{
		if (fading)
		{
			return;
		}
		StartCoroutine(FadeCoroutine());
	}

	//For an example of usage check CargoSupplyItem prefab
	private IEnumerator FadeCoroutine()
	{
		float t = 0;

		fading = true;
		while (t < fadeDuration)
		{
			t += Time.deltaTime;
			imageToFade.fillAmount = fadeCurve.Evaluate(t / fadeDuration);
			yield return null;
		}
		yield return new WaitForSeconds(waitDuration);
		while (t > 0)
		{
			t -= Time.deltaTime;
			imageToFade.fillAmount = fadeCurve.Evaluate(t / fadeDuration);
			yield return null;
		}
		fading = false;
	}

	private void OnEnable()
	{
		imageToFade.fillAmount = 0;
		fading = false;
	}
}
