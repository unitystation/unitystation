using System;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI
{
	[Serializable]
	public sealed class SlimeBehaviorSettings : IMobBehaviorIntervalSettings
	{
		public float Interval = 1f;
		public bool Friendly;
		public LayerMask HitMask;
		public AddressableAudioSource EatFoodSound;
		public int IntervalId => 3;
		public float IntervalSeconds => Interval;
	}
}
