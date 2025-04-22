using System;
using System.Collections;
using UnityEngine;
using Systems.Explosions;
using HealthV2;
using Core;
using Cysharp.Threading.Tasks;
using Logs;
using Systems.Atmospherics;
using TileManagement;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "effect", menuName = "ScriptableObjects/Chemistry/Effect/ChemExplosionPullPush")]
	public class ChemExplosionPullPush : ChemExplosion
	{
		[SerializeField, Tooltip("Should this effects force push not pull")] private bool ShouldPush = false;
		[SerializeField] private string effectName = "Dark Matter";


		[SerializeField] private GameObject cataclysmPrefab = null;
		[SerializeField] private float cataclysmThreshold = 2000;

		public override IEnumerator NowExplosion(MonoBehaviour sender, float amount)
		{
			yield return WaitFor.Seconds(Delay);

			UniversalObjectPhysics objectBehaviour = sender.GetComponentCustom<UniversalObjectPhysics>();
			RegisterObject registerObject = sender.GetComponentCustom<RegisterObject>();
			BodyPart bodyPart = sender.GetComponentCustom<BodyPart>();
			ExplosionNode node = ExplosionTypes.NodeTypes[explosionType];

			float strength = ChemistryUtils.CalculateYieldFromReaction(amount, potency);

			bool insideBody = bodyPart == true && bodyPart.HealthMaster == true;

			var picked = sender.GetComponentCustom<Pickupable>();
			if (picked != null)
			{
				//If sender is in an inventory use the position of the inventory.
				if (picked.ItemSlot != null)
				{
					objectBehaviour = picked.ItemSlot.ItemStorage.GetRootStorageOrPlayer().GetComponentCustom<UniversalObjectPhysics>();
					registerObject = picked.ItemSlot.ItemStorage.GetRootStorageOrPlayer().GetComponentCustom<RegisterObject>();
				}
			}

			if (strength <= 0) yield break;

			strength = ShouldPush ? -strength : strength;
			Vector3 positionOfExplosion;

			if (registerObject == null)
			{
				if (insideBody) positionOfExplosion = bodyPart.HealthMaster.RegisterTile.WorldPosition;
				else positionOfExplosion = objectBehaviour.registerTile.WorldPosition;
			}
			else positionOfExplosion = registerObject.WorldPosition;

			Explosion.StartExplosion(positionOfExplosion.CutToInt(), strength, node, stunNearbyPlayers: false, radiusMultiplier: 10);

			DarkMatterMainOverlay(positionOfExplosion, Mathf.Abs(strength), node.EffectOverlayType);
		}

		private async UniTask DarkMatterMainOverlay(Vector3 positionOfExplosion, float strength, OverlayType overlayType)
		{
			MatrixInfo matrixInfo = MatrixManager.AtPoint(positionOfExplosion, CustomNetworkManager.IsServer);
			TileChangeManager tileChangeManager = matrixInfo.TileChangeManager;

			Vector3Int local = positionOfExplosion.ToLocal(matrixInfo).CutToInt();

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
