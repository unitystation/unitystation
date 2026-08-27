using System.Linq;
using NUnit.Framework;
using UnityEngine;
using US13.Systems.StatusesAndEffects;

namespace Tests.StatusAndEffectsFramework
{
	[Category(nameof(Balance))]
	public class StatusEffectManagerTest
	{
		private StatusEffectManager manager;

		[SetUp]
		public void Setup()
		{
			var mockEntity = new GameObject();
			manager = mockEntity.AddComponent<StatusEffectManager>();
		}

		[Test]
		public void WhenAddingAnewStatusToManagerStatusIsAdded()
		{
			var basicStatus = ScriptableObject.CreateInstance<MockStatus>();
			manager.AddStatus(basicStatus);
			Assert.True(manager.HasStatus(basicStatus));
			Assert.AreEqual(1, manager.Statuses.Count);
			Assert.AreNotSame(basicStatus, manager.Statuses.Single());
		}

		[Test]
		public void WhenAddingExistingStatusToManagerNewStatusIsNotAdded()
		{
			var basicStatus = ScriptableObject.CreateInstance<MockStatus>();
			manager.AddStatus(basicStatus);
			manager.AddStatus(basicStatus);
			Assert.True(manager.HasStatus(basicStatus));
			Assert.AreEqual(1, manager.Statuses.Count);
		}

		[Test]
		public void WhenAddingExistingStatusFromDifferentSourcesOnlyFirstIsAdded()
		{
			var basicStatus = ScriptableObject.CreateInstance<MockStatus>();
			var basicStatus2 = ScriptableObject.CreateInstance<MockStatus>();
			basicStatus.name = "basicStatus";
			basicStatus2.name = "basicStatus";
			manager.AddStatus(basicStatus);
			manager.AddStatus(basicStatus2);
			Assert.True(manager.HasStatus(basicStatus));
			Assert.True(manager.HasStatus(basicStatus2));
			Assert.AreEqual(1, manager.Statuses.Count);
		}

		[Test]
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

		[Test]
		public void WhenAddingImmediateStatusEffectIsImmediate()
		{
			var immediate = ScriptableObject.CreateInstance<ImmediateStatusEffect>();
			manager.AddStatus(immediate);
			var activeImmediate = manager.Statuses.OfType<ImmediateStatusEffect>().Single();
			Assert.True(manager.HasStatus(immediate));
			Assert.True(activeImmediate.DidEffect);
		}

		[Test]
		public void WhenAddingStackableToAlreadyExistingEffectStackIsIncremented()
		{
			var stackable = ScriptableObject.CreateInstance<StackableStatusEffect>();
			manager.AddStatus(stackable);
			var activeStackable = manager.Statuses.OfType<StackableStatusEffect>().Single();
			Assert.True(manager.HasStatus(stackable));
			Assert.AreEqual(1, activeStackable.Stacks);
			manager.AddStatus(stackable);
			Assert.AreEqual(2, activeStackable.Stacks);
		}

		[Test]
		public void WhenSameStatusAssetIsAddedToDifferentManagersEachManagerGetsOwnInstance()
		{
			var secondManager = new GameObject().AddComponent<StatusEffectManager>();
			var status = ScriptableObject.CreateInstance<ExpirableStatusEffect>();
			status.name = "expirableStatus";
			manager.AddStatus(status);
			secondManager.AddStatus(status);

			var firstActiveStatus = manager.Statuses.Single();
			var secondActiveStatus = secondManager.Statuses.Single();

			Assert.AreNotSame(status, firstActiveStatus);
			Assert.AreNotSame(status, secondActiveStatus);
			Assert.AreNotSame(firstActiveStatus, secondActiveStatus);
			Assert.True(manager.HasStatus(status));
			Assert.True(secondManager.HasStatus(status));
		}

		[Test]
		public void WhenExpirableStatusExpiresInOneManagerOtherManagersKeepTheirStatus()
		{
			var secondManager = new GameObject().AddComponent<StatusEffectManager>();
			var status = ScriptableObject.CreateInstance<ExpirableStatusEffect>();
			status.name = "expirableStatus";
			manager.AddStatus(status);
			secondManager.AddStatus(status);

			var firstActiveStatus = manager.Statuses.OfType<ExpirableStatusEffect>().Single();
			firstActiveStatus.CheckExpiration();

			Assert.False(manager.HasStatus(status));
			Assert.True(secondManager.HasStatus(status));
		}

		[Test]
		public void WhenRemovingStatusByAssetActiveInstanceIsRemoved()
		{
			var status = ScriptableObject.CreateInstance<MockStatus>();
			status.name = "basicStatus";
			manager.AddStatus(status);

			manager.RemoveStatus(status);

			Assert.False(manager.HasStatus(status));
			Assert.AreEqual(0, manager.Statuses.Count);
		}

		[Test]
		public void WhenAddingExistingExpirableStatusSubscriptionIsNotDuplicated()
		{
			var status = ScriptableObject.CreateInstance<ExpirableStatusEffect>();
			status.name = "expirableStatus";
			manager.AddStatus(status);
			manager.AddStatus(status);

			var activeStatus = manager.Statuses.OfType<ExpirableStatusEffect>().Single();

			Assert.AreEqual(1, activeStatus.SubscriberCount);
		}

		[Test]
		public void WhenAddingExistingImmediateStatusEffectRunsAgainWithoutAddingDuplicateStatus()
		{
			var immediate = ScriptableObject.CreateInstance<ImmediateStatusEffect>();
			immediate.name = "immediateStatus";
			manager.AddStatus(immediate);
			var activeImmediate = manager.Statuses.OfType<ImmediateStatusEffect>().Single();

			manager.AddStatus(immediate);

			Assert.AreEqual(1, manager.Statuses.Count);
			Assert.AreEqual(2, activeImmediate.EffectCount);
		}

		[Test]
		public void WhenAddingStatusCloneStatusCanBeCheckedAndRemovedByOriginalAsset()
		{
			var status = ScriptableObject.CreateInstance<MockStatus>();
			status.name = "basicStatus";
			var statusClone = Object.Instantiate(status);
			manager.AddStatus(statusClone);

			Assert.True(manager.HasStatus(status));

			manager.RemoveStatus(status);

			Assert.False(manager.HasStatus(status));
			Assert.AreEqual(0, manager.Statuses.Count);
		}

		[Test]
		public void WhenAddingRuntimeCloneTransientStackConfigurationIsPreserved()
		{
			var status = ScriptableObject.CreateInstance<StackableStatusEffect>();
			status.name = "stackableStatus";
			var statusClone = Object.Instantiate(status);
			statusClone.InitialStacks = 5;

			manager.AddStatus(statusClone);

			var activeStatus = manager.Statuses.OfType<StackableStatusEffect>().Single();
			Assert.AreSame(statusClone, activeStatus);
			Assert.AreEqual(5, activeStatus.Stacks);
			Assert.True(manager.HasStatus(status));
		}
	}
}
