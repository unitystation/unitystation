using System.Collections.Generic;
using AddressableReferences;
using Items;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Objects.Medical
{
	public class MedicalTerminal : NetworkBehaviour, IRightClickable
	{
		[SerializeField] private AddressableAudioSource warnSound;
		[SerializeField] private float scanInterval = 4.5f;

		[SyncVar] private bool muteWarn = false;
		public List<HealthInfo> CrewInfo { get; private set; } = new List<HealthInfo>();

		public UnityEvent OnScan = new UnityEvent();

		public struct HealthInfo
		{
			public string Info;
			public SuitSensor.SensorMode Mode;
			public float HealthPercent;
		}

		private void Awake()
		{
			if (CustomNetworkManager.IsServer) UpdateManager.Add(Scan, scanInterval);
		}

		private void Warn()
		{
			if (muteWarn) return;
			_ = SoundManager.PlayNetworkedAtPosAsync(warnSound, gameObject.AssumedWorldPosServer());
		}

		public void Scan()
		{
			CrewInfo.Clear();
			var warn = false;
			foreach (var sensor in SuitSensor.WornAndActiveSensors)
			{
				if (sensor.Mode == SuitSensor.SensorMode.OFF) continue; //hmmm This should theoretically never be hit
				HealthInfo info = new HealthInfo
				{
					Info = sensor.GetInfo(),
					Mode = sensor.Mode,
					HealthPercent = sensor.OverallHealth()
				};
				CrewInfo.Add(info);
				if (info.HealthPercent <= 35f) warn = true;
			}

			if (warn) Warn();
			OnScan?.Invoke();
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			return new RightClickableResult().AddElement("Toggle Alert Sound", () => muteWarn = !muteWarn);
		}
	}
}