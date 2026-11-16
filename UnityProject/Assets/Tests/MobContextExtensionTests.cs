using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using NUnit.Framework;
using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using US13.Core.Editor;
using US13.HealthV2.Living;
using BodyPart = US13.HealthV2.Living.BodyParts.BodyPart;
using US13.Items.Implants.Organs;
using US13.Mobs.BrainAI;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.Tasks;
using Object = UnityEngine.Object;

namespace Tests
{
	public class MobContextExtensionTests
	{
		[Serializable]
		private sealed class ProbeExtension : MobBehaviorContextExtension, IMobFollowPolicy, IMobMovementFailureHandler
		{
			public bool AllowFollow = true;
			public bool SawClearedTarget;
			public bool ThrowOnUnbind;
			public List<string> Events = new();
			public override IMobBehaviorContextExtension CreateRuntime() => new ProbeExtension();
			public bool CanFollowTarget => AllowFollow;
			public override void Bind(MobBehaviorContext context, LivingHealthMasterBase health)
			{
				base.Bind(context, health);
				Assert.That(context.Health, Is.SameAs(health));
				Events.Add("bind");
			}
			public override void BeginRun() => Events.Add($"begin:{Context.Generation}");
			public override void EndRun() => Events.Add($"end:{Context.Generation}");
			public override void Unbind()
			{
				Events.Add("unbind");
				base.Unbind();
				if (ThrowOnUnbind) throw new InvalidOperationException("test cleanup failure");
			}
			public void OnMovementFailed(MobBehaviorContext context) => SawClearedTarget = context.Target == null;
		}

		[Serializable]
		private sealed class InvalidFactoryExtension : MobBehaviorContextExtension
		{
			public bool ReturnTemplate;
			public override IMobBehaviorContextExtension CreateRuntime() => ReturnTemplate ? this : null;
		}

		private sealed class CountingTask : MobTask
		{
			public int Completed, Cancelled;
			public override bool FindTarget(MobBehaviorContext context, int radius) => true;
			public override bool IsValid(MobBehaviorContext context) => true;
			public override float Begin(MobBehaviorContext context) => 2;
			public override void Complete(MobBehaviorContext context) => Completed++;
			public override void Cancel() => Cancelled++;
		}

		private static MobBehaviorContext Create(params IMobBehaviorContextExtension[] selections) =>
			new(new BrainMobAI { ContextExtensions = selections.ToList() });

		[Test]
		public void TemplatesProduceIndependentRuntimeDataAndSupportInterfaceLookup()
		{
			var template = new ArialContextExtension { ActivityTicks = 30, IsStalking = false };
			var first = Create(template);
			var second = Create(template);
			Assert.That(first.TryGetExtension<ArialContextExtension>(out var a), Is.True);
			Assert.That(second.TryGetExtension<ArialContextExtension>(out var b), Is.True);
			Assert.That(first.TryGetExtension<IMobFollowPolicy>(out var policy), Is.True);
			Assert.That(policy, Is.SameAs(a));
			Assert.That(a, Is.Not.SameAs(template).And.Not.SameAs(b));
			Assert.That(a.ActivityTicks, Is.Zero, "Runtime state must not be copied from a template.");
			Assert.That(a.IsStalking, Is.True);
			a.ActivityTicks = 17;
			Assert.That(b.ActivityTicks, Is.Zero);
			Assert.That(first.TryGetExtension<SlimeContextExtension>(out var absent), Is.False);
			Assert.That(absent, Is.Null);
		}

		[Test]
		public void InvalidSelectionsAndAmbiguousInterfaceLookupsAreRejected()
		{
			Assert.Throws<InvalidOperationException>(() => Create(new IMobBehaviorContextExtension[] { null }));
			Assert.Throws<InvalidOperationException>(() => Create(new TaskContextExtension(), new TaskContextExtension()));
			Assert.Throws<InvalidOperationException>(() => new MobBehaviorContext(new BrainMobAI { ContextExtensions = null }));
			Assert.Throws<InvalidOperationException>(() => Create(new InvalidFactoryExtension()));
			Assert.Throws<InvalidOperationException>(() => Create(new InvalidFactoryExtension { ReturnTemplate = true }));
			var context = Create(new ArialContextExtension(), new ProbeExtension());
			Assert.Throws<InvalidOperationException>(() => context.TryGetExtension<IMobFollowPolicy>(out _));
		}

