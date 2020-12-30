using System;
using System.Collections;
using UnityEngine;
using DigitalRuby.LightningBolt;
using Core.Lighting;

namespace Systems.ElectricalArcs
{
	/// <summary>
	/// Creates an electrical arc effect. Responsible for creating, pulsing and ending arcs.
	/// </summary>
	public class ElectricalArc
	{
		/// <summary>
		/// Subscribe to e.g. do periodic damage.
		/// </summary>
		public event Action<ElectricalArc> OnArcPulse;

		private readonly float PULSE_INTERVAL = 0.5f;

		public ElectricalArcSettings Settings { get; private set; }

		private LightningBoltScript[] arcs;

		/// <summary>
		/// Informs all clients and the server to create arcs with the given settings.
		/// While the arcs are active, the server emits a regular pulse event with which can be subscribed to, for handling damage etc.
		/// </summary>
		/// <returns>an arc instance created on the server</returns>
		public static ElectricalArc ServerCreateNetworkedArcs(ElectricalArcSettings settings)
		{
			ElectricalArcMessage.SendToAll(settings);

			var arc = new ElectricalArc();
			arc.CreateArcs(settings);
			return arc;
		}

		public void CreateArcs(ElectricalArcSettings settings)
		{
			Settings = settings;

			if (CanReach() == false) return;

			arcs = new LightningBoltScript[settings.arcCount];
			for (int i = 0; i < settings.arcCount; i++)
			{
				arcs[i] = CreateSingleArc(settings);
			}

			UpdateManager.Add(TriggerArc, arcs[0].Duration);
			UpdateManager.Add(DoPulse, PULSE_INTERVAL);
			UpdateManager.Add(EndArcs, settings.duration);
		}

		private void TriggerArc()
		{
			// If either end object is null and the position magnitude is
			// less than 1 (meaning we were probably relying on object for position), end the arcing.
			if ((Settings.startObject == null && Settings.startPosition.sqrMagnitude <= Settings.startPosition.normalized.sqrMagnitude) ||
					(Settings.endObject == null && Settings.endPosition.sqrMagnitude <= Settings.endPosition.normalized.sqrMagnitude))
			{
				EndArcs();
			}

			foreach (var arc in arcs)
			{
				// Add some random end position variation for flavour.
				arc.EndPosition = Settings.endPosition +
						new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(-0.3f, 0.3f), 1);
				arc.Trigger();
			}
		}

		private void DoPulse()
		{
			if (CanReach() == false)
			{
				EndArcs();
			}

			if (CustomNetworkManager.IsServer)
			{
				OnArcPulse?.Invoke(this);
			}
		}

		private bool CanReach()
		{
			Vector3 startPos = Settings.startPosition;
			if (Settings.startObject != null)
			{
				startPos += Settings.startObject.transform.position;
			}

			Vector3 endPos = Settings.endPosition;
			if (Settings.endObject != null)
			{
				endPos += Settings.endObject.transform.position;
			}

			var linecast = MatrixManager.Linecast(
					startPos,
					LayerTypeSelection.Walls | LayerTypeSelection.Windows, LayerMask.NameToLayer("Door Closed"),
					endPos);
			return linecast.ItHit == false || Vector3.Distance(endPos, linecast.HitWorld) < 0.1f; // Allow for some raycast/linecast tolerance
		}

		private LightningBoltScript CreateSingleArc(ElectricalArcSettings settings)
		{
			LightningBoltScript arc = GameObject.Instantiate(settings.arcEffectPrefab).GetComponent<LightningBoltScript>();

			arc.StartObject = settings.startObject;
			arc.EndObject = settings.endObject;
			arc.StartPosition = settings.startPosition;
			arc.EndPosition = settings.endPosition;

			arc.GetComponentInChildren<ElectricalArcGlow>().SetIntensity(1f / settings.arcCount);

			return arc;
		}

		private void DespawnArcs()
		{
			foreach (LightningBoltScript arc in arcs)
			{
				GameObject.Destroy(arc.gameObject);
			}
		}

		private void EndArcs()
		{
			DespawnArcs();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TriggerArc);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, DoPulse);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, EndArcs);
		}
	}

	/// <summary>
	/// Contains relevant settings for creating an electrical arc effect, for use with <see cref="ElectricalArc"/>.
	/// </summary>
	public class ElectricalArcSettings
	{
		public readonly GameObject arcEffectPrefab;
		public readonly GameObject startObject;
		public readonly GameObject endObject;
		public readonly Vector3 startPosition;
		public readonly Vector3 endPosition;
		public readonly int arcCount;
		public readonly float duration;

		public ElectricalArcSettings(GameObject arcEffectPrefab, GameObject startObject, GameObject endObject,
				Vector3 startWorldPos, Vector3 endWorldPos, int arcCount = 1, float duration = 2)
		{
			this.arcEffectPrefab = arcEffectPrefab;
			this.startObject = startObject;
			this.endObject = endObject;
			startPosition = startWorldPos;
			endPosition = endWorldPos;
			this.arcCount = arcCount;
			this.duration = duration;
		}
	}
}
