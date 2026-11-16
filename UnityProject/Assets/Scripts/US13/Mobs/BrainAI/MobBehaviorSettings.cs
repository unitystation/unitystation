using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Attributes;
using US13.Mobs.Traversal;

namespace US13.Mobs.BrainAI
{
	/// <summary>Shared movement and timing configuration; optional capabilities use settings extensions.</summary>
	[Serializable]
	public sealed class MobBehaviorSettings
	{
		public float WanderInterval = 1.5f;
		public float SearchInterval = 1.5f;
		public float FollowInterval = 1.5f;
		public float FollowDistance = 4f;
		public AddressableAudioSource FollowEnterSound;
		public AddressableAudioSource FollowExitSound;
		public Vector2Int Hesitation = new(5, 7);
		[SerializeReference, SelectImplementation(typeof(ITraversalStrat))]
		public List<ITraversalStrat> TraversalStrategies = new();
	}
}
