using System;
using System.Collections;
using Mirror;
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

		/// <summary>
		/// Use for short duration floating effects as this will not sync to late joining clients
		/// </summary>
		/// <param name="newState"></param>
		[ClientRpc]
		public void RpcServerToggleFloat(bool newState)
		{
			if (newState)
			{
				StartFloating();
				return;
			}

			StopFloating();
		}

		public void StartFloating()
		{
			WillAnimate = true;
			StartCoroutine(Animate());
		}

		public void StopFloating()
		{
			WillAnimate = false;
		}

		private IEnumerator Animate()
		{
			while(WillAnimate)
			{
				tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, POS, 0), SPEED);
				yield return WaitFor.Seconds(SPEED);
				tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, -POS, 0), SPEED);
				yield return WaitFor.Seconds(SPEED);
				tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), SPEED / 2);
				yield return WaitFor.Seconds(SPEED / 2);
			}
			tween.LocalMove(NetworkedLeanTween.Axis.Y, new Vector3(0, 0, 0), 0.1f);
		}
	}
}
