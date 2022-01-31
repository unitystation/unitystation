using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Light2D;

namespace Effects
{
	public class SparkEffect : MonoBehaviour
	{
		[SerializeField, Min(0)]
		private float time = 1f;

		[SerializeField, FormerlySerializedAs("light")]
		private LightSprite lightSprite = null;

		[SerializeField]
		private bool networkDestroy = true;

		private void OnEnable()
		{
			lightSprite.Color.a = 1f;

			StartCoroutine(EffectTimer());
		}

		private IEnumerator EffectTimer()
		{
			float totalTime = 0;

			while (totalTime < time)
			{
				totalTime += Time.deltaTime;

				lightSprite.Color.a = 1 - (totalTime / time);
				yield return WaitFor.EndOfFrame;
			}

			if (networkDestroy == false)
			{
				gameObject.SetActive(false);
				yield break;
			}

			if (CustomNetworkManager.IsServer == false) yield break;

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
