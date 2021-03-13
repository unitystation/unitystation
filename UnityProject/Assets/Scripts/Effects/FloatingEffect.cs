using System;
using System.Collections;
using UnityEngine;
using Util;

namespace Effects
{
	public class FloatingEffect : LTEffect
	{
		public bool animateOnStartup = false;

		[NonSerialized] public bool WillAnimate = false;

		private const float SPEED = 0.95f;
		private const float POS = 0.08f;


		private void Awake()
		{
			if (animateOnStartup)
			{
				StartFloating();
			}
		}

		public void StartFloating()
		{
			WillAnimate = true;
			StartCoroutine(Animate());
		}

		public void StopFloating()
		{
			WillAnimate = false;
			tween.RpcLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), 0.01f);
			StopAllCoroutines();
		}

		private IEnumerator Animate()
		{
			while(WillAnimate)
			{
				tween.RpcLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, POS, 0), SPEED);
				yield return WaitFor.Seconds(SPEED);
				tween.RpcLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, -POS, 0), SPEED);
				yield return WaitFor.Seconds(SPEED);
				tween.RpcLocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), SPEED / 2);
				yield return WaitFor.Seconds(SPEED / 2);
			}
		}
	}
}
