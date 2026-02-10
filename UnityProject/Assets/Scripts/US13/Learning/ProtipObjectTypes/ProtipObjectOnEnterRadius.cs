using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Tilemaps.Behaviours.Objects;
using US13.Tilemaps.Utils;
using Util;

namespace US13.Learning.ProtipObjectTypes
{
	public class ProtipObjectOnEnterRadius : ProtipObject
	{
		[Range(2, 25)] public float SearchRadius = 12f;
		public float SearchCooldown = 6f;
		public LayerMask MaskToCheck;
		public RegisterTile tile;

		private const float SEARCH_LIMIT = 25f;

		private void Start()
		{
			if(CustomNetworkManager.IsHeadless) return;
			tile = gameObject.RegisterTile();
			UpdateManager.Add(CheckForNearbyItems, SearchCooldown);
		}

		public void OnDestroy()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE , CheckForNearbyItems);
		}

		private void CheckForNearbyItems()
		{
			if (tile == null) return;
			var possibleTargets = Physics2D.OverlapCircleAll(tile.WorldPosition.To3(), SearchRadius, MaskToCheck);
			foreach (var target in possibleTargets)
			{
				if (gameObject == target.gameObject) continue;
				if (Vector3.Distance(tile.WorldPosition, target.gameObject.RegisterTile().WorldPosition) > SEARCH_LIMIT) continue;
				if (MatrixManager.Linecast(gameObject.AssumedWorldPosServer(), LayerTypeSelection.Walls,
					    MaskToCheck, target.gameObject.RegisterTile().WorldPosition).ItHit) continue;
				TriggerTip(TipSO, target.gameObject);
			}
		}
	}
}