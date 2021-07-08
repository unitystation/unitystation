using System.Collections;
using UnityEngine;

namespace Effects
{
	public class RotateEffect : LTEffect
	{
		private int flips;
		private float animTime;
		private float rotationAngle;
		private bool isRandom = true; //If you're rotating more than once and want your rotations to be random.

		public void StartAnimation()
		{
			StopAllCoroutines();
			StartCoroutine(Rotate(flips, animTime));
		}

		public void SetupEffectvars(int f, float at, float rotAngle, bool random)
		{
			flips = f;
			animTime = at;
			rotationAngle = rotAngle;
			isRandom = random;
		}

		private IEnumerator Rotate(int numberOfrotates, float time)
		{
			float rotationResult = 0.5f;
			if (isRandom)
			{
				rotationResult = Random.value;
			}
			var trackedrotates = 0;
			while (numberOfrotates >= trackedrotates)
			{
				var rot = tween.Target.rotation.eulerAngles;
				rot.z = PickRandomRotation(rot, rotationAngle, rotationResult);
				trackedrotates++;
				RotateObject(rot, time);
				yield return new WaitForSeconds(time);
			}
			base.StopAnimation();
		}

		private void RotateObject(Vector3 rot, float time)
		{
			if(spriteReference != null && isServer == true)
			{
				LeanTween.rotate(spriteReference.gameObject, rot, time);
			}
			tween.RpcRotateGameObject(rot, time);
		}

		private float PickRandomRotation(Vector3 rotation, float target, float result)
		{
			if (result >= 0.5f)
			{
				return rotation.z += target;
			}

			return rotation.z -= target;
		}
	}
}
