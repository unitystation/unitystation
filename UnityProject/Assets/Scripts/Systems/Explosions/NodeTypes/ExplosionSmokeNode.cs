using System;
using Systems.Explosions;
using Chemistry;
using AddressableReferences;
using UnityEngine;
using TileManagement;
using ScriptableObjects;

namespace Systems.Explosions
{
	class ExplosionSmokeNode : ExplosionNode
    {
		private GameObject SmokePrefab = CommonPrefabs.Instance.ChemSmoke;
		public override string EffectName
		{
			get { return null; }
		}
		public override OverlayType EffectOverlayType
		{
			get { return OverlayType.None; }
		}
		public override AddressableAudioSource CustomSound
		{
			get { return CommonSounds.Instance.Smoke; }
		}

		public override float DoDamage(Matrix matrix, float DamageDealt, Vector3Int pos)
		{
			SpawnSmoke(matrix, pos, Reagents);
			return 10.0f; //magic number
		}

		private void SpawnSmoke(Matrix matrix, Vector3Int pos, ReagentMix usedReagents)
        {
			SpawnResult result = Spawn.ServerPrefab(SmokePrefab, pos);
			ChemSmoke smoke = result.GameObject.GetComponent<ChemSmoke>();
			smoke.Matrix = matrix;
			smoke.Reagents = usedReagents;
        }
	}
}