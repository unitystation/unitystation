using System;
using System.Collections.Generic;
using NaughtyAttributes;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using HealthV2;

using UnityEngine;

namespace Objects.Other
{
	/// <summary>
	/// Allows shows to leave footprints when worn.
	/// </summary>
	/// 
	public class LeaveFootprints : FloorHazard
	{
		private ReagentContainer reagentContainer;
		//private GameObject me;

		private void Awake()
		{
		
		}

		// Update is called once per frame
		void Update()
		{
		}
		public void GiveFootprints(MakesFootPrints print = null)
		{
			Debug.Log("givefootprint");
			Debug.Log(print.spillContents.MajorMixReagent.description);

		}


		//.AssumedWorldPosServer();

		//MatrixManager.ReagentReact(bloodLoss,
		//	RelatedPart.HealthMaster.gameObject.RegisterTile().WorldPositionServer);

		public override void OnStep(GameObject eventData)
		{
			GiveFootprints();
			//base.OnStep(eventData);
		}

		public override bool WillStep(GameObject eventData)
		{
			Debug.Log(eventData);

			Debug.Log("step?");
			if (eventData.gameObject.TryGetComponent<MakesFootPrints>(out var _)) return true;
			//if (eventData.gameObject.TryGetComponent<LivingHealthMasterBase>(out var _)) return true;
			return false;
		}



		/*
		Vector3Int worldPos = interaction.WorldPositionTarget.RoundToInt();
		MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
		Vector3Int localPos = MatrixManager.WorldToLocalInt(worldPos, matrixInfo);

		MatrixManager.ReagentReact(reagentContainer.TakeReagents(reagentsPerUse), worldPos);
		*/
	}
}
