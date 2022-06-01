using System;
using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnEnterRadius : ProtipObject
	{
		public List<ObjectCheckData> ObjectsToCheck = new List<ObjectCheckData>();
		[Range(2, 25)] public float SearchRadius = 12f;
		public float SearchCooldown = 6f;
		public LayerMask MaskToCheck;
		public RegisterTile tile;

		private bool playerSearching = false;
		private const float SEARCH_LIMIT = 25f;

		private void Start()
		{
			if(CustomNetworkManager.IsHeadless) return;
			if (gameObject == PlayerManager.LocalPlayerScript.gameObject) playerSearching = true;
			if(ProtipManager.Instance.PlayerExperienceLevel == ProtipManager.ExperienceLevel.Robust) return;
			tile = gameObject.RegisterTile();
			UpdateManager.Add(CheckForNearbyItems, SearchCooldown);
		}

		private void CheckForNearbyItems()
		{
			if (tile == null) return;
			var possibleTargets = Physics2D.OverlapCircleAll(tile.WorldPosition.ToNonInt3(), SearchRadius, MaskToCheck);
			foreach (var target in possibleTargets)
			{
				if(gameObject == target.gameObject) continue;
				if (Vector3.Distance(tile.WorldPosition, target.gameObject.RegisterTile().WorldPosition) > SEARCH_LIMIT) continue;
				if (MatrixManager.Linecast(tile.WorldPosition, LayerTypeSelection.Walls,
					    MaskToCheck, target.gameObject.RegisterTile().WorldPosition).ItHit) continue;
				foreach (var data in ObjectsToCheck)
				{
					var prefabTracker = data.GameObjectToCheck.GetComponent<PrefabTracker>();
					var targetTracker = target.GetComponent<PrefabTracker>();
					if(prefabTracker == null || targetTracker == null) continue;
					if(prefabTracker.ForeverID != targetTracker.ForeverID) continue;
					TriggerTip(data.AssoicatedSo, playerSearching ? gameObject : null);
					ObjectsToCheck.Remove(data);
					break;
				}
			}
		}

		[Serializable]
		public struct ObjectCheckData : IEquatable<ObjectCheckData>
		{
			public GameObject GameObjectToCheck;
			public ProtipSO AssoicatedSo;

			public bool Equals(ObjectCheckData obj)
			{
				if(obj.AssoicatedSo == null || AssoicatedSo == null) return false;
				if(obj.AssoicatedSo != AssoicatedSo) return false;
				return base.Equals (obj);
			}
		}
	}
}