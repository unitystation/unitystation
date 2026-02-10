using Logs;
using NaughtyAttributes;
using UnityEngine;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Objects.Engineering;
using Util;
using Event = US13.Managers.Event;

namespace US13.Systems.Electricity
{
	public class AutoAPCLinker : MonoBehaviour
	{
		[SerializeField] private APCPoweredDevice targetDevice;
		[SerializeField] private float scanRadius;
		[SerializeField] private bool verboseDebugging;

		private void Awake()
		{
			if (CustomNetworkManager.IsServer == false) return;
			EventManager.AddHandler(Event.RoundStarted, UpdateAPCStatus);
		}

		private void OnDestroy()
		{
			EventManager.RemoveHandler(Event.RoundStarted, UpdateAPCStatus);
		}

		[Button()]
		private void UpdateAPCStatus()
		{
			if (targetDevice.RelatedAPC != null)
			{
				if (verboseDebugging) Loggy.Warning("[AutoAPCLinker] - " +
				                                        "Device already has a related APC. Skipping..", Category.Electrical);
				return;
			}
			var result = Physics2D.CircleCastAll(targetDevice.gameObject.AssumedWorldPosServer(), scanRadius, Vector2.zero);
			foreach (var hit in result)
			{
				if (hit.transform.gameObject.TryGetComponent<APC>(out var apc) == false) continue;
				apc.AddDevice(targetDevice);
				if (verboseDebugging)
				{
					Loggy.Info($"[AutoAPCLinker] - Found APC for {targetDevice.gameObject.ExpensiveName()} " +
					           $"at {apc.gameObject.AssumedWorldPosServer()}", Category.Electrical);
				}
				break;
			}
		}
	}
}