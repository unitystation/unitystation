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

		private bool networked;

		private void OnEnable()
		{
			networked = TryGetComponent<NetworkIdentity>(out _);
			if(networked && CustomNetworkManager.IsServer == false) return;

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

			if (networked)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
			else
			{
				_ = Despawn.ClientSingle(gameObject);
			}
		}
	}
}