		[Test]
		public void FollowPoliciesComposeAndMovementHandlersDoNotRequireASpecies()
		{
			Assert.That(Create().CanFollowTarget, Is.True);
			var context = Create(new ArialContextExtension(), new ProbeExtension());
			context.TryGetExtension<ArialContextExtension>(out var arial);
			context.TryGetExtension<ProbeExtension>(out var probe);
			Assert.That(context.CanFollowTarget, Is.True);
			arial.IsStalking = false;
			Assert.That(context.CanFollowTarget, Is.False);
			arial.IsStalking = true; probe.AllowFollow = false;
			Assert.That(context.CanFollowTarget, Is.False);
			probe.AllowFollow = true;
			Assert.That(context.CanFollowTarget, Is.True);
			var target = new GameObject("movement target");
			try
			{
				context.Target = target;
				context.NotifyMovementFailed();
				Assert.That(probe.SawClearedTarget, Is.True);
			}
			finally { Object.DestroyImmediate(target); }
		}

		[Test]
		public void TaskHesitationAndSearchStateLiveInTheSelectedExtension()
		{
			var ai = new BrainMobAI { ContextExtensions = new() { new TaskContextExtension() } };
			ai.Settings.Hesitation = new Vector2Int(1, 1); ai.Settings.WanderInterval = 2;
			var context = new MobBehaviorContext(ai);
			context.TryGetExtension<TaskContextExtension>(out var tasks);
			context.NotifyMovementFailed();
			Assert.That(tasks.NextSearchTime, Is.EqualTo(Time.time + 2).Within(0.01f));
			tasks.NearbySearch = true;
			context.BeginRun();
			Assert.That(tasks.NextSearchTime, Is.Zero);
			Assert.That(tasks.NearbySearch, Is.False);
			Assert.That(new MobTaskExecution(Create(), 0).Status, Is.EqualTo(Node.Status.Failure));
		}

		[Test]
		public void FamiliarityCopiesIndependentlyAndPersistentStateSurvivesRestarts()
		{
			var parent = Create(new SlimeContextExtension(), new ArialContextExtension());
			var offspring = Create(new SlimeContextExtension());
			parent.TryGetExtension<SlimeContextExtension>(out var a);
			offspring.TryGetExtension<SlimeContextExtension>(out var b);
			parent.TryGetExtension<ArialContextExtension>(out var arial);
			var player = new GameObject("familiar player");
			try
			{
				a.GoodPlayers[player] = 47; arial.ActivityTicks = 22; arial.IsStalking = false;
				b.CopyFamiliarityFrom(a);
				Assert.That(b.GoodPlayers[player], Is.EqualTo(47));
				b.GoodPlayers[player]++;
				Assert.That(a.GoodPlayers[player], Is.EqualTo(47));
				parent.EndRun(); parent.BeginRun();
				Assert.That(a.GoodPlayers[player], Is.EqualTo(47));
				Assert.That(arial.ActivityTicks, Is.EqualTo(22));
				Assert.That(arial.IsStalking, Is.False);
			}
			finally { Object.DestroyImmediate(player); }
		}

		[Test]
		public void BindingReleasesOldOrganReferencesWithoutDiscardingBrainMemory()
		{
			var root = new GameObject("context lifecycle test"); root.SetActive(false);
			MobBehaviorContext context = null;
			try
			{
				var firstHealth = AddHealth(root, "first body");
				var secondHealth = AddHealth(root, "second body");
				var brain = Child(root, "brain"); brain.AddComponent<NetworkIdentity>();
				var host = brain.AddComponent<BodyPartBehaviors>();
				var ai = new BrainMobAI { ContextExtensions = new() { new SlimeContextExtension(), new ArialContextExtension(), new TaskContextExtension(), new ProbeExtension() } };
				var core = new SlimeCore(); host.Behaviors.Add(ai); host.Behaviors.Add(core); host.Initialize();
				var mouth = Child(root, "mouth"); mouth.AddComponent<NetworkIdentity>();
				var part = mouth.AddComponent<BodyPart>();
				var eater = new SlimeEat(); mouth.AddComponent<BodyPartBehaviors>().Behaviors.Add(eater);
				firstHealth.BodyPartList.Add(part);
				context = ai.Context;
				context.Bind(firstHealth); context.Bind(firstHealth);
				context.TryGetExtension<SlimeContextExtension>(out var slime);
				context.TryGetExtension<ArialContextExtension>(out var arial);
				context.TryGetExtension<TaskContextExtension>(out var tasks);
				context.TryGetExtension<ProbeExtension>(out var probe);
				Assert.That(slime.SlimeCore, Is.SameAs(core));
				Assert.That(slime.SlimeEat, Is.SameAs(eater));
				slime.GoodPlayers[brain] = 8; arial.ActivityTicks = 9; arial.IsStalking = false;
				context.BeginRun(); context.EndRun();
				Assert.That(probe.Events, Is.EqualTo(new[] { "bind", "begin:1", "end:2" }));
				context.Unbind(); context.Unbind();
				Assert.That(slime.SlimeCore, Is.Null); Assert.That(slime.SlimeEat, Is.Null);
				Assert.That(tasks.Task, Is.Null); Assert.That(context.Health, Is.Null);
				context.Bind(secondHealth);
				Assert.That(context.Health, Is.SameAs(secondHealth));
				Assert.That(slime.SlimeEat, Is.Null, "The new body has no eating organ.");
				Assert.That(slime.GoodPlayers[brain], Is.EqualTo(8));
				Assert.That(arial.ActivityTicks, Is.EqualTo(9)); Assert.That(arial.IsStalking, Is.False);
				probe.ThrowOnUnbind = true;
				Assert.Throws<AggregateException>(() => context.Unbind());
				Assert.That(context.Health, Is.Null); Assert.That(slime.SlimeCore, Is.Null);
			}
			finally { context?.Unbind(); Object.DestroyImmediate(root); }
		}

