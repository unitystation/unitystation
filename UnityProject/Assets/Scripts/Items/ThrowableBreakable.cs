using AddressableReferences;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Items
{
	public class ThrowableBreakable : MonoBehaviour
	{
		[SerializeField]
		private GameObject brokenItem = default;

		[SerializeField, Range(0, 100)]
		private int chanceToBreak = 100;

		[SerializeField]
		private AddressableAudioSource soundToPlay = SingletonSOSounds.Instance.GlassBreak01;

		private CustomNetTransform customNetTransform;

		private void Start()
		{
			customNetTransform = GetComponent<CustomNetTransform>();
			customNetTransform.OnThrowEnd.AddListener(OnThrown);
		}

		private void OnDisable()
		{
			customNetTransform.OnThrowEnd.RemoveListener(OnThrown);
		}

		private void OnThrown(ThrowInfo info)
		{
			if (DMMath.Prob(chanceToBreak))
			{
				Spawn.ServerPrefab(brokenItem, gameObject.AssumedWorldPosServer());
				SoundManager.PlayNetworkedAtPos(soundToPlay, gameObject.AssumedWorldPosServer());
				Despawn.ServerSingle(gameObject);
			}
		}

	}
}
