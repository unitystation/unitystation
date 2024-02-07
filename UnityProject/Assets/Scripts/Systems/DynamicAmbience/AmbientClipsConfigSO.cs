using System;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Tiles;
using UnityEngine;

namespace Systems.DynamicAmbience
{
	[CreateAssetMenu(fileName = "AmbientClipsConfig", menuName = "ScriptableObjects/Audio/AmbientClipsConfig")]
	public class AmbientClipsConfigSO : ScriptableObject
	{
		public List<ItemTrait> triggerTraits = new List<ItemTrait>();
		public List<AddressableAudioSource> ambientClips = new List<AddressableAudioSource>();
		public List<BasicTile> requiredTiles = new List<BasicTile>();
		public bool needsUnderFloorsNotCovered = false;
		public bool onlyWorksOnMainStation = false;
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
			if (onlyWorksOnMainStation && registerTile.Matrix.IsMainStation == false) return false;
			if (needsUnderFloorsNotCovered && registerTile.IsUnderFloor() == false) return false;
			if (requiredTiles.Count == 0) return true;
			var tile = player.RegisterTile().GetCurrentStandingTile();
			return tile != null && requiredTiles.Contains(tile);
		}

		public void PlayRandomClipLocally()
		{
			_ = SoundManager.Play(ambientClips.PickRandom(), new Guid().ToString());
			Debug.Log("Playing from config: " + name);
		}
	}
}