using System.Collections.Generic;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	public class BatterySystem : HealthSystemBase
	{
		public List<BatteryPack> BatteryPacks = new List<BatteryPack>();

		public List<BatteryPoweredComponent> Consuming = new List<BatteryPoweredComponent>();

		public float ConsumingWatts = 0;

		public override void BodyPartAdded(BodyPart bodyPart)
		{
			var newSaturation =  bodyPart.GetComponent<BatteryPoweredComponent>();
			if (newSaturation != null)
			{
				if (Consuming.Contains(newSaturation) == false)
				{
					Consuming.Add(newSaturation);
					BodyPartListChange();
				}
			}

			var Battery =  bodyPart.GetComponent<BatteryPack>();
			if (Battery != null)
			{
				if (BatteryPacks.Contains(Battery) == false)
				{
					BatteryPacks.Add(Battery);
				}
			}
		}

		public override void BodyPartRemoved(BodyPart bodyPart)
		{
			var newSaturation =  bodyPart.GetComponent<BatteryPoweredComponent>();
			if (newSaturation != null)
			{
				if (Consuming.Contains(newSaturation))
				{
					Consuming.Remove(newSaturation);
				}

				BodyPartListChange();
			}

			var Battery =  bodyPart.GetComponent<BatteryPack>();
			if (Battery == null) return;
			if (BatteryPacks.Contains(Battery) == false)
			{
				BatteryPacks.Remove(Battery);
			}
		}

		public void BodyPartListChange()
		{
			ConsumingWatts = 0;
			foreach (var Consume in Consuming)
			{
				ConsumingWatts += Consume.WattConsumption;
			}
		}

		public override void SystemUpdate()
		{
			// Calculate the sum of all the numbers in the list
			float sum = 0;

			foreach (var batteryPack in BatteryPacks)
			{
				foreach (var battery in batteryPack.Cells)
				{
					sum += battery.Watts;
				}
			}

			if (sum - ConsumingWatts > 0)
			{
				// Subtract x from the sum and calculate the adjustment factor
				float adjustmentFactor = (sum - ConsumingWatts) / sum;

				// Adjust each number in the list by the factor
				foreach (var batteryPack in BatteryPacks)
				{
					foreach (var Cell in batteryPack.Cells)
					{
						Cell.Watts = Mathf.RoundToInt((Cell.Watts * adjustmentFactor)) ;
					}
				}

				foreach (var consumer in Consuming) //TODO is Bit slow
				{
					if (consumer.PowerModifier.Multiplier != 1)
					{
						consumer.PowerModifier.Multiplier = 1f;
					}
				}
			}
			else
			{
				if (sum > 0)
				{
					foreach (var batteryPack in BatteryPacks)
					{
						foreach (var cell in batteryPack.Cells)
						{
							cell.Watts = 0;
						}
					}
				}

				foreach (var consumer in Consuming) //TODO is Bit slow
				{
					if (consumer.PowerModifier.Multiplier != 0)
					{
						//consumer.PowerModifier.Multiplier = 0.0f; //TODO!!!!
					}
				}
			}
		}

		public override HealthSystemBase CloneThisSystem()
		{
			return new BatterySystem();
		}

	}

}

