using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using US13.Core.Editor;
using US13.Core.Lifecycle;
using US13.Core.Utils;
using US13.HealthV2.Living;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Mobs.BrainAI;
using US13.Mobs.BrainAI.Actions;
using US13.Mobs.BrainAI.Actions.Arial;
using US13.Mobs.BrainAI.Actions.Slimes;
using US13.Mobs.BrainAI.States.SimpleBot;
using US13.Mobs.BrainAI.Tasks;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Player;
using US13.Systems.Inventory;
using Object = UnityEngine.Object;

namespace Tests
{
	public class BrainMobBehaviorTests
	{
		public sealed class SpawnGuardHealth : LivingHealthMasterBase
		{
			public SpawnResult NextResult;
			public int Attempts;
			protected override SpawnResult SpawnBodyPartPrefab(GameObject prefab) { Attempts++; return NextResult; }
		}

		[Test]
		public void MissingSavedBodyPartFailsBeforeSpawningAnyParts()
		{
			var owner = new GameObject("spawn validation test"); owner.SetActive(false);
			var prefab = new GameObject("valid prefab"); prefab.SetActive(false);
			var species = ScriptableObject.CreateInstance<PlayerHealthData>(); species.name = "test species";
			try
			{
				owner.AddComponent<NetworkIdentity>();
				var health = owner.AddComponent<SpawnGuardHealth>(); health.InitialSpecies = species;
				var parts = new ObjectList(); parts.Elements.Add(prefab); parts.Elements.Add(null);
				var error = Assert.Throws<InvalidOperationException>(() => health.InstantiateAndSetUp(parts));
				Assert.That(error.Message, Does.Contain("test species").And.Contain("entry 1"));
				Assert.That(health.Attempts, Is.Zero);
			}
			finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(prefab); Object.DestroyImmediate(species); }
		}

