using System;
using System.Collections.Generic;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.Traversal;
using US13.Tilemaps.Behaviours.Layers;

namespace US13.Mobs.BrainAI
{
	/// <summary>Working data shared by one brain's subgraphs, never by different mobs.</summary>
	public sealed class MobBehaviorContext
	{
		public readonly BrainMobAI AI;
		public GameObject Target;
		public bool TargetWasObject;
		public GameObject DesiredTarget;
		public Matrix TargetMatrix;
		public Vector3Int TargetCell;
		public List<Vector3Int> Path;
		public float TargetRange = 1.5f;
		public int Generation { get; private set; }
		public float RecentDamage;
		private readonly List<IMobBehaviorContextExtension> extensions = new();
		private readonly Dictionary<Type, object> extensionCache = new();
		private object movementOwner;
		private bool bound;
		public LivingHealthMasterBase Health { get; private set; }
		public MobTraversal Traversal => AI.Traversal;

		public MobBehaviorContext(BrainMobAI ai)
		{
			AI = ai ?? throw new ArgumentNullException(nameof(ai));
			if (ai.ContextExtensions == null) throw new InvalidOperationException("The brain's context extension list is missing.");
			var types = new HashSet<Type>();
			foreach (var selection in ai.ContextExtensions)
			{
				if (selection == null) throw new InvalidOperationException("The brain has a null context extension selection.");
				if (!types.Add(selection.GetType())) throw new InvalidOperationException($"Duplicate context extension {selection.GetType().FullName}");
				var runtime = selection.CreateRuntime();
				if (runtime == null || ReferenceEquals(runtime, selection) || runtime.GetType() != selection.GetType())
					throw new InvalidOperationException($"Context extension {selection.GetType().FullName} must create a fresh runtime instance of its own type.");
				extensions.Add(runtime);
			}
		}

		public bool TryGetExtension<T>(out T extension) where T : class
		{
			if (!extensionCache.TryGetValue(typeof(T), out var result))
			{
				foreach (var candidate in extensions)
				{
					if (candidate is not T match) continue;
					if (result != null) throw new InvalidOperationException($"Ambiguous context extension lookup for {typeof(T).FullName}");
					result = match;
				}
				extensionCache.Add(typeof(T), result);
			}
			extension = result as T;
			return extension != null;
		}

		public void Bind(LivingHealthMasterBase health)
		{
			if (health == null) throw new ArgumentNullException(nameof(health));
			if (bound && Health == health) return;
			if (bound) Unbind();
			Health = health;
			bound = true;
			try
			{
				foreach (var extension in extensions) extension.Bind(this, health);
			}
			catch { Unbind(); throw; }
		}
		public void BeginRun()
		{
			Generation++;
			ClearTarget();
			DesiredTarget = null;
			foreach (var extension in extensions) extension.BeginRun();
		}
		public void EndRun()
		{
			Generation++;
			if (movementOwner != null) ReleaseMovement(movementOwner);
			ClearTarget();
			DesiredTarget = null;
			foreach (var extension in extensions) extension.EndRun();
		}
		public bool TryAcquireMovement(object owner)
		{
			if (movementOwner != null && !ReferenceEquals(movementOwner, owner)) return false;
			movementOwner = owner;
			return true;
		}
		public void ReleaseMovement(object owner)
		{
			if (!ReferenceEquals(movementOwner, owner)) return;
			movementOwner = null;
			if (Traversal != null && Traversal.IsTraversing) Traversal.CancelCurrentTraversalImmediately();
		}
		public void Unbind()
		{
			if (!bound) return;
			Generation++;
			if (movementOwner != null) ReleaseMovement(movementOwner);
			ClearTarget();
			DesiredTarget = null;
			try
			{
				List<Exception> errors = null;
				for (int i = extensions.Count - 1; i >= 0; i--)
				{
					try { extensions[i].Unbind(); }
					catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
				}
				if (errors != null) throw new AggregateException("Unable to unbind mob context extensions.", errors);
			}
			finally { Health = null; bound = false; }
		}
		public void ClearTarget()
		{
			Target = null;
			TargetWasObject = false;
			TargetMatrix = null;
			TargetCell = Vector3Int.zero;
			Path = null;
		}
		public bool CanFollowTarget
		{
			get
			{
				foreach (var extension in extensions)
					if (extension is IMobFollowPolicy policy && !policy.CanFollowTarget) return false;
				return true;
			}
		}

		public void NotifyMovementFailed()
		{
			ClearTarget();
			foreach (var extension in extensions)
				if (extension is IMobMovementFailureHandler handler) handler.OnMovementFailed(this);
		}
	}
}
