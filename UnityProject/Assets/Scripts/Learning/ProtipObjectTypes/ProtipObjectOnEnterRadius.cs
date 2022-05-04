using System;
using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnEnterRadius : ProtipObject
	{
		public List<ObjectCheckData> ObjectsToCheck = new List<ObjectCheckData>();
		public float SearchRadius = 25f;
		public float SearchCooldown = 6f;
		public LayerMask MaskToCheck;

		private void Start()
		{
			if(CustomNetworkManager.IsServer && Application.isEditor == false) return;
			if(ProtipManager.Instance.PlayerExperienceLevel == ProtipManager.ExperienceLevel.Robust) return;
			UpdateManager.Add(CheckForNearbyItems, SearchCooldown);
		}

		private void CheckForNearbyItems()
		{
			var possibleTargets = Physics2D.OverlapCircleAll(gameObject.AssumedWorldPosServer(), SearchRadius, MaskToCheck);
			foreach (var target in possibleTargets)
			{
				if (Application.isEditor)
				{
					Debug.DrawLine(gameObject.AssumedWorldPosServer(), target.gameObject.AssumedWorldPosServer(), Color.green, 8f);
				}
				if(gameObject == target.gameObject) continue;
				if (MatrixManager.Linecast(gameObject.AssumedWorldPosServer(), LayerTypeSelection.Walls,
					    MaskToCheck, target.gameObject.AssumedWorldPosServer()).ItHit) continue;
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

			public override bool Equals(object obj)
			{
				if (obj == null || GetType() != obj.GetType())
				{
					return false;
				}
				
				return base.Equals (obj);
			}
		}
	}
}