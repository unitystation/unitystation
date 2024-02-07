using System;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using UnityEngine;

namespace Systems.DynamicAmbience
{
	[CreateAssetMenu(fileName = "AmbientClipsConfig", menuName = "ScriptableObjects/Audio/AmbientClipsConfig")]
	public class AmbientClipsConfigSO : ScriptableObject
	{

		public List<ItemTrait> triggerTraits = new List<ItemTrait>();
		public List<AddressableAudioSource> ambientClips = new List<AddressableAudioSource>();

		public bool CanTrigger(List<ItemTrait> nearbyTraits)
		{
			return triggerTraits.Any(nearbyTraits.Contains);
		}

		public void PlayRandomClipLocally()
		{
			_ = SoundManager.Play(ambientClips.PickRandom(), new Guid().ToString());
		}
	}
}