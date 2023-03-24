using System.Collections.Generic;
using Chemistry;
using Items.Implants.Organs;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	[System.Serializable]
	public class ReagentPoolSystem : HealthSystemBase
	{
		public ReagentMix BloodPool = new ReagentMix();
		public int StartingBlood = 500;

		[SerializeField]
		[Required("Need to know our limits for how much blood we have and what not.")]
		private CirculatoryInfo bloodInfo = null;
		public Chemistry.Reagent CirculatedReagent => bloodType.CirculatedReagent;
		public CirculatoryInfo BloodInfo => bloodInfo;

		[SerializeField]
		[Required("Must have a blood type in a circulatory system.")]
		public BloodType bloodType = null;

		[HideInInspector] public List<Heart> PumpingDevices = new List<Heart>();




		///<summary>
		/// Adds a volume of blood along with the maximum normal reagents
		///</summary>
		public void AddFreshBlood(ReagentMix bloodPool, float amount)
		{
			// Currently only does blood and required reagents, should at nutriments and other common gases
			var bloodToAdd = new ReagentMix(bloodType, amount);
			bloodToAdd.Add(CirculatedReagent, bloodType.GetSpareGasCapacity(bloodToAdd));
			bloodPool.Add(bloodToAdd);
		}
		public override void StartFresh()
		{
			AddFreshBlood(BloodPool, StartingBlood);
		}


		public void Bleed(float amount, bool spawnReagentOnFloor = true)
		{
			var bloodLoss = new ReagentMix();
			BloodPool.TransferTo(bloodLoss, amount);
			if (spawnReagentOnFloor) MatrixManager.ReagentReact(bloodLoss, Base.gameObject.RegisterTile().WorldPositionServer);
		}

		/// <summary>
		/// Returns the total amount of blood in the body of the type of blood the body should have
		/// </summary>
		public float GetTotalBlood()
		{
			return GetSpareBlood();
		}


		/// <summary>
		/// Returns the total amount of 'spare' blood outside of the organs
		/// </summary>
		public float GetSpareBlood()
		{
			return BloodPool.Total;
		}

		public override HealthSystemBase CloneThisSystem()
		{
			return new ReagentPoolSystem()
			{
				BloodPool = this.BloodPool.Clone(),
				StartingBlood = this.StartingBlood,
				bloodType = this.bloodType,
				bloodInfo = this.bloodInfo
			};
		}
	}
}
