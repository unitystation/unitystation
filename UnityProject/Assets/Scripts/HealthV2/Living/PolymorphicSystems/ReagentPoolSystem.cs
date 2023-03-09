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
		private BloodType bloodType = null; //TODO move to Reagent

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


		public override HealthSystemBase CloneThisSystem()
		{
			return new ReagentPoolSystem()
			{
				BloodPool = this.BloodPool.Clone(),
				StartingBlood = this.StartingBlood,
				bloodType = this.bloodType
			};
		}
	}
}
