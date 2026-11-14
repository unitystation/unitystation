using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Attributes;
using US13.Core.Networking.AsyncMessageQueue;
using US13.Core.Utils;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Managers.UpdateManager;
using US13.Systems;
using US13.Systems.Explosions;
using US13.Systems.Inventory;
using Util;

namespace US13.HealthV2.Living
{
	[DisallowMultipleComponent]
	public sealed class BodyPartBehaviors : NetworkBehaviour, IServerInventoryMove, IEmpAble
	{
		[SerializeReference] [SelectImplementation(typeof(IBodyPartBehavior))]
		public List<IBodyPartBehavior> Behaviors = new();

		// Retained for serialized prefabs. LivingHealthMasterBase owns the health tick rate.
		[HideInInspector] public float SecondsUntilNextTick = 1f;
		private bool initialized;
		private bool systemsSetUp;
		private LivingHealthMasterBase attachedHealth;
		private readonly List<IBodyPartBehavior> attachedBehaviors = new();
		private readonly HashSet<IBodyPartBehavior> enabledBehaviors = new();
		private string ticket;
		private int clientNetworkGeneration;

		private bool HasNetworkState => Enumerable.OfType<BodyPartBehavior>(Behaviors).Any(behavior => behavior.CaptureNetworkState() != null);

		private readonly Dictionary<int, string> lastSentStates = new();

		private void Awake()
		{
			Initialize();
		}

		public void Initialize()
		{
			if (initialized) return;
			initialized = true;
			// Bind the complete graph before any behavior tries to find a sibling behavior.
			foreach (IBodyPartBehavior behavior in Behaviors) behavior?.Initialize(this);
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				if (behavior != null) InvokeBehavior(behavior, behavior.OnInitialize);
			}
		}

