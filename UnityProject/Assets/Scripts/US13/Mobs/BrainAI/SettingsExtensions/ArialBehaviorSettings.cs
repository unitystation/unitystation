using System;
using System.Collections.Generic;
using US13.Core.Addressables.Types;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI
{
	[Serializable]
	public sealed class ArialBehaviorSettings : IMobBehaviorIntervalSettings
	{
		public float ThrowInterval = 0.35f;
		public List<AddressableAudioSource> EnterSounds = new();
		public AddressableAudioSource ThrowEnterSound;
		public AddressableAudioSource ThrowExitSound;
		public int IntervalId => 4;
		public float IntervalSeconds => ThrowInterval;
	}
}
