using System;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Attributes;
using US13.Items.Traits;
using US13.Mobs.Traversal;
using US13.Player;
using US13.Player.Language;
using US13.Systems.Inventory;
using US13.Tilemaps.Tiles;

namespace US13.Mobs.BrainAI
{
	public enum MobTaskKind { None, Clean, Floor, Medical, Fire, Security }
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
		public BotTaskSettings Task = new();
		public SlimeBehaviorSettings Slime = new();
		public ArialBehaviorSettings Arial = new();
	}
	[Serializable]
	public sealed class BotTaskSettings
	{
		public MobTaskKind Kind;
		public float Duration = 2f;
		public AddressableAudioSource PerformSound;
		public AddressableAudioSource EmaggedPerformSound;
		public LanguageSO Language;
		public List<AudibleMobDialogue> EnterDialogue = new();
		public List<AudibleMobDialogue> ExitDialogue = new();
		public List<AudibleMobDialogue> IdleDialogue = new();
		public List<AudibleMobDialogue> IdleEmaggedDialogue = new();
		public List<AudibleMobDialogue> FoundDialogue = new();
		public float DialogueChancePercent = 50;
		public List<PlayerHealthData> BlacklistedSpecies = new();
		public LayerTile TileToPlace;
		public Reagent ReagentToSpill;
		public int SprayDistance = 6;
		public ReagentMix FireRetardantMix;
		public ReagentMix EmaggedChemicalMix;
		public ItemStorage InternalStorage;
		public ItemTrait HandcuffTrait;
	}
	[Serializable]
	public sealed class SlimeBehaviorSettings
	{
		public float Interval = 1f;
		public bool Friendly;
		public LayerMask HitMask;
		public AddressableAudioSource EatFoodSound;
	}
	[Serializable]
	public sealed class ArialBehaviorSettings
	{
		public float ThrowInterval = 0.35f;
		public List<AddressableAudioSource> EnterSounds = new();
		public AddressableAudioSource ThrowEnterSound;
		public AddressableAudioSource ThrowExitSound;
	}
}
