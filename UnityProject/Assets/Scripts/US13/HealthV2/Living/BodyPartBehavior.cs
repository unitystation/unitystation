using System;
using System.Collections;
using System.Threading;
using Mirror;
using UnityEngine;
using US13.Core.Admin.Logs;
using US13.HealthV2.Living.BodyParts;
using US13.Systems.Explosions;

namespace US13.HealthV2.Living
{
	/// <summary>Shared lifecycle and context for managed body part behaviors.</summary>
	[Serializable]
	public abstract class BodyPartBehavior : IBodyPartBehavior, IEmpAble
	{
		public bool Enabled = true;
		public bool OwnerOnlyNetworkState;
		public BodyPart RelatedPart { get; set; }
		public LivingHealthMasterBase LivingHealthMaster => RelatedPart?.HealthMaster;
		public BodyPartBehaviors Owner { get; private set; }
		public GameObject gameObject => Owner.gameObject;
		public Transform transform => Owner.transform;
		public string name => gameObject.name;
		protected bool isServer => netIdentity != null && netIdentity.isServer;
		protected bool isClient => netIdentity != null && netIdentity.isClient;
		public NetworkIdentity netIdentity => Owner == null ? null : Owner.netIdentity ?? Owner.GetComponentInParent<NetworkIdentity>(true);
		public uint netId => netIdentity == null ? 0 : netIdentity.netId;
		protected CancellationToken destroyCancellationToken => Owner.destroyCancellationToken;

		public void Initialize(BodyPartBehaviors owner)
		{
			Owner = owner;
			RelatedPart = owner.GetComponentInParent<BodyPart>(true);
		}

		public virtual void OnInitialize() { }
		public virtual void OnBehaviorStarted() { }
		public virtual void OnBehaviorEnabled() { }
		public virtual void OnBehaviorDisabled() { }
		public virtual void OnBehaviorDestroyed() { }
		public virtual void ImplantPeriodicUpdate() { }
		public virtual void OnAddedToBody(LivingHealthMasterBase livingHealth) { }
		public virtual void SetUpSystems() { }
		public virtual void OnTakeDamage(BodyPartDamageData data) { }
		public virtual void OnEmp(int strength) { }
		public virtual string CaptureNetworkState() => null;
		public virtual void ApplyNetworkState(string json) { }
		public virtual void OnNetworkEvent(string eventName, string payload) { }

		public virtual void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			if (source == null) return;
			if (livingHealth.playerScript.Mind != null && livingHealth.playerScript.Mind.NonImportantMind == false)
			{
				AdminLogsManager.AddNewLog(source,
					$"{Util.SweetExtensions.ExpensiveName(gameObject)} has been removed from {Util.SweetExtensions.ExpensiveName(livingHealth.gameObject)}'s body.",
					LogCategory.MobDamage);
			}
		}

		// Unity services belong to the owning component; behaviors themselves are plain objects.
		public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
		public T GetCachedComponent<T>(bool includeDisabled = true) where T : Component => Util.SweetExtensions.GetCachedComponent<T>(gameObject, includeDisabled);
		public T GetBodyPartBehavior<T>() where T : class => gameObject.GetBodyPartBehavior<T>();
		public bool TryGetBodyPartBehavior<T>(out T behavior) where T : class => gameObject.TryGetBodyPartBehavior(out behavior);
		public T[] GetBodyPartBehaviorsInChildren<T>(bool includeInactive = false) where T : class => gameObject.GetBodyPartBehaviorsInChildren<T>(includeInactive);
		public T[] GetComponentsInChildren<T>(bool includeInactive = false) => gameObject.GetComponentsInChildren<T>(includeInactive);
		protected Coroutine StartCoroutine(IEnumerator routine) => Owner.StartCoroutine(routine);
		protected void StopCoroutine(Coroutine routine) => Owner.StopCoroutine(routine);
	}
}