		[TestCase(false)][TestCase(true)]
		public void FailedBodyPartSpawnDoesNotDereferenceTheMissingObject(bool successfulWithNoObject)
		{
			var owner = new GameObject("spawn validation test"); owner.SetActive(false);
			var prefab = new GameObject("test part"); prefab.SetActive(false);
			try
			{
				owner.AddComponent<NetworkIdentity>();
				var health = owner.AddComponent<SpawnGuardHealth>();
				health.BodyPartStorage = owner.AddComponent<ItemStorage>();
				health.NextResult = successfulWithNoObject ? SpawnResult.Single(null, null) : SpawnResult.Fail(null);
				var parts = new ObjectList(); parts.Elements.Add(prefab);
				var error = Assert.Throws<InvalidOperationException>(() => health.InstantiateAndSetUp(parts));
				Assert.That(error.Message, Does.Contain("test part").And.Contain("entry 0"));
				Assert.That(health.Attempts, Is.EqualTo(1));
			}
			finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(prefab); }
		}

		[Test]
		public void SpeciesAndEmbeddedBotControllerReferencesResolve() => MobBehaviorGraphTools.ValidateSpawnReferences();

		[Test]
		public void SavedSpeciesHeadReferenceResolves()
		{
			var species = AssetDatabase.LoadAssetAtPath<PlayerHealthData>("Assets/Prefabs/Player/Resources/BodyParts/SimpleBots/Simplebot.asset");
			Assert.That(species.Base.Head.Elements.Single(), Is.Not.Null,
				"The saved species reference must resolve; loading the prefab by path does not check its saved fileID.");
		}

		[TestCase("FloorBotBody")][TestCase("FireBotBody")]
		public void BotBodyKeepsItsSavedRootIdentifier(string name)
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Mobs/Bot/Resources/Bodies/{name}.prefab");
			Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long id), Is.True);
			Assert.That(id, Is.EqualTo(960852636360704351L), "Changing the root ID breaks species and inherited-prefab references.");
		}

		private sealed class RecordingTask : MobTask
		{
			public int Completed, Ticked, Cancelled;
			public bool Valid = true;
			public override bool FindTarget(MobBehaviorContext context, int radius) => true;
			public override bool IsValid(MobBehaviorContext context) => Valid;
			public override float Begin(MobBehaviorContext context) => 2;
			public override void Tick(MobBehaviorContext context, float elapsed) => Ticked++;
			public override void Complete(MobBehaviorContext context) => Completed++;
			public override void Cancel() => Cancelled++;
		}

		[Test]
		public void EndingAnActionPreventsItsDelayedEffects()
		{
			var task = new RecordingTask();
			var context = ContextWithTask(task);
			var execution = new MobTaskExecution(context, 10);
			Assert.That(execution.Tick(11), Is.EqualTo(Node.Status.Running));
			execution.Cancel(); execution.Cancel();
			Assert.That(execution.Tick(20), Is.EqualTo(Node.Status.Failure));
			Assert.That(task.Completed, Is.Zero);
			Assert.That(task.Ticked, Is.EqualTo(1));
			Assert.That(task.Cancelled, Is.EqualTo(1));
		}

		[Test]
		public void RestartingTheBrainInvalidatesOldTaskEffects()
		{
			var task = new RecordingTask();
			var context = ContextWithTask(task);
			var old = new MobTaskExecution(context, 10);
			context.EndRun(); context.BeginRun();
			Assert.That(old.Tick(20), Is.EqualTo(Node.Status.Failure));
			Assert.That(task.Completed, Is.Zero);
		}

		[Test]
		public void ATaskCompletesOnceAndRevalidatesItsTarget()
		{
			var task = new RecordingTask();
			var context = ContextWithTask(task);
			var execution = new MobTaskExecution(context, 10);
			Assert.That(execution.Tick(12), Is.EqualTo(Node.Status.Success));
			Assert.That(execution.Tick(20), Is.EqualTo(Node.Status.Success));
			Assert.That(task.Completed, Is.EqualTo(1));
			var invalid = new MobTaskExecution(context, 30);
			task.Valid = false;
			Assert.That(invalid.Tick(40), Is.EqualTo(Node.Status.Failure));
			Assert.That(task.Completed, Is.EqualTo(1));
		}

		private static MobBehaviorContext ContextWithTask(MobTask task)
		{
			var ai = new BrainMobAI { ContextExtensions = new() { new TaskContextExtension() } };
			var context = new MobBehaviorContext(ai);
			Assert.That(context.TryGetExtension<TaskContextExtension>(out var extension), Is.True);
			extension.Task = task;
			return context;
		}
		[TestCase("Wander")][TestCase("TaskTarget")][TestCase("TaskMovement")][TestCase("TaskEffect")]
		[TestCase("SimpleBot")][TestCase("BrainNPC")][TestCase("Slime")][TestCase("Arial")]
		[TestCase("ArialTarget")][TestCase("ArialActivity")][TestCase("ArialPrank")]
		public void GraphContainsValidAuthoringAndRuntimeData(string name)
		{
			var path = $"{MobBehaviorGraphTools.GraphFolder}/{name}.asset";
			MobBehaviorGraphTools.ValidateGraphTypes(path);
			var authoring = AssetDatabase.LoadMainAssetAtPath(path);
			Assert.That(authoring, Is.Not.Null, path);
			Assert.That(authoring.GetType().Name, Is.EqualTo("BehaviorAuthoringGraph"));
			foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
				Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(asset), Is.False, $"Missing type in {name}/{asset.name}");
			var graph = AssetDatabase.LoadAllAssetsAtPath(path).OfType<BehaviorGraph>().Single();
			var serialized = new SerializedObject(graph);
			Assert.That(serialized.FindProperty("Graphs").arraySize, Is.GreaterThan(0));
			Assert.That(serialized.FindProperty("Graphs").GetArrayElementAtIndex(0).FindPropertyRelative("Root").managedReferenceValue, Is.Not.Null);
			var actualActions = new HashSet<Type>();
			var visited = new HashSet<long>();
			var property = serialized.GetIterator();
			bool enterChildren = true;
			while (property.Next(enterChildren))
			{
				enterChildren = property.propertyType != SerializedPropertyType.ManagedReference || visited.Add(property.managedReferenceId);
				if (property.propertyType == SerializedPropertyType.ManagedReference && property.managedReferenceValue is BrainMobState action)
					actualActions.Add(action.GetType());
			}
			Assert.That(actualActions, Is.EquivalentTo(ExpectedActions(name)), "A rebuild must retain every custom action, including embedded subgraphs.");
		}

		private static Type[] ExpectedActions(string name) => name switch
		{
			"TaskTarget" => new[] { typeof(FindMobTaskTarget) },
			"TaskMovement" => new[] { typeof(MoveToMobTarget) },
			"TaskEffect" => new[] { typeof(PerformMobTask) },
			"Wander" or "BrainNPC" => new[] { typeof(WaitMobInterval), typeof(WanderMob) },
			"SimpleBot" => new[] { typeof(FindMobTaskTarget), typeof(MoveToMobTarget), typeof(PerformMobTask), typeof(WaitMobInterval), typeof(WanderMob) },
			"Slime" => new[] { typeof(SplitSlime), typeof(ReactToSlimeDamage), typeof(IsSlimeEating), typeof(SelectSlimeFood), typeof(EatSelectedFood), typeof(RandomMobStep), typeof(WaitMobInterval) },
			"ArialTarget" => new[] { typeof(ChooseArialTarget), typeof(WaitMobInterval) },
			"ArialPrank" => new[] { typeof(PreparePrankApproach), typeof(MoveToMobTarget), typeof(TeleportMobToStation), typeof(ThrowNearbyObject) },
			"ArialActivity" => new[] { typeof(FollowMobTarget), typeof(PreparePrankApproach), typeof(MoveToMobTarget), typeof(TeleportMobToStation), typeof(ThrowNearbyObject), typeof(WaitMobInterval), typeof(WanderMob) },
			"Arial" => new[] { typeof(ChooseArialTarget), typeof(FollowMobTarget), typeof(PreparePrankApproach), typeof(MoveToMobTarget), typeof(TeleportMobToStation), typeof(ThrowNearbyObject), typeof(WaitMobInterval), typeof(WanderMob) },
			_ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
		};

		[TestCase("RuntimeTypeString", "US13.Mobs.BrainAI.WanderMob, Assets")]
		[TestCase("IsPlaceholder", true)]
		public void ValidationRejectsObsoleteAuthoringTypesAndPlaceholders(string fieldName, object invalidValue)
		{
			var path = $"{MobBehaviorGraphTools.GraphFolder}/Wander.asset";
			var authoring = AssetDatabase.LoadMainAssetAtPath(path);
			// Mutate the cache in memory only, bypassing Unity's automatic repair callbacks.
			var models = (IEnumerable)authoring.GetType().GetProperty("NodeModelsInfo").GetValue(authoring);
			var model = models.Cast<object>().First(item => (string)item.GetType().GetField("Name").GetValue(item) == "Wander One Tile");
			var field = model.GetType().GetField(fieldName);
			var original = field.GetValue(model);
			try
			{
				field.SetValue(model, invalidValue);
				var error = Assert.Throws<InvalidOperationException>(() => MobBehaviorGraphTools.ValidateGraphTypes(path));
				Assert.That(error.Message, Does.Contain(path).And.Contain(fieldName));
			}
			finally { field.SetValue(model, original); }
		}

		[TestCase("SimpleBot")][TestCase("Slime")][TestCase("Arial")]
		public void AgentsCloneGraphsAndCanRepeatedlyStartAndStop(string name)
		{
			var source = AssetDatabase.LoadAllAssetsAtPath($"{MobBehaviorGraphTools.GraphFolder}/{name}.asset").OfType<BehaviorGraph>().Single();
			var firstObject = new GameObject("first graph owner");
			var secondObject = new GameObject("second graph owner");
			try
			{
				var first = firstObject.AddComponent<BehaviorGraphAgent>(); first.enabled = false;
				var second = secondObject.AddComponent<BehaviorGraphAgent>(); second.enabled = false;
				first.Graph = source; first.Init(); second.Graph = source; second.Init();
				Assert.That(first.Graph, Is.Not.SameAs(source));
				Assert.That(first.Graph, Is.Not.SameAs(second.Graph));
				Assert.That(first.Graph.BlackboardReference.GetVariable<GameObject>("Self", out var firstOwner), Is.True);
				Assert.That(second.Graph.BlackboardReference.GetVariable<GameObject>("Self", out var secondOwner), Is.True);
				Assert.That(firstOwner.Value, Is.SameAs(firstObject));
				Assert.That(secondOwner.Value, Is.SameAs(secondObject));
				for (int run = 0; run < 3; run++)
				{
					first.Graph.Start(); first.Graph.Tick(); first.Graph.End();
					Assert.That(first.Graph.IsRunning, Is.False);
				}
				Assert.That(source.IsRunning, Is.False);
			}
			finally { Object.DestroyImmediate(firstObject); Object.DestroyImmediate(secondObject); }
		}

		[Test]
		public void FloorBotRetainsTuningAndHasOnlyOneAiBodyPartBehavior()
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mobs/Bot/Resources/Controllers/SimpleBotController.prefab");
			var host = prefab.GetComponentsInChildren<BodyPartBehaviors>(true).Single(h => h.Behaviors.OfType<BrainMobAI>().Any());
			Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(host), Is.False);
			var ai = host.Behaviors.OfType<BrainMobAI>().Single();
			Assert.That(ai.Graph, Is.Not.Null);
			Assert.That(ai.Settings.Task.Kind, Is.EqualTo(MobTaskKind.Floor));
			Assert.That(ai.Settings.WanderInterval, Is.EqualTo(2));
			Assert.That(ai.Settings.SearchInterval, Is.EqualTo(4));
			Assert.That(ai.Settings.Task.Duration, Is.EqualTo(2));
			Assert.That(ai.Settings.Task.TileToPlace, Is.Not.Null);
			Assert.That(ai.Settings.Task.Language, Is.Not.Null);
			Assert.That(ai.Settings.Task.IdleEmaggedDialogue.Count, Is.EqualTo(2));
			Assert.That(host.Behaviors.Count(b => b != null && b.GetType().Namespace.StartsWith("US13.Mobs.BrainAI")), Is.EqualTo(1));
			Assert.That(typeof(BodyPartBehavior).IsAssignableFrom(typeof(BrainMobState)), Is.False);
		}

		[TestCase("FloorBotBody")][TestCase("CleanBotBody")][TestCase("FireBotBody")][TestCase("MedBotBody")][TestCase("SecuritronBody")]
		public void BotPresentationIsAComponentWithFourSprites(string name)
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Mobs/Bot/Resources/Bodies/{name}.prefab");
			var presentation = prefab.GetComponentsInChildren<SimpleBotBody>(true);
			Assert.That(presentation.Length, Is.EqualTo(1), name);
			var sprites = new SerializedObject(presentation[0]).FindProperty("possibleSprites");
			Assert.That(sprites.arraySize, Is.EqualTo(4));
			for (int i = 0; i < 4; i++) Assert.That(sprites.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, name);
		}

		[Test]
		public void WorkingDataAndMovementOwnershipAreIsolatedAndInvalidatedOnStop()
		{
			var first = new MobBehaviorContext(new BrainMobAI());
			var second = new MobBehaviorContext(new BrainMobAI());
			var owner = new object(); var other = new object();
			Assert.That(first.TryAcquireMovement(owner), Is.True);
			Assert.That(first.TryAcquireMovement(other), Is.False);
			first.ReleaseMovement(other);
			Assert.That(first.TryAcquireMovement(other), Is.False);
			Assert.That(second.TryAcquireMovement(other), Is.True);
			int generation = first.Generation;
			first.EndRun();
			Assert.That(first.Generation, Is.GreaterThan(generation));
			Assert.That(first.TryAcquireMovement(other), Is.True);
		}

		[Test]
		public void TaskPresentationStateReachesLateJoiningClients()
		{
			var server = new BrainMobAI();
			server.ApplyNetworkState("{\"IsEmagged\":true,\"IsPerformingTask\":true}");
			var client = new BrainMobAI();
			client.ApplyNetworkState(server.CaptureNetworkState());
			Assert.That(client.IsEmagged && client.IsPerformingTask, Is.True);
			server.SetPerformingTask(false);
			client.ApplyNetworkState(server.CaptureNetworkState());
			Assert.That(client.IsEmagged && !client.IsPerformingTask, Is.True);
		}

		[Test]
		public void MindControlTracksGhostingAndTheActivelyControlledBody()
		{
			bool wasServer = CustomNetworkManager.IsServer;
			var ghost = new GameObject("test mind"); ghost.SetActive(false);
			var body = new GameObject("test body"); body.SetActive(false);
			const uint bodyId = 4294950001;
			try
			{
				CustomNetworkManager.IsServer = true;
				var identity = ghost.AddComponent<NetworkIdentity>();
				typeof(NetworkIdentity).GetProperty("isServer").SetValue(identity, true);
				ghost.AddComponent<AllClientsObservableMind>();
				var mind = ghost.AddComponent<Mind>();
				typeof(NetworkIdentity).GetMethod("InitializeNetworkBehaviours", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(identity, null);
				NetworkServer.spawned.Add(bodyId, body.AddComponent<NetworkIdentity>());
				typeof(Mind).GetField("IDActivelyControlling", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(mind, bodyId);
				Assert.That(mind.IsActivelyControlling(body), Is.False);
				mind.ControlledBy = new PlayerInfo();
				Assert.That(mind.IsActivelyControlling(body), Is.True);
				mind.IsGhosting = true;
				Assert.That(mind.IsActivelyControlling(body), Is.False);
				mind.IsGhosting = false;
				Assert.That(mind.IsActivelyControlling(body), Is.True);
				Assert.That(mind.IsActivelyControlling(ghost), Is.False);
				mind.ControlledBy = null;
				Assert.That(mind.IsActivelyControlling(body), Is.False);
			}
			finally
			{
				NetworkServer.spawned.Remove(bodyId);
				Object.DestroyImmediate(ghost); Object.DestroyImmediate(body);
				CustomNetworkManager.IsServer = wasServer;
			}
		}
	}
}
