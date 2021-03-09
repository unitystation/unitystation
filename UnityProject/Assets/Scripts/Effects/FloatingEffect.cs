using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : LTEffect
{
	public bool WillAnimate;
	private const float speed = 0.9f;
	private const float pos = 0.08f;

	public void StartFloating()
	{
		WillAnimate = true;
		tween.isAnim = true;
		StartCoroutine(Animate());
	}

	public void StopFloating()
	{
		WillAnimate = false;
		tween.CmdLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), 0.01f);
		tween.isAnim = false;
		StopAllCoroutines();
	}

	private IEnumerator Animate()
	{
		while(WillAnimate == true)
		{
			tween.CmdLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, pos, 0), speed);
			yield return new WaitForSeconds(speed);
			tween.CmdLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, -pos, 0), speed);
			yield return new WaitForSeconds(speed);
			tween.CmdLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), speed / 2);
			yield return new WaitForSeconds(speed / 2);
		}
	}
}
