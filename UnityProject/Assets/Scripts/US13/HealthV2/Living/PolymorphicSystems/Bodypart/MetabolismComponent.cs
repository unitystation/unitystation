using US13.HealthV2.Living;
using UnityEngine;

namespace US13.HealthV2.Living.PolymorphicSystems.Bodypart
{
	[System.Serializable]
	public class MetabolismComponent : BodyPartComponentBase<ChemicalMetabolismSystem>
	{
		[Tooltip("How many metabolic reactions can happen inside of this body part Per tick per 1u of blood flow ")]
		public float ReagentMetabolism = 0.2f;

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("componentImmuneResponse")] private ImmuneResponse _networkComponentImmuneResponse = new ImmuneResponse();
		public ImmuneResponse componentImmuneResponse
		{
			get => _networkComponentImmuneResponse;
			set
			{
				if (System.Collections.Generic.EqualityComparer<ImmuneResponse>.Default.Equals(_networkComponentImmuneResponse, value)) return;
				_networkComponentImmuneResponse = value;
			}
		}

		[HideInInspector]
		[UnityEngine.SerializeReference] public ReagentCirculatedComponent reagentCirculatedComponent;

		[HideInInspector]
		[UnityEngine.SerializeReference] public SaturationComponent saturationComponent;

		public float CurrentBloodSaturation
		{
			get
			{
				if (saturationComponent != null)
				{
					return saturationComponent.currentBloodSaturation;
				}
				else
				{
					return 1;
				}
			}
		}


		public float BloodThroughput
		{
			get
			{
				if (reagentCirculatedComponent != null)
				{
					return reagentCirculatedComponent.Throughput;
				}
				else
				{
					return 1;
				}
			}
		}

		public override void OnInitialize()
		{
			base.OnInitialize();
			reagentCirculatedComponent = GetBodyPartBehavior<ReagentCirculatedComponent>();
			saturationComponent = GetBodyPartBehavior<SaturationComponent>();
		}
		[System.Serializable]
		private sealed class BehaviorNetworkState
		{
			public ImmuneResponse componentImmuneResponse;
		}

		public override string CaptureNetworkState() => JsonUtility.ToJson(new BehaviorNetworkState
		{
			componentImmuneResponse = this.componentImmuneResponse,
		});

		public override void ApplyNetworkState(string json)
		{
			var state = JsonUtility.FromJson<BehaviorNetworkState>(json);
			componentImmuneResponse = state.componentImmuneResponse;
		}

	}
}
