using System.Collections.Generic;
using AddressableReferences;
using Items;
using Mirror;
using UnityEngine;

namespace Objects.Medical
{
	public class MedicalConsole : NetworkBehaviour
	{
		[SerializeField] private AddressableAudioSource warnSound;
		[SerializeField] private float scanInterval = 4.5f;

		[SyncVar] private bool muteWarn = false;
		private List<HealthInfo> crewInfo = new List<HealthInfo>();

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
			crewInfo.Clear();
			var warn = false;
			foreach (var playerInfo in PlayerList.Instance.AllPlayers)
			{
				if (playerInfo.Mind == null) continue;
				if (playerInfo.Mind.NonImportantMind) continue;
				if (playerInfo.Mind.occupation == null) continue;
				if (playerInfo.Mind.occupation.IsCrewmember == false) continue;
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
					crewInfo.Add(info);
					if(info.HealthPercent <= 0.5f) warn = true;
				}
			}
			if (warn) Warn();
		}

	}
}