		private void Start()
		{
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				if (IsEnabled(behavior))
				{
					InvokeBehavior(behavior, behavior.OnBehaviorStarted);
				}
			}
		}

		private void OnEnable()
		{
			Initialize();
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				if (IsEnabled(behavior))
				{
					enabledBehaviors.Add(behavior);
					InvokeBehavior(behavior, behavior.OnBehaviorEnabled);
				}
			}
		}

		private void OnDisable()
		{
			// A flag may have changed since OnEnable; every acquired subscription still needs cleanup.
			foreach (IBodyPartBehavior behavior in enabledBehaviors)
				InvokeBehavior(behavior, behavior.OnBehaviorDisabled);
			enabledBehaviors.Clear();
		}

		private void OnDestroy()
		{
			if (!initialized) return;
			if (attachedHealth != null) OnRemovedFromBody(attachedHealth);
			var part = GetComponent<BodyPart>();
			if (part != null && part.HealthMaster != null) part.BodyPartRemoveHealthMaster();
			StopNetworking();
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				if (behavior != null) InvokeBehavior(behavior, behavior.OnBehaviorDestroyed);
			}
		}

		private static void InvokeBehavior(IBodyPartBehavior behavior, Action callback)
		{
			try { callback(); }
			catch (Exception exception)
			{
				Loggy.Error($"Error on body part behavior {behavior.GetType().Name} ({callback.Method.Name}):\n{exception}");
			}
		}

		private static bool IsEnabled(IBodyPartBehavior behavior)
		{
			return behavior != null && (behavior is not BodyPartBehavior managed || managed.Enabled);
		}

		// BodyPart owns organ ticking, including contained parts. Scheduling it here as well
		// would tick implanted organs twice and run detached organs without a health master.
		public void BehaviorsTick()
		{
			if (attachedHealth == null) return;
			var health = attachedHealth;
			for (int i = attachedBehaviors.Count - 1; i >= 0 && attachedHealth == health; i--)
			{
				if (i >= attachedBehaviors.Count) continue;
				IBodyPartBehavior behavior = attachedBehaviors[i];
				if (IsEnabled(behavior)) InvokeBehavior(behavior, behavior.ImplantPeriodicUpdate);
			}
		}

		public bool IsBehaviorAttached(IBodyPartBehavior behavior)
		{
			return attachedHealth != null && IsEnabled(behavior) && attachedBehaviors.Contains(behavior);
		}

		public void OnBodyPartDamage(BodyPartDamageData data)
		{
			Initialize();
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				if (IsEnabled(behavior)) InvokeBehavior(behavior, () => behavior.OnTakeDamage(data));
			}
		}

		public void OnBodyPartAdded(LivingHealthMasterBase livingHealth)
		{
			Initialize();
			if (livingHealth == null || attachedHealth == livingHealth) return;
			if (attachedHealth != null) OnRemovedFromBody(attachedHealth);
			attachedHealth = livingHealth;
			attachedBehaviors.AddRange(Behaviors.Where(IsEnabled).Distinct());

			// Bind every provider before running any provider's derived initialization.
			foreach (IBodyPartBehavior behavior in attachedBehaviors)
			{
				if (behavior is IBodyPartComponentBase provider)
					InvokeBehavior(behavior, () => provider.BindSystem(livingHealth));
			}
			foreach (IBodyPartBehavior behavior in attachedBehaviors)
			{
				if (behavior is IBodyPartComponentBase)
					InvokeBehavior(behavior, () => behavior.OnAddedToBody(livingHealth));
			}
			SetUpSystems();
			foreach (IBodyPartBehavior behavior in attachedBehaviors)
			{
				if (behavior is not IBodyPartComponentBase)
					InvokeBehavior(behavior, () => behavior.OnAddedToBody(livingHealth));
			}
		}

		public void SetUpSystems()
		{
			if (attachedHealth == null || systemsSetUp) return;
			systemsSetUp = true;
			foreach (IBodyPartBehavior behavior in attachedBehaviors)
				InvokeBehavior(behavior, behavior.SetUpSystems);
		}

		public void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			if (attachedHealth == null || attachedHealth != livingHealth) return;
			attachedHealth = null;
			foreach (IBodyPartBehavior behavior in attachedBehaviors)
			{
				if (behavior is not IBodyPartComponentBase)
					InvokeBehavior(behavior, () => behavior.OnRemovedFromBody(livingHealth, source));
			}
			// Consumers and systems can still use provider associations during removal.
			var part = GetComponent<BodyPart>();
			if (part != null) part.NotifySystemsRemoved(livingHealth);
			foreach (IBodyPartBehavior behavior in attachedBehaviors)
			{
				if (behavior is IBodyPartComponentBase)
					InvokeBehavior(behavior, () => behavior.OnRemovedFromBody(livingHealth, source));
			}
			attachedBehaviors.Clear();
			systemsSetUp = false;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			Initialize();
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				if (IsEnabled(behavior) && behavior is IServerInventoryMove moved)
				{
					InvokeBehavior(behavior, () => moved.OnInventoryMoveServer(info));
				}
			}
		}

		public void OnEmp(int strength)
		{
			Initialize();
			foreach (IBodyPartBehavior behavior in Behaviors)
			{
				if (IsEnabled(behavior) && behavior is IEmpAble emp)
					InvokeBehavior(behavior, () => emp.OnEmp(strength));
			}
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			Initialize();
			ticket = $"body-part-behaviors/{netId}/{ComponentIndex}";
			if (!HasNetworkState) return;
			RpcMessageQueue.Instance.RegisterHandler(ticket, CaptureForConnection);
			UpdateManager.Add(PublishNetworkState, 0.1f);
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			Initialize();
			clientNetworkGeneration++;
			ticket = $"body-part-behaviors/{netId}/{ComponentIndex}";
			// Host clients share their behavior instances with the server.
			if (isServer)
			{
				foreach (IClientSynchronisedEffect effect in Enumerable.OfType<IClientSynchronisedEffect>(Behaviors))
				{
					effect.SyncOnPlayer(effect.OnPlayerID, effect.OnPlayerID);
				}
				return;
			}

			RpcMessageQueue.Instance.Subscribe(ticket + "/events", ApplyEvent);
			if (!HasNetworkState) return;
			RpcMessageQueue.Instance.Subscribe(ticket, ApplySnapshot);
			RequestInitialState(clientNetworkGeneration).Forget();
		}

		public override void OnStopClient()
		{
			clientNetworkGeneration++;
			if (ClientSynchronisedEffectsManager.Instance != null)
			{
				foreach (IClientSynchronisedEffect effect in Enumerable.OfType<IClientSynchronisedEffect>(Behaviors))
				{
					effect.ImplementationSyncOnPlayer(effect.OnPlayerID, NetId.Empty);
				}
			}
			StopNetworking();
			base.OnStopClient();
		}

		public override void OnStopServer()
		{
			StopNetworking();
			base.OnStopServer();
		}

		private void StopNetworking()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PublishNetworkState);
			if (ticket == null || RpcMessageQueue.Instance == null) return;
			RpcMessageQueue.Instance.UnregisterHandler(ticket);
			RpcMessageQueue.Instance.Unsubscribe(ticket, ApplySnapshot);
			RpcMessageQueue.Instance.Unsubscribe(ticket + "/events", ApplyEvent);
			lastSentStates.Clear();
		}

		private async UniTaskVoid RequestInitialState(int generation)
		{
			QueuedMessage result = await RpcMessageQueue.Instance.Queue(ticket);
			if (this != null && generation == clientNetworkGeneration && result.Status == MessageStatus.Success)
				ApplySnapshot(result.ValueFromJson);
		}

		private string CaptureForConnection(NetworkConnection connection)
		{
			if (connection == null || !netIdentity.observers.ContainsKey(connection.connectionId))
			{
				throw new InvalidOperationException("The requested body part is not observed by this connection.");
			}
			return CaptureSnapshot(connection == netIdentity.connectionToClient);
		}

		private string CaptureSnapshot(bool isOwner)
		{
			var snapshot = new Snapshot();
			for (var i = 0; i < Behaviors.Count; i++)
			{
				if (Behaviors[i] is not BodyPartBehavior behavior) continue;
				if (behavior.OwnerOnlyNetworkState && !isOwner) continue;
				string state = behavior.CaptureNetworkState();
				if (state != null) snapshot.States.Add(new BehaviorState { Index = i, Json = state });
			}

			return JsonUtility.ToJson(snapshot);
		}

		private void PublishNetworkState()
		{
			string observerState = null;
			string ownerState = null;
			foreach (NetworkConnectionToClient observer in netIdentity.observers.Values)
			{
				if (observer is LocalConnectionToClient) continue;
				string json = observer == netIdentity.connectionToClient ? ownerState ??= CaptureSnapshot(true) : observerState ??= CaptureSnapshot(false);
				if (lastSentStates.TryGetValue(observer.connectionId, out string previous) && previous == json) continue;
				lastSentStates[observer.connectionId] = json;
				RpcMessageQueue.Instance.Publish(ticket, json, observer);
			}
		}

		private void ApplySnapshot(string json)
		{
			if (this == null) return;
			var snapshot = JsonUtility.FromJson<Snapshot>(json);
			foreach (BehaviorState state in snapshot.States)
			{
				if (state.Index >= 0 && state.Index < Behaviors.Count &&
				    Behaviors[state.Index] is BodyPartBehavior behavior)
				{
					behavior.ApplyNetworkState(state.Json);
				}
			}
		}

		public void SendEvent(BodyPartBehavior behavior, string eventName, string payload, NetworkConnection recipient)
		{
			if (!isServer || recipient == null) return;
			if (recipient is LocalConnectionToClient)
			{
				behavior.OnNetworkEvent(eventName, payload);
				return;
			}

			var message = new BehaviorEvent
			{
				Index = Behaviors.IndexOf(behavior),
				Name = eventName, Payload = payload
			};
			RpcMessageQueue.Instance.Publish(ticket + "/events", JsonUtility.ToJson(message), recipient);
		}

		private void ApplyEvent(string json)
		{
			var message = JsonUtility.FromJson<BehaviorEvent>(json);
			if (message.Index >= 0 && message.Index < Behaviors.Count && Behaviors[message.Index] is BodyPartBehavior behavior)
			{
				behavior.OnNetworkEvent(message.Name, message.Payload);
			}
		}

		[Serializable]
		private class Snapshot
		{
			public List<BehaviorState> States = new();
		}

		[Serializable]
		private class BehaviorState
		{
			public int Index;
			public string Json;
		}

		[Serializable]
		private class BehaviorEvent
		{
			public int Index;
			public string Name;
			public string Payload;
		}
	}
}
