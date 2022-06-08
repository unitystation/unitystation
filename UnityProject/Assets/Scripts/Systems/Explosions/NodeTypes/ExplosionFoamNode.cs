using System;
using Systems.Explosions;
using Chemistry;
using AddressableReferences;
using UnityEngine;
using TileManagement;
using ScriptableObjects;

namespace Systems.Explosions
{
	public class ExplosionFoamNode : ExplosionNode
    {
		private GameObject FoamPrefab = CommonPrefabs.Instance.ChemFoam;
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
			if (matrix.IsAtmosPassableAt(pos, true))
			{
				SpawnFoam(matrix, pos, Reagents);
			}
			return 10.0f; //magic number
		}

		private void SpawnFoam(Matrix matrix, Vector3Int pos, ReagentMix usedReagents)
		{
			SpawnResult result = Spawn.ServerPrefab(FoamPrefab, pos);
			ChemFoam foam = result.GameObject.GetComponent<ChemFoam>();
			foam.Matrix = matrix;
			foam.Reagents = usedReagents;
		}
	}
}
