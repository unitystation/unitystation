using System;
using System.Linq;
using NUnit.Framework;
using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using US13.Core.Editor;
using US13.HealthV2.Living;
using US13.Mobs.BrainAI;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.SettingsExtensions;
using Object = UnityEngine.Object;

namespace Tests
{
	public class MobSettingsExtensionTests
	{
		private static BehaviorGraph Graph(string name) => AssetDatabase.LoadAllAssetsAtPath($"{MobBehaviorGraphTools.GraphFolder}/{name}.asset").OfType<BehaviorGraph>().Single();

		[Test]
		public void SettingsLookupReturnsTheSelectedConfigurationAndReportsMissingOrAmbiguousValues()
		{
			var task = new BotTaskSettings { Duration = 7 };
			var ai = new BrainMobAI { SettingsExtensions = new() { task, new SlimeBehaviorSettings() } };
			Assert.That(ai.TryGetSettings<BotTaskSettings>(out var selected), Is.True);
			Assert.That(selected, Is.SameAs(task));
			Assert.That(ai.GetSettings<BotTaskSettings>().Duration, Is.EqualTo(7));
			Assert.That(ai.TryGetSettings<ArialBehaviorSettings>(out var missing), Is.False);
			Assert.That(missing, Is.Null);
			Assert.Throws<InvalidOperationException>(() => ai.GetSettings<ArialBehaviorSettings>());
			Assert.Throws<InvalidOperationException>(() => ai.TryGetSettings<IMobBehaviorSettingsExtension>(out _));
			ai.SettingsExtensions.Add(new BotTaskSettings());
			Assert.Throws<InvalidOperationException>(() => ai.GetSettings<BotTaskSettings>());
		}

		[Test]
		public void SharedAndExtensionIntervalsUseTheConfiguredValues()
		{
			var slime = new SlimeBehaviorSettings { Interval = 2.5f };
			var arial = new ArialBehaviorSettings { ThrowInterval = 0.75f };
			var ai = new BrainMobAI { SettingsExtensions = new() { slime, arial } };
			ai.Settings.SearchInterval = 4;
			Assert.That(ai.TryGetInterval(1, out var search), Is.True); Assert.That(search, Is.EqualTo(4));
			Assert.That(ai.TryGetInterval(3, out var eating), Is.True); Assert.That(eating, Is.EqualTo(2.5f));
			Assert.That(ai.TryGetInterval(4, out var throwing), Is.True); Assert.That(throwing, Is.EqualTo(0.75f));
			slime.Interval = 3;
			Assert.That(ai.TryGetInterval(3, out eating), Is.True); Assert.That(eating, Is.EqualTo(3));
			Assert.That(ai.TryGetInterval(99, out _), Is.False);
		}

		[TestCase("SimpleBot", typeof(BotTaskSettings))]
		[TestCase("Slime", typeof(SlimeBehaviorSettings))]
		[TestCase("Arial", typeof(ArialBehaviorSettings))]
		public void GraphValidationRequiresItsSelectedSettings(string graphName, Type settingsType)
		{
			var ai = new BrainMobAI { Graph = Graph(graphName) };
			var error = Assert.Throws<InvalidOperationException>(() => MobBehaviorGraphTools.ValidateSettings(ai, "test brain"));
			Assert.That(error.Message, Does.Contain("test brain").And.Contain(settingsType.FullName));
			ai.SettingsExtensions.Add((IMobBehaviorSettingsExtension)Activator.CreateInstance(settingsType));
			Assert.DoesNotThrow(() => MobBehaviorGraphTools.ValidateSettings(ai, "test brain"));
		}

		[Test]
		public void ValidationRejectsInvalidSelectionsAndDetectsContextSettingsRequirements()
		{
			var ai = new BrainMobAI { Graph = Graph("BrainNPC"), SettingsExtensions = null };
			Assert.Throws<InvalidOperationException>(() => MobBehaviorGraphTools.ValidateSettings(ai, "null list"));
			ai.SettingsExtensions = new() { null };
			Assert.Throws<InvalidOperationException>(() => MobBehaviorGraphTools.ValidateSettings(ai, "null entry"));
			ai.SettingsExtensions = new() { new BotTaskSettings(), new BotTaskSettings() };
			Assert.Throws<InvalidOperationException>(() => MobBehaviorGraphTools.ValidateSettings(ai, "duplicate"));
			ai.SettingsExtensions.Clear(); ai.ContextExtensions.Add(new TaskContextExtension());
			var error = Assert.Throws<InvalidOperationException>(() => MobBehaviorGraphTools.ValidateSettings(ai, "task context"));
			Assert.That(error.Message, Does.Contain(nameof(BotTaskSettings)));
			ai.SettingsExtensions.Add(new BotTaskSettings());
			Assert.DoesNotThrow(() => MobBehaviorGraphTools.ValidateSettings(ai, "task context"));
		}

		[Test]
		public void InstantiatedBrainsOwnTheirMutableSettings()
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Implants/Slime/SlimeCore.prefab");
			var owner = new GameObject("settings isolation test"); owner.SetActive(false);
			BrainMobAI Brain(GameObject root) => root.GetComponentsInChildren<BodyPartBehaviors>(true).SelectMany(host => host.Behaviors).OfType<BrainMobAI>().Single();
			try
			{
				var source = Brain(prefab).GetSettings<SlimeBehaviorSettings>();
				var first = Brain(Object.Instantiate(prefab, owner.transform)).GetSettings<SlimeBehaviorSettings>();
				var second = Brain(Object.Instantiate(prefab, owner.transform)).GetSettings<SlimeBehaviorSettings>();
				Assert.That(first, Is.Not.SameAs(source).And.Not.SameAs(second));
				bool original = source.Friendly;
				first.Friendly = !original;
				Assert.That(second.Friendly, Is.EqualTo(original));
				Assert.That(source.Friendly, Is.EqualTo(original));
			}
			finally { Object.DestroyImmediate(owner); }
		}
	}
}
