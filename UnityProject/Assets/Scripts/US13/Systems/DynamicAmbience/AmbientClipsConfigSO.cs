using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Items.Traits;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.Tilemaps.Tiles;
using Util;

namespace US13.Systems.DynamicAmbience
{
	[CreateAssetMenu(fileName = "AmbientClipsConfig", menuName = "ScriptableObjects/Audio/AmbientClipsConfig")]
	public class AmbientClipsConfigSO : ScriptableObject
	{
		public List<ItemTrait> triggerTraits = new List<ItemTrait>();
		public List<AddressableAudioSource> ambientClips = new List<AddressableAudioSource>();
		public List<BasicTile> requiredTiles = new List<BasicTile>();
		public bool needsUnderFloorsNotCovered = false;
		public bool onlyUsesTileChecks = false;
		public int priority = 0;

		public bool CanTrigger(List<ItemTrait> nearbyTraits, GameObject player)
		{
			if (onlyUsesTileChecks && TileChecks(player)) return true;
			return triggerTraits.Any(nearbyTraits.Contains) && TileChecks(player);
		}

		private bool TileChecks(GameObject player)
		{
			var registerTile = player.RegisterTile();
			if (needsUnderFloorsNotCovered && registerTile.IsUnderFloor() == false) return false;
			if (requiredTiles.Count == 0) return true;
			var tile = player.RegisterTile().GetCurrentStandingTile();
			return tile != null && requiredTiles.Contains(tile);
		}

		public string PlayRandomClipLocally()
		{
			var token = Guid.NewGuid().ToString();
			_ = SoundManager.Play(ambientClips.PickRandom(), token,  new AudioSourceParameters( )
			{
				Volume= 0.5f,
				SpatialBlend = 1,
				MixerType = MixerType.Ambient
			});
			return token;
		}
	}
}