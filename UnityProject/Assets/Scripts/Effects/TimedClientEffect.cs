using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedClientEffect : MonoBehaviour
{
	[SerializeField]
	[Min(0)]
	private float time = 1f;

	private void OnEnable()
	{
		StartCoroutine(EffectTimer());
	}

	private IEnumerator EffectTimer()
	{
		float totalTime = 0;

		while (totalTime < time)
		{
			totalTime += Time.deltaTime;
			yield return WaitFor.EndOfFrame;
		}

		Destroy(this.gameObject);
	}
}
