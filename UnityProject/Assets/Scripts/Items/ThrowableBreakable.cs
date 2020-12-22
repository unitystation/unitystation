using AddressableReferences;
using NaughtyAttributes;
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
		private bool useCustomSound = false;

		[SerializeField, ShowIf(nameof(useCustomSound))]
		private AddressableAudioSource customSound = default;

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
				SoundManager.PlayNetworkedAtPos(useCustomSound ? customSound : SingletonSOSounds.Instance.GlassBreak01, gameObject.AssumedWorldPosServer());
				Despawn.ServerSingle(gameObject);
			}
		}

	}
}
