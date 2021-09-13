using AddressableReferences;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
	/// <summary>
	/// Allows a gameobject to transform into a different object when it is thrown, upon landing.
	/// </summary>
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

		private void Awake()
		{
			customNetTransform = GetComponent<CustomNetTransform>();
		}

		private void OnEnable()
		{
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
				SoundManager.PlayNetworkedAtPos(useCustomSound ? customSound : CommonSounds.Instance.GlassBreak01, gameObject.AssumedWorldPosServer());
				_ = Despawn.ServerSingle(gameObject);
			}
		}
	}
}
