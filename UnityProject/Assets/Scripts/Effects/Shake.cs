using System.Collections;
using UnityEngine;

namespace Effects
{
	public class Shake : LTEffect
	{
		/// <summary>
		/// A shake effect that shakes an entire GameObject or it's sprites only.
		/// </summary>

		private float shakeDuration;
		private float shakeDistance;
		private float delayBetweenShakes;
		private void Awake()
		{
			GetOriginalPosition();
		}

		private void StoreShakeData(float duration, float distance, float delay)
		{
			shakeDistance = distance;
			shakeDuration = duration;
			delayBetweenShakes = delay;
		}

		public void StartShake(float duration, float distance, float delay)
		{
			StoreShakeData(duration, distance, delay);
			StartCoroutine(Shaking(shakeDuration, shakeDistance, delayBetweenShakes));
		}

		public override void RpcStopAnim()
		{
			HaltShake();
			base.RpcStopAnim();
		}
		public void HaltShake()
		{
			StopAllCoroutines();
			if(animType == AnimMode.GAMEOBJECT)
			{
				transform.position = originalPosition;

			}
			else
			{
				//(MaxIsJoe) : Please do not use transform.position for this again unless you want
				//Sprites to disappear into hiddenpos.
				spriteReference.transform.localPosition = new Vector2(0,0);
			}
		}

		private IEnumerator Shaking(float duration, float distance, float delayBetweenShakes)
		{
			float timer = 0f;

			GetOriginalPosition(); //Fix for teleporting objects that have moved away from their original location on spawn.

			while (timer < duration)
			{
				timer += Time.deltaTime;

				Vector3 randomPosition = tween.Target.transform.position + (Random.insideUnitSphere * distance);

				switch (animType)
				{
					case AnimMode.GAMEOBJECT:
						AnimatePosition(randomPosition);
						break;
					case AnimMode.SPRITE:
						randomPosition = new Vector3(0,0,0) + (Random.insideUnitSphere * distance);
						AnimateSpritePosition(randomPosition);
						break;
				}


				if (delayBetweenShakes > 0f)
				{
					yield return new WaitForSeconds(delayBetweenShakes);
				}
				else
				{
					yield return null;
				}
			}

			switch (animType)
			{
				case AnimMode.GAMEOBJECT:
					LeanTween.move(gameObject, originalPosition, 0.1f);
					break;
				case AnimMode.SPRITE:
					LeanTween.moveLocal(spriteReference.gameObject, new Vector3(0,0,0), 0.1f);
					break;
			}

		}

		//We call regular LeanTween alongside NTL to make sure the animation plays on the server as well.

		private void AnimateSpritePosition(Vector3 pos)
		{
			LeanTween.moveLocal(spriteReference.gameObject, pos, 0.1f);
			tween.RpcLocalMove(axisMode, pos, 0.1f);
		}

		private void AnimatePosition(Vector3 pos)
		{
			LeanTween.move(gameObject, originalPosition, 0.1f);
			tween.RpcMove(axisMode, pos, 0.1f);
		}
	}
}
