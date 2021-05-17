using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Util;

namespace Effects
{
	public class FloatingEffect : LTEffect
	{
		[SyncVar(hook = nameof(SyncAnimating))]
		private bool isAnimating;
		public bool IsAnimating => isAnimating;

		private const float SPEED = 0.95f;
		private const float POS = 0.08f;

		private void SyncAnimating(bool oldState, bool newState)
		{
			if (newState)
			{
				StartFloating();
				return;
			}

			StopFloating();
		}

		[Server]
		public void ServerToggleFloating(bool state)
		{
			isAnimating = state;
		}

		private void StartFloating()
		{
			StartCoroutine(Animate());
		}

		private void StopFloating()
		{
			tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), 0.01f);
			StopAllCoroutines();
		}

		private IEnumerator Animate()
		{
			while(isAnimating)
			{
				tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, POS, 0), SPEED);
				yield return WaitFor.Seconds(SPEED);
				tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, -POS, 0), SPEED);
				yield return WaitFor.Seconds(SPEED);
				tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), SPEED / 2);
				yield return WaitFor.Seconds(SPEED / 2);
			}
		}
	}
}
