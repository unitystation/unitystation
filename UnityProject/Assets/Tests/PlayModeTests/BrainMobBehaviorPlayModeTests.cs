#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using US13.HealthV2.Living;
using US13.Core.Editor;
using US13.Mobs.BrainAI;
using US13.Player;
using US13.Systems.Inventory;

namespace Tests
{
	public class BrainMobBehaviorPlayModeTests
	{
		[UnityTest] public IEnumerator BotGraphCanSuspendAndRestart() => ExerciseGraph("SimpleBot");
		[UnityTest] public IEnumerator SlimeGraphCanSuspendAndRestart() => ExerciseGraph("Slime");
		[UnityTest] public IEnumerator ArialGraphCanSuspendAndRestart() => ExerciseGraph("Arial");

		[UnityTest] public IEnumerator FloorBotPrefabsCanInstantiate() => InstantiateParts(BotBody("FloorBotBody"));
		[UnityTest] public IEnumerator CleanBotPrefabsCanInstantiate() => InstantiateParts(BotBody("CleanBotBody"));
		[UnityTest] public IEnumerator FireBotPrefabsCanInstantiate() => InstantiateParts(BotBody("FireBotBody"));
		[UnityTest] public IEnumerator MedBotPrefabsCanInstantiate() => InstantiateParts(BotBody("MedBotBody"));
		[UnityTest] public IEnumerator SecuritronPrefabsCanInstantiate() => InstantiateParts(BotBody("SecuritronBody"));
		[UnityTest] public IEnumerator SimplebotSpeciesPrefabsCanInstantiate() => InstantiateParts(SpeciesParts("BodyParts/SimpleBots/Simplebot"));
		[UnityTest] public IEnumerator SlimeSpeciesPrefabsCanInstantiate() => InstantiateParts(SpeciesParts("BodyParts/SlimeBall/SlimeBall"));
		[UnityTest] public IEnumerator ArialSpeciesPrefabsCanInstantiate() => InstantiateParts(SpeciesParts("Arial/Arial"));
		[UnityTest] public IEnumerator HumanNPCSpeciesPrefabsCanInstantiate() => InstantiateParts(SpeciesParts("BodyParts/Human/HumanNPC"));

		private static IEnumerable<GameObject> BotBody(string name)
		{
			yield return AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Mobs/Bot/Resources/Bodies/{name}.prefab");
		}

		private static IEnumerable<GameObject> SpeciesParts(string name)
		{
			var species = AssetDatabase.LoadAssetAtPath<PlayerHealthData>($"Assets/Prefabs/Player/Resources/{name}.asset");
			Assert.That(species, Is.Not.Null);
			var parts = species.Base;
			return new[] { parts.Head, parts.Torso, parts.ArmLeft, parts.ArmRight, parts.LegLeft, parts.LegRight }
				.Where(list => list != null).SelectMany(list => list.Elements);
		}

		private static IEnumerator InstantiateParts(IEnumerable<GameObject> parts)
		{
			// Exercise Unity's cloning of saved species, inherited bodies and contained organs without
			// running world-dependent spawn hooks. Server placement needs a loaded station and network host.
			var owner = new GameObject("saved mob prefab test"); owner.SetActive(false);
			var pending = new Queue<GameObject>(parts);
			var visited = new HashSet<GameObject>();
			int brains = 0;
			try
			{
				while (pending.Count > 0)
				{
					var prefab = pending.Dequeue();
					Assert.That(prefab, Is.Not.Null, "A saved body-part reference must resolve before spawning.");
					if (!visited.Add(prefab)) continue;
					var instance = Object.Instantiate(prefab, owner.transform);
					Assert.That(instance, Is.Not.Null, prefab.name);
					foreach (var host in instance.GetComponentsInChildren<BodyPartBehaviors>(true))
						foreach (var ai in host.Behaviors.OfType<BrainMobAI>())
						{
							Assert.That(ai.Graph, Is.Not.Null, prefab.name);
							MobBehaviorGraphTools.ValidateContextExtensions(ai, prefab.name);
							MobBehaviorGraphTools.ValidateSettings(ai, prefab.name);
							brains++;
						}
					foreach (var storage in instance.GetComponentsInChildren<ItemStorage>(true))
						if (storage.Populater != null)
							foreach (var content in storage.Populater.GetFirstLayerDeprecatedAndNew())
								if (content != null) pending.Enqueue(content);
				}
				Assert.That(brains, Is.EqualTo(1), "The saved parts must instantiate exactly one configured AI brain.");
			}
			finally { Object.Destroy(owner); }
			yield return null;
		}

		private static IEnumerator ExerciseGraph(string name)
		{
			var source = AssetDatabase.LoadAllAssetsAtPath($"Assets/ScriptableObjects/Ai/BehaviorGraphs/{name}.asset").OfType<BehaviorGraph>().Single();
			var owner = new GameObject("graph lifecycle test");
			var agent = owner.AddComponent<BehaviorGraphAgent>(); agent.enabled = false;
			agent.Graph = source; agent.Init();
			var instance = agent.Graph;
			Assert.That(instance, Is.Not.SameAs(source));
			Assert.That(agent.GetVariable<GameObject>("Self", out var self), Is.True);
			Assert.That(self.Value, Is.SameAs(owner));
			try
			{
				// No attached eligible brain: these graphs must remain safe during spawn/removal transitions.
				for (int run = 0; run < 3; run++)
				{
					instance.Start();
					yield return null;
					instance.Tick(); instance.End();
					Assert.That(instance.IsRunning, Is.False);
					yield return null;
				}
				Assert.That(source.IsRunning, Is.False);
			}
			finally { Object.Destroy(owner); }
			yield return null;
			Assert.That(agent.Graph, Is.Null, "Destroying the agent must relinquish its graph instance.");
			Assert.That(instance.IsRunning, Is.False);
			// The package tears down released instances without destroying their native object.
			Object.Destroy(instance);
			yield return null;
		}
	}
}
#endif
