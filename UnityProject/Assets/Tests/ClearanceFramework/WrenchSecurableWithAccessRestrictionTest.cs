using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.HealthV2;
using US13.Items;
using US13.Items.Traits;
using US13.Objects;
using US13.Systems.Clearance;
using US13.Systems.Construction;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace Tests.ClearanceFramework
{
	[TestFixture]
	[Category(nameof(Balance))]
	public class WrenchSecurableWithAccessRestrictionTest
	{
		private GameObject performer;
		private GameObject wrench;
		private GameObject otherItem;
		private GameObject target;
		private GameObject otherTarget;
		private MockClearanceSourceComponent performerClearance;
		private ClearanceRestricted restricted;
		private WrenchSecurableWithAccessRestriction wrenchRestriction;

		[SetUp]
		public void SetUp()
		{
			performer = new GameObject("performer");
			performerClearance = performer.AddComponent<MockClearanceSourceComponent>();

			var commonTraits = AssetDatabase.LoadAssetAtPath<CommonTraits>("Assets/ScriptableObjects/Traits/CommonTraitsSingleton.asset");
			Assert.NotNull(commonTraits);
			Assert.NotNull(commonTraits.Wrench);
			wrench = CreateItem("wrench", commonTraits.Wrench);
			otherItem = CreateItem("not a wrench");

			target = new GameObject("restricted object");
			target.AddComponent<NetworkIdentity>();
			wrenchRestriction = target.AddComponent<WrenchSecurableWithAccessRestriction>();
			restricted = target.GetComponent<ClearanceRestricted>();
			restricted.SetCheckType(CheckType.Any);
			restricted.SetClearance(new List<Clearance> {Clearance.Security});

			otherTarget = new GameObject("other target");
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(performer);
			Object.DestroyImmediate(wrench);
			Object.DestroyImmediate(otherItem);
			Object.DestroyImmediate(target);
			Object.DestroyImmediate(otherTarget);
		}

		[Test]
		public void GivenRequiredClearanceWhenCheckingWrenchAccessReturnsTrue()
		{
			performerClearance.SetClearance(Clearance.Security);

			Assert.True(wrenchRestriction.HasClearanceToWrench(performer));
		}

		[Test]
		public void GivenMissingClearanceWhenCheckingWrenchAccessReturnsFalse()
		{
			performerClearance.SetClearance(Clearance.Engine);

			Assert.False(wrenchRestriction.HasClearanceToWrench(performer));
		}

		[Test]
		public void GivenNoClearanceRestrictionWhenCheckingWrenchAccessReturnsTrue()
		{
			restricted.SetClearance(new List<Clearance>());

			Assert.True(wrenchRestriction.HasClearanceToWrench(performer));
		}

		[Test]
		public void ComponentClaimsWrenchInteractionsBeforeHeldItems()
		{
			Assert.That(wrenchRestriction, Is.InstanceOf<IFirstInteractable<HandApply>>());
			Assert.That(performerClearance.LowPopIssuedClearance, Is.Empty);
		}

		[Test]
		public void GivenValidWrenchTargetWhenCheckingWrenchInteractionReturnsTrue()
		{
			Assert.True(wrenchRestriction.IsWrenchInteraction(CreateInteraction(wrench)));
		}

		[Test]
		public void GivenDifferentTargetWhenCheckingWrenchInteractionReturnsFalse()
		{
			Assert.False(wrenchRestriction.IsWrenchInteraction(CreateInteraction(wrench, otherTarget)));
		}

		[Test]
		public void GivenNonWrenchWhenCheckingWrenchInteractionReturnsFalse()
		{
			Assert.False(wrenchRestriction.IsWrenchInteraction(CreateInteraction(otherItem)));
		}

		[Test]
		public void WillInteractReturnsFalseWhenDefaultValidationFails()
		{
			Assert.False(wrenchRestriction.WillInteract(CreateInteraction(wrench), NetworkSide.Server));
		}

		[Test]
		public void GivenRequiredClearanceWhenPerformingInteractionDelegatesToWrenchSecurable()
		{
			performerClearance.SetClearance(Clearance.Security);
			target.GetComponent<WrenchSecurable>().blockAnchorChange = true;

			ExpectClientChatError();
			Assert.DoesNotThrow(() => wrenchRestriction.ServerPerformInteraction(CreateInteraction(wrench)));
		}

		[Test]
		public void GivenMissingClearanceWhenPerformingWrenchInteractionDeniesAccess()
		{
			performerClearance.SetClearance(Clearance.Engine);

			ExpectClientChatError();
			ExpectMissingObjectPhysicsError();
			Assert.DoesNotThrow(() => wrenchRestriction.ServerPerformInteraction(CreateInteraction(wrench)));
		}

		[Test]
		public void GivenMissingClearanceWhenPerformingNonWrenchInteractionOnlyPlaysDeniedSound()
		{
			performerClearance.SetClearance(Clearance.Engine);

			ExpectMissingObjectPhysicsError();
			Assert.DoesNotThrow(() => wrenchRestriction.ServerPerformInteraction(CreateInteraction(otherItem)));
		}

		private HandApply CreateInteraction(GameObject handObject, GameObject interactionTarget = null)
		{
			return HandApply.ByClient(performer, handObject, interactionTarget ?? target, BodyPartType.None,
				null, Intent.Help, null, false);
		}

		private static GameObject CreateItem(string objectName, ItemTrait itemTrait = null)
		{
			var item = new GameObject(objectName);
			item.AddComponent<NetworkIdentity>();
			var itemAttributes = item.AddComponent<ItemAttributesV2>();
			if (itemTrait != null)
			{
				itemAttributes.AddTrait(itemTrait);
			}

			return item;
		}

		private static void ExpectClientChatError()
		{
			LogAssert.Expect(LogType.Error, new Regex("A server only method was called on a client in chat.cs"));
		}

		private static void ExpectMissingObjectPhysicsError()
		{
			LogAssert.Expect(LogType.Error, new Regex("Unable to find UniversalObjectPhysics on restricted object"));
		}

		private sealed class MockClearanceSourceComponent : MonoBehaviour, IClearanceSource
		{
			private List<Clearance> clearance = new();

			public IEnumerable<Clearance> GetCurrentClearance => IssuedClearance;
			public IEnumerable<Clearance> IssuedClearance => clearance;
			public IEnumerable<Clearance> LowPopIssuedClearance => Enumerable.Empty<Clearance>();

			public void SetClearance(params Clearance[] newClearance)
			{
				clearance = newClearance.ToList();
			}
		}
	}
}
