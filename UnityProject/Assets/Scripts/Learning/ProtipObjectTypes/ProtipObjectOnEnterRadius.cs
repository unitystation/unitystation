using System;
using System.Collections.Generic;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnEnterRadius : ProtipObject
	{
		public Dictionary<GameObject, ProtipSO> ObjectsToCheck = new Dictionary<GameObject, ProtipSO>();
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
				if(gameObject == target.gameObject) continue;
				if (MatrixManager.Linecast(gameObject.AssumedWorldPosServer(), LayerTypeSelection.Walls,
					    MaskToCheck, target.gameObject.AssumedWorldPosServer()).ItHit) continue;
				if(ObjectsToCheck.ContainsKey(target.gameObject) == false) continue;
				TriggerTip(ObjectsToCheck[target.gameObject]);
			}
		}
	}
}