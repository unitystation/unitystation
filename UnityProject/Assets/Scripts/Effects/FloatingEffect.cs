using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : LTEffect
{
	public bool willAnimate;
	private float speed = 1.1f;
	private float pos = 0.1f;

	public void startFloating()
	{
		willAnimate = true;
		tween.isAnim = true;
		StartCoroutine(animate());
	}

	public void stopFloating()
	{
		willAnimate = false;
		tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), 0.01f);
		tween.isAnim = false;
		StopAllCoroutines();
	}

	private IEnumerator animate()
	{
		while(willAnimate == true)
		{
			tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, pos, 0), speed);
			yield return new WaitForSeconds(speed);
			tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, -pos, 0), speed);
			yield return new WaitForSeconds(speed);
			tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), speed / 2);
			yield return new WaitForSeconds(speed / 2);
		}
	}
}
