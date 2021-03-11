using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : LTEffect
{
	public bool animateOnStartup = true;

	[HideInInspector]
	public bool willAnimate = false;

	private const float speed = 0.9f;
	private const float pos = 0.08f;


	private void Awake()
	{
		if (animateOnStartup)
		{
			StartFloating();
		}
	}

	public void StartFloating()
	{
		willAnimate = true;
		StartCoroutine(Animate());
	}

	public void StopFloating()
	{
		willAnimate = false;
		tween.RpcLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), 0.01f);
		StopAllCoroutines();
	}

	private IEnumerator Animate()
	{
		while(willAnimate == true)
		{
			tween.RpcLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, pos, 0), speed);
			yield return new WaitForSeconds(speed);
			tween.RpcLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, -pos, 0), speed);
			yield return new WaitForSeconds(speed);
			tween.RpcLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), speed / 2);
			yield return new WaitForSeconds(speed / 2);
		}
	}
}
