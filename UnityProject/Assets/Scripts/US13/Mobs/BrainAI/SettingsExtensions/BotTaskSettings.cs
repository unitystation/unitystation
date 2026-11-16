using System;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Items.Traits;
using US13.Player;
using US13.Player.Language;
using US13.Systems.Inventory;
using US13.Tilemaps.Tiles;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI
{
	public enum MobTaskKind { None, Clean, Floor, Medical, Fire, Security }

	[Serializable]
	public sealed class BotTaskSettings : IMobBehaviorSettingsExtension
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
}
