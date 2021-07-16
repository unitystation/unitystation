using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using Chemistry.Components;
using NaughtyAttributes;

namespace HealthV2
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	public class CirculatorySystemBase : MonoBehaviour
	{

		[SerializeField]
		[Required("Must have a blood type in a circulatory system.")]
		private BloodType bloodType = null;
		public BloodType BloodType => bloodType;
		public ReagentMix UsedBloodPool;
		public ReagentMix ReadyBloodPool;
		public Chemistry.Reagent CirculatedReagent => bloodType.CirculatedReagent;

		[SerializeField]
		[Required("Inital injecton of blood on player spawn")]
		private int StartingBlood = 500;

		[SerializeField]
		[Required("Need to know our limits for how much blood we have and what not.")]
		private CirculatoryInfo bloodInfo = null;
		public CirculatoryInfo BloodInfo => bloodInfo;

		private LivingHealthMasterBase healthMaster;

		public void SetBloodType(BloodType inBloodType)
		{
			bloodType = inBloodType;
		}

		private void Awake()
		{
			healthMaster = GetComponent<LivingHealthMasterBase>();
			AddFreshBlood(ReadyBloodPool, StartingBlood);
		}

		///<summary>
		/// Adds a volume of blood along with the maximum normal reagents
		///</summary>
		public void AddFreshBlood(ReagentMix bloodPool, float amount)
		{
			// Currently only does blood and required reagents, should at nutriments and other common gases
			var bloodToAdd = new ReagentMix(BloodType, amount);
			bloodToAdd.Add(CirculatedReagent, bloodType.GetGasCapacity(bloodToAdd));
			bloodPool.Add(bloodToAdd);
		}

		public void Bleed(float amount)
		{
			var bloodLoss = new ReagentMix();
			ReadyBloodPool.TransferTo(bloodLoss, amount);
			MatrixManager.ReagentReact(bloodLoss, healthMaster.gameObject.RegisterTile().WorldPositionServer);
		}
	}
}
