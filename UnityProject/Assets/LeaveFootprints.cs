using System;
using System.Collections.Generic;
using NaughtyAttributes;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using Player;

using UnityEngine;

namespace Items
{
	/// <summary>
	/// Allows shows to leave footprints when worn.
	/// </summary>
	/// 
	public class LeaveFootprints : MonoBehaviour
	{
		private ReagentContainer reagentContainer;
		//private GameObject me;

		private void Awake()
		{
			if (!reagentContainer)
			{
				reagentContainer = GetComponent<ReagentContainer>();
			}
		
		}

		// Update is called once per frame
		void Update()
		{

		}

		//.AssumedWorldPosServer();

		//MatrixManager.ReagentReact(bloodLoss,
			//	RelatedPart.HealthMaster.gameObject.RegisterTile().WorldPositionServer);



	/*
	Vector3Int worldPos = interaction.WorldPositionTarget.RoundToInt();
	MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
	Vector3Int localPos = MatrixManager.WorldToLocalInt(worldPos, matrixInfo);

	MatrixManager.ReagentReact(reagentContainer.TakeReagents(reagentsPerUse), worldPos);
	*/
	}
}
