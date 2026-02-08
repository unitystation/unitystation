using System;
using System.Collections;
using Chemistry;
using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;
using US13.Core;
using US13.Core.Lifecycle;
using US13.Core.Utils;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Systems.Explosions;
using US13.Systems.Explosions.NodeTypes;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Behaviours.Meta.Atmospherics;
using US13.Tilemaps.Utils;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Systems.ChemistryEffects
{
	[CreateAssetMenu(fileName = "effect", menuName = "ScriptableObjects/Chemistry/Effect/ChemExplosionPull")]
	public class ChemExplosionPull : ChemExplosion
	{
		[SerializeField] private string effectName = "Dark Matter";

		[SerializeField] private GameObject cataclysmPrefab = null;
		[SerializeField] private float cataclysmThreshold = 2000;

		public override IEnumerator NowExplosion(MonoBehaviour sender,ReagentMix ReagentMix,  Vector3 WorldPosition, float amount)
		{
			yield return WaitFor.Seconds(Delay);

			float strength = ChemistryUtils.CalculateYieldFromReaction(amount, potency);
			if (strength <= 0) yield break;
			ExplosionNode node = ExplosionTypes.NodeTypes[explosionType];

			node.ExplosionStartWorldPosition = WorldPosition;
			Explosion.StartExplosion(WorldPosition.RoundToInt(), strength, node, stunNearbyPlayers: false, radiusMultiplier: 10);
			_ = DarkMatterMainOverlay(WorldPosition, Mathf.Abs(strength), node.EffectOverlayType);
		}

		private async UniTask DarkMatterMainOverlay(Vector3 positionOfExplosion, float strength, OverlayType overlayType)
		{
			MatrixInfo matrixInfo = MatrixManager.AtPoint(positionOfExplosion, CustomNetworkManager.IsServer);
			TileChangeManager tileChangeManager = matrixInfo.TileChangeManager;

			Vector3Int local = positionOfExplosion.ToLocal(matrixInfo).RoundToInt();

			if (tileChangeManager.MetaTileMap.HasOverlay(local, TileType.Effects, effectName)) return;
			tileChangeManager.MetaTileMap.AddOverlay(local, TileType.Effects, effectName);

			var darkMatterLightSpawn = Spawn.ServerPrefab(AtmosManager.Instance.DarkMatterLight, positionOfExplosion);

			int miliSeconds = (int)(strength * 0.01f);

			var components = ComponentsTracker<CommonComponents>.GetComponentFromGameObject(darkMatterLightSpawn.GameObject);
			if (components == true)
			{
				if (components.TrySafeGetComponent<UniversalObjectPhysics>(out var physics))
				{
					physics.AppearAtWorldPositionServer(positionOfExplosion);
					physics.Scale.SetScale(Vector3.one * miliSeconds * 0.5f);
				}
			}
			else Loggy.Error("Attempted to access CommonComponents on a DarkMatterLight object, but couldn't find one!");

			ExplosionManager.EffectDataToClean.EffectCallback callback = new();
			callback.callBackAction = SpawnCataclysm;
			callback.position = positionOfExplosion;
			callback.explosionStrength = Math.Abs(strength);

			ExplosionManager.CleanupEffectLater(miliSeconds, tileChangeManager.MetaTileMap,
				local, overlayType, darkMatterLightSpawn.GameObject, callback);

		}

		private void SpawnCataclysm(Vector3 positionOfExplosion, float strength)
		{
			if(strength > cataclysmThreshold) Spawn.ServerPrefab(cataclysmPrefab, positionOfExplosion);
		}
	}
}
