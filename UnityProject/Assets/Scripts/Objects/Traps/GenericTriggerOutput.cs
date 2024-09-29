using System.Collections.Generic;
using UnityEngine;
using Shared.Systems.ObjectConnection;
using Logs;
using Objects.Logic;
using Mirror;
using Systems.Clearance;

namespace Objects.Traps
{
	public class GenericTriggerOutput : NetworkBehaviour, IMultitoolMasterable
	{

		[SerializeField]
		private List<GameObject> genericTriggerObjects = new List<GameObject>();

		private List<IGenericTrigger> genericTriggers = new List<IGenericTrigger>();

		protected virtual void Awake()
		{
			SyncList();
		}

		protected void OnEnable()
		{
			SyncList();
		}

		protected void SyncList()
		{
			genericTriggers.Clear();
			List<GameObject> iterables = new List<GameObject>(genericTriggerObjects);
			foreach (var gobj in iterables)
			{
				if(gobj.TryGetComponent<IGenericTrigger>(out var trigger) == false)
				{
					genericTriggerObjects.Remove(gobj);
					Loggy.LogWarning($"[GenericTriggerOutput/SyncList] Gameobject {gobj.name} did not have IGenericTrigger interface. Removing from list...");
					continue;
				}
				genericTriggers.Add(trigger);
			}
		}

		public void TriggerOutput()
		{
			foreach(IGenericTrigger trigger in genericTriggers)
			{
				if (trigger == null)
				{
					Loggy.LogWarning($"[GenericTriggerOutput/TriggerOutput] Trigger in genericTrigger list was null! Removing...");
					RemoveTrigger(trigger);
				}
				trigger.OnTrigger();
			}
		}

		public void TriggerOutputWithClearance(IClearanceSource source)
		{
			foreach (IGenericTrigger trigger in genericTriggers)
			{
				if (trigger == null)
				{
					Loggy.LogWarning($"[GenericTriggerOutput/TriggerOutputWithClearance] Trigger in genericTrigger list was null! Removing...");
					RemoveTrigger(trigger);
				}
				trigger.OnTriggerWithClearance(source);
			}
		}

		public void ReleaseOutput()
		{
			foreach (IGenericTrigger trigger in genericTriggers)
			{
				if (trigger == null)
				{
					Loggy.LogWarning($"[GenericTriggerOutput/ReleaseOutput] Trigger in genericTrigger list was null! Removing...");
					RemoveTrigger(trigger);
				}
				trigger.OnTriggerEnd();
			}
		}

		#region Multitool

		public MultitoolConnectionType ConType => MultitoolConnectionType.GenericTrigger;

		int IMultitoolMasterable.MaxDistance => 50;

		[field: SerializeField] public bool CanRelink { get; set; } = true;
		[field: SerializeField] public bool CanBeMastered { get; private set; } = true;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = true;

		public void RemoveTrigger(IGenericTrigger genericTrigger)
		{
			if (genericTriggers.Contains(genericTrigger) == false) return;

			genericTriggers.Remove(genericTrigger);
			genericTriggerObjects.Remove(genericTrigger.gameObject);

			genericTrigger.OnTriggerEnd();
		}

		public void AddTrigger(IGenericTrigger genericTrigger)
		{
			if (genericTriggers.Contains(genericTrigger)) return;

			genericTriggers.Add(genericTrigger);
			genericTriggerObjects.Add(genericTrigger.gameObject);
		}

		public void SubscribeToController(GameObject potentialObject)
		{
			//Logic Gates can have more than one IGenericTrigger per gameObject so GetComponent won't work.
			//We ask the gate to get the correct interface.
			if (potentialObject.TryGetComponent<LogicGate>(out var gate)) 
			{
				IGenericTrigger trigger = gate.RetrieveTrigger();
				if(trigger != null) AddGenericTrigger(trigger);
				return;
			}

			var genericTrigger = potentialObject.GetComponent<IGenericTrigger>();
			if (genericTrigger == null) return;

			AddGenericTrigger(genericTrigger);
		}

		private void AddGenericTrigger(IGenericTrigger genericTrigger)
		{
			if (genericTriggers.Contains(genericTrigger)) RemoveTrigger(genericTrigger);
			else AddTrigger(genericTrigger);
		}

		#endregion
	}
}
