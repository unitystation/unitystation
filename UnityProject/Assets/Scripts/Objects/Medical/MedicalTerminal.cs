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
			foreach (var playerInfo in PlayerList.Instance.AllPlayers)
			{
				if (playerInfo.Mind == null) continue;
				if (playerInfo.Mind.NonImportantMind) continue;
				var uniforms =
					playerInfo.Mind.CurrentPlayScript.Equipment.ItemStorage.GetNamedItemSlots(NamedSlot.uniform);
				foreach (var uniform in uniforms)
				{
					if (uniform.IsEmpty) continue;
					if (uniform.ItemObject.TryGetComponent<SuitSensor>(out var sensor) == false) continue;
					if (sensor.Mode == SuitSensor.SensorMode.OFF) continue;
					HealthInfo info = new HealthInfo
					{
						Info = sensor.GetInfo(),
						Mode = sensor.Mode,
						HealthPercent = sensor.OverallHealth(playerInfo.Mind.CurrentPlayScript.playerHealth),
					};
					CrewInfo.Add(info);
					if(info.HealthPercent <= 35f) warn = true;
				}
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