		[Test]
		public void SavedBrainsProvideTheirGraphsRequiredExtensions()
		{
			int count = 0;
			foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Items/Implants/Slime", "Assets/Prefabs/Items/Implants/Organs/Special Organs", "Assets/Prefabs/Mobs/Bot/Resources", "Assets/Prefabs/Mobs/Living/Alien/Arial" }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				foreach (var host in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<BodyPartBehaviors>(true))
					foreach (var ai in host.Behaviors.OfType<BrainMobAI>())
					{
						MobBehaviorGraphTools.ValidateContextExtensions(ai, path);
						MobBehaviorGraphTools.ValidateSettings(ai, path);
						count++;
					}
			}
			Assert.That(count, Is.GreaterThanOrEqualTo(28));
		}

		[TestCase("SimpleBot", typeof(TaskContextExtension))]
		[TestCase("Slime", typeof(SlimeContextExtension))]
		[TestCase("Arial", typeof(ArialContextExtension))]
		public void GraphValidationRejectsMissingRequiredExtensions(string graphName, Type extensionType)
		{
			var graph = AssetDatabase.LoadAllAssetsAtPath($"{MobBehaviorGraphTools.GraphFolder}/{graphName}.asset").OfType<BehaviorGraph>().Single();
			var ai = new BrainMobAI { Graph = graph };
			var error = Assert.Throws<InvalidOperationException>(() => MobBehaviorGraphTools.ValidateContextExtensions(ai, "test brain"));
			Assert.That(error.Message, Does.Contain("test brain").And.Contain(extensionType.FullName));
			ai.ContextExtensions.Add((IMobBehaviorContextExtension)Activator.CreateInstance(extensionType));
			Assert.DoesNotThrow(() => MobBehaviorGraphTools.ValidateContextExtensions(ai, "test brain"));
		}

		[Test]
		public void UnbindingInvalidatesTaskEffectsAndCancellationRemainsIdempotent()
		{
			var root = new GameObject("task detach test"); root.SetActive(false);
			var context = Create(new TaskContextExtension());
			try
			{
				context.Bind(AddHealth(root, "body"));
				context.TryGetExtension<TaskContextExtension>(out var tasks);
				var task = new CountingTask(); tasks.Task = task;
				var execution = new MobTaskExecution(context, 0);
				context.Unbind();
				Assert.That(execution.Tick(5), Is.EqualTo(Node.Status.Failure));
				execution.Cancel(); execution.Cancel();
				Assert.That(task.Completed, Is.Zero);
				Assert.That(task.Cancelled, Is.EqualTo(1));
			}
			finally { context.Unbind(); Object.DestroyImmediate(root); }
		}

		private static GameObject Child(GameObject root, string name)
		{
			var child = new GameObject(name); child.transform.SetParent(root.transform); return child;
		}
		private static LivingHealthMasterBase AddHealth(GameObject root, string name)
		{
			var body = Child(root, name); body.AddComponent<NetworkIdentity>();
			return body.AddComponent<BrainMobBehaviorTests.SpawnGuardHealth>();
		}
	}
}
