using Items;
using Systems.Explosions;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Construction.Parts
{
	public class Battery : MonoBehaviour, IEmpAble, IExaminable, IChargeable
	{
		[FormerlySerializedAs("Watts")] [SerializeField]
		private int watts = 9000;

		public int Watts
		{
			get => watts;
			set
			{
				if (SelfCharging)
				{
					if (MaxWatts >= value)
					{
						UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, SelfCharge);
					}
					else
					{
						UpdateManager.Add(SelfCharge, 1);
					}
				}

				watts = value;
			}
		}

		public int MaxWatts = 9000;

		public int InternalResistance = 240;

		public bool isBroken = false;

		public bool SelfCharging = false;

		public int SelfChargeWatts = 0;

		public bool IsFullyCharged =>  watts >= MaxWatts;

		public void SelfCharge()
		{
			if (SelfCharging)
			{
				Watts += SelfChargeWatts;
			}
		}

		public void ChargeBy(float watts)
		{
			if(this.watts + watts > MaxWatts)
			{
				this.watts = MaxWatts;
				return;
			}
			this.watts += (int)watts;
			return;
		}

		public void OnEmp(int EmpStrength)
		{
			watts -= EmpStrength * 100;
			Mathf.Clamp(watts, 0, MaxWatts);

			if(EmpStrength > 50 && DMMath.Prob(25))
			{
				isBroken = true;
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			string status = "";
			if (isBroken)
			{
				status = $"<color=red>It appears to be broken.";
			}
			return $"{gameObject.GetComponent<ItemAttributesV2>().InitialDescription}. Charge indicator shows a {watts/MaxWatts*100} percent charge." +
			       status;
		}
	}
}
