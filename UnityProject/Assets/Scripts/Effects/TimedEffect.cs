using System.Collections;
using Mirror;
using UnityEngine;

namespace Effects
{
	public class TimedEffect : MonoBehaviour
	{
		[SerializeField]
		[Min(0)]
		private float time = 1f;

		private void OnEnable()
		{
			if(TryGetComponent<NetworkIdentity>(out _) && CustomNetworkManager.IsServer == false) return;

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

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
