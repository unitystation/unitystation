using System;
using System.Collections.Generic;
using NaughtyAttributes;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using HealthV2;
using Systems.Clothing;


using UnityEngine;

namespace Objects.Other
{
	/// <summary>
	/// Allows shows to leave footprints when worn.
	/// TODO: Do not make this inherit off FloorHarzard, make a base "FloorTrigger" prefab for this and hazards
	/// </summary>
	///
	public class LeaveFootprints : FloorHazard
	{
		public ReagentContainer reagentContainer;
		//private GameObject me;

		// Update is called once per frame
		void Update()
		{
		}
		public void GiveFootprints(MakesFootPrints print = null, int index = 0)
		{
			if(reagentContainer.ReagentMixTotal > 1f)
			{
				reagentContainer.TransferTo(reagentContainer.ReagentMixTotal * 0.10f, print.spillContents);
			}
		}


		//.AssumedWorldPosServer();

		//MatrixManager.ReagentReact(bloodLoss,
		//	RelatedPart.HealthMaster.gameObject.RegisterTile().WorldPositionServer);

		public override void OnPlayerStep(PlayerScript eventData)
		{
			var playerStorage = eventData.gameObject.GetComponent<DynamicItemStorage>();

			if (playerStorage != null)
			{
				foreach (var feetSlot in playerStorage.GetNamedItemSlots(NamedSlot.feet))
				{
					GiveFootprints(feetSlot.ItemObject.gameObject.GetComponent<MakesFootPrints>());
					//, eventData.gameObject.GetComponent<Rotatable>().CurrentDirection.Degrees
					//eventData.GetComponent<PlayerSync>()
				}
			}

			//base.OnStep(eventData);
		}

		public override bool WillAffectPlayer(PlayerScript eventData)
		{

			var playerStorage = eventData.gameObject.GetComponent<DynamicItemStorage>();
			if (playerStorage != null)
			{
				foreach (var feetSlot in playerStorage.GetNamedItemSlots(NamedSlot.feet))
				{
					//Debug.Log(feetSlot.ItemObject.gameObject);
					//Debug.Log(feetSlot.ItemObject.gameObject.GetComponent<MakesFootPrints>().spillContents);

					if (feetSlot.ItemObject.gameObject.TryGetComponent<MakesFootPrints>(out var _))
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}
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
