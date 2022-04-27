using System;
using System.Collections.Generic;
using UnityEngine;

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
					Debug.Log(target.name);
					Debug.DrawLine(gameObject.AssumedWorldPosServer(), target.gameObject.AssumedWorldPosServer(), Color.green, 25f);
				}
				if(gameObject == target.gameObject) continue;
				if (MatrixManager.Linecast(gameObject.AssumedWorldPosServer(), LayerTypeSelection.Walls,
					    MaskToCheck, target.gameObject.AssumedWorldPosServer()).ItHit == false) continue;
				foreach (var data in ObjectsToCheck)
				{
					if(data.GameObjectToCheck != target.gameObject) continue;
					TriggerTip(data.AssoicateSo);
				}
			}
		}

		[Serializable]
		public struct ObjectCheckData
		{
			public GameObject GameObjectToCheck;
			public ProtipSO AssoicateSo;
		}
	}
}