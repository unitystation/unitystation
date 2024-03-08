using NUnit.Framework;
using Systems.StatusesAndEffects;
using UnityEngine;

namespace Tests.StatusAndEffectsFramework
{

	public class StatusEffectManagerTest
	{
		private StatusEffectManager manager;

		public void Setup()
		{
			var mockEntity = new GameObject();
			manager = mockEntity.AddComponent<StatusEffectManager>();
		}

		public void WhenAddingAnewStatusToManagerStatusIsAdded()
		{
			var basicStatus = ScriptableObject.CreateInstance<MockStatus>();
			manager.AddStatus(basicStatus);
			Assert.True(manager.HasStatus(basicStatus));
			Assert.AreEqual(1, manager.Statuses.Count);
		}

		public void WhenAddingExistingStatusToManagerNewStatusIsNotAdded()
		{
			var basicStatus = ScriptableObject.CreateInstance<MockStatus>();
			manager.AddStatus(basicStatus);
			manager.AddStatus(basicStatus);
			Assert.True(manager.HasStatus(basicStatus));
			Assert.AreEqual(1, manager.Statuses.Count);
		}

		public void WhenAddingExistingStatusFromDifferentSourcesOnlyFirstIsAdded()
		{
			var basicStatus = ScriptableObject.CreateInstance<MockStatus>();
			var basicStatus2 = ScriptableObject.CreateInstance<MockStatus>();
			basicStatus.name = "basicStatus"; // in game, name property is populated by file name of the scriptable object.
			basicStatus2.name = "basicStatus";
			manager.AddStatus(basicStatus);
			manager.AddStatus(basicStatus2);
			Assert.True(manager.HasStatus(basicStatus));
			Assert.True(manager.HasStatus(basicStatus2));
			Assert.AreEqual(1, manager.Statuses.Count);
		}

		public void WhenAddingDifferentStatusesBothAreAdded()
		{
			var basicStatus = ScriptableObject.CreateInstance<MockStatus>();
			var basicStatus2 = ScriptableObject.CreateInstance<MockStatus>();
			basicStatus.name = "basicStatus";
			basicStatus2.name = "basicStatus2";
			manager.AddStatus(basicStatus);
			manager.AddStatus(basicStatus2);
			Assert.True(manager.HasStatus(basicStatus));
			Assert.True(manager.HasStatus(basicStatus2));
			Assert.AreEqual(2, manager.Statuses.Count);
		}

		public void WhenAddingImmediateStatusEffectIsImmediate()
		{
			var immediate = ScriptableObject.CreateInstance<ImmediateStatusEffect>();
			manager.AddStatus(immediate);
			Assert.True(manager.HasStatus(immediate));
			Assert.True(immediate.DidEffect);
		}
		
		public void WhenAddingStackableToAlreadyExistingEffectStackIsIncremented()
		{
			var stackable = ScriptableObject.CreateInstance<StackableStatusEffect>();
			manager.AddStatus(stackable);
			Assert.True(manager.HasStatus(stackable));
			Assert.AreEqual(1, stackable.Stacks);
			manager.AddStatus(stackable);
			Assert.AreEqual(2, stackable.Stacks);
		}

	}
}