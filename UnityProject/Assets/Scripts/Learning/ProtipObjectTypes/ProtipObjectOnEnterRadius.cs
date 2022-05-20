using System;
using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnEnterRadius : ProtipObject
	{
		public List<ObjectCheckData> ObjectsToCheck = new List<ObjectCheckData>();
		public float SearchRadius = 12f;
		public float SearchCooldown = 6f;
		public LayerMask MaskToCheck;

		private void Start()
		{
			if(CustomNetworkManager.IsHeadless) return;
			if(ProtipManager.Instance.PlayerExperienceLevel == ProtipManager.ExperienceLevel.Robust) return;
			UpdateManager.Add(CheckForNearbyItems, SearchCooldown);
		}

		private void CheckForNearbyItems()
		{
			var possibleTargets = Physics2D.OverlapCircleAll(gameObject.RegisterTile().WorldPosition.ToNonInt3(), SearchRadius, MaskToCheck);
			foreach (var target in possibleTargets)
			{
				if(gameObject == target.gameObject) continue;
				if (MatrixManager.Linecast(gameObject.RegisterTile().WorldPosition, LayerTypeSelection.Walls,
					    MaskToCheck, target.gameObject.RegisterTile().WorldPosition).ItHit) continue;
				foreach (var data in ObjectsToCheck)
				{
					var prefabTracker = data.GameObjectToCheck.GetComponent<PrefabTracker>();
					var targetTracker = target.GetComponent<PrefabTracker>();
					if(prefabTracker == null || targetTracker == null) continue;
					if(prefabTracker.ForeverID != targetTracker.ForeverID) continue;
					TriggerTip(data.AssoicatedSo);
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