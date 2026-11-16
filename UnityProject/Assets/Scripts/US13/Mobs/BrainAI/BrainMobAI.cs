using System;
using System.Collections.Generic;
using Logs;
using Unity.Behavior;
using UnityEngine;
using US13.Core.Attributes;
using US13.Core.Utils;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Mobs.BrainAI.States.SimpleBot;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.Traversal;

namespace US13.Mobs.BrainAI
{
	[Serializable]
	public class BrainMobAI : BodyPartBehavior, ICanBeEmaggedMob
	{
		public BehaviorGraph Graph;
		public MobBehaviorSettings Settings = new();
		[SerializeReference, SelectImplementation(typeof(IMobBehaviorContextExtension))]
		public List<IMobBehaviorContextExtension> ContextExtensions = new();
		public bool DEBUGoverride;
		[SerializeField] private bool isEmagged;
		[SerializeField] private bool isPerformingTask;
		[NonSerialized] public Brain Brain;
		[NonSerialized] public CommonComponents Body;
		[NonSerialized] private BehaviorGraphAgent agent;
		[NonSerialized] private bool running;
		[NonSerialized] private bool registered;
		[NonSerialized] private bool available;
		[NonSerialized] private LivingHealthMasterBase attachedHealth;
		public MobBehaviorContext Context { get; private set; }
		public MobTraversal Traversal => Brain?.Traversal;
		public bool IsEmagged => isEmagged;
		public bool IsPerformingTask => isPerformingTask;
		public bool IsControlledByPlayer => !DEBUGoverride && Brain?.PossessingMind != null &&
			Brain.PossessingMind.IsActivelyControlling(Body != null ? Body.gameObject : null);
		public bool CanRun => Context != null && Context.Health != null && Context.Health == attachedHealth && CustomNetworkManager.IsServer && available && Enabled &&
			Owner != null && Owner.isActiveAndEnabled && attachedHealth != null && Body != null &&
			!attachedHealth.IsDead && !IsControlledByPlayer && Body.UniversalObjectPhysics != null &&
			Body.UniversalObjectPhysics.IsVisible && attachedHealth.RegisterTile?.Matrix?.PresentPlayers?.Count > 0;

		public override void OnInitialize()
		{
			Brain = GetBodyPartBehavior<Brain>();
			Context = new MobBehaviorContext(this);
		}

		public override void OnBehaviorEnabled()
		{
			available = true;
			if (registered) return;
			UpdateManager.Add(CallbackType.UPDATE, Tick);
			registered = true;
		}

		public override void OnBehaviorStarted() => OnBehaviorEnabled();
		public void SetPerformingTask(bool value) => isPerformingTask = value;

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			if (attachedHealth == livingHealth) return;
			if (attachedHealth != null) OnRemovedFromBody(attachedHealth);
			attachedHealth = livingHealth;
			Body = livingHealth.GetComponent<CommonComponents>();
			Brain ??= GetBodyPartBehavior<Brain>();
			Context ??= new MobBehaviorContext(this);
			Context.Bind(livingHealth);
			livingHealth.OnTakeDamageType += OnDamage;
			livingHealth.OnDeath += Stop;
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			if (attachedHealth != livingHealth) return;
			Stop();
			livingHealth.OnTakeDamageType -= OnDamage;
			livingHealth.OnDeath -= Stop;
			try { Context?.Unbind(); }
			finally { attachedHealth = null; Body = null; }
		}

		private void OnDamage(DamageType type, GameObject source, float damage)
		{
			if (CustomNetworkManager.IsServer && Context != null) Context.RecentDamage += Mathf.Max(0, damage);
		}

		private void Tick()
		{
			if (!CanRun || Graph == null) { Stop(); return; }
			if (agent == null)
			{
				// Init clones the asset. The organ owns ticking, so Unity's automatic Start/Update stay disabled.
				agent = gameObject.AddComponent<BehaviorGraphAgent>();
				agent.enabled = false;
				agent.Graph = Graph;
				agent.Init();
				if (agent.Graph == null)
				{
					Loggy.Error($"Invalid mob behavior graph on {name}", Category.Mobs);
					OnBehaviorDisabled();
					return;
				}
			}
			if (!running)
			{
				Context.BeginRun();
				running = true;
				agent.Graph.Start();
			}
			agent.Graph.Tick();
		}

		private void Stop()
		{
			if (!running) return;
			running = false;
			try { Context.EndRun(); }
			finally
			{
				try { if (agent != null) agent.Graph?.End(); }
				finally { SetPerformingTask(false); }
			}
		}

		public override void OnBehaviorDisabled()
		{
			available = false;
			Stop();
			if (!registered) return;
			UpdateManager.Remove(CallbackType.UPDATE, Tick);
			registered = false;
		}

		public override void OnBehaviorDestroyed()
		{
			OnBehaviorDisabled();
			if (attachedHealth != null) OnRemovedFromBody(attachedHealth);
			if (agent != null)
			{
				var instance = agent.Graph;
				agent.Graph = null; // Release nodes and callbacks before destroying the runtime clone.
				if (instance != null) UnityEngine.Object.Destroy(instance);
				UnityEngine.Object.Destroy(agent);
			}
			agent = null;
		}

		public void EmagMob()
		{
			if (!CustomNetworkManager.IsServer) return;
			Stop();
			isEmagged = true;
		}

		[Serializable]
		private sealed class NetworkState
		{
			public bool IsEmagged;
			public bool IsPerformingTask;
		}

		public override string CaptureNetworkState() => JsonUtility.ToJson(new NetworkState
		{
			IsEmagged = isEmagged, IsPerformingTask = isPerformingTask
		});

		public override void ApplyNetworkState(string json)
		{
			var state = JsonUtility.FromJson<NetworkState>(json);
			if (state == null) return;
			isEmagged = state.IsEmagged;
			isPerformingTask = state.IsPerformingTask;
		}
	}
}
