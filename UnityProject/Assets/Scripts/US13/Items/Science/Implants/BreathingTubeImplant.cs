using NaughtyAttributes;
using UnityEngine;
using US13.HealthV2.Living;
using Util;

namespace US13.Items.Science.Implants
{
	[System.Serializable]
	public class BreathingTubeImplant : BodyPartBehavior
	{
		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("isEMPed")] private bool _networkIsEMPed = false;
		public bool isEMPed
		{
			get => _networkIsEMPed;
			private set
			{
				if (System.Collections.Generic.EqualityComparer<bool>.Default.Equals(_networkIsEMPed, value)) return;
				_networkIsEMPed = value;
			}
		}

		public bool isEMPVunerable = false;

		[ShowIf("isEMPVunerable")]
		public int EMPResistance = 2;

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			RelatedPart.HealthMaster.RespiratorySystem.AddImplant(this);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			RelatedPart.HealthMaster.RespiratorySystem.RemoveImplant(this);
		}

		public override void OnEmp(int strength)
		{
			if (isEMPVunerable == false) return;

			if (EMPResistance == 0 || DMMath.Prob(100 / EMPResistance))
			{
				isEMPed = true;
			}
		}

		[System.Serializable]
		private sealed class BehaviorNetworkState
		{
			public bool isEMPed;
		}

		public override string CaptureNetworkState() => JsonUtility.ToJson(new BehaviorNetworkState
		{
			isEMPed = this.isEMPed,
		});

		public override void ApplyNetworkState(string json)
		{
			var state = JsonUtility.FromJson<BehaviorNetworkState>(json);
			isEMPed = state.isEMPed;
		}

	}
}
