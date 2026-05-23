using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
	[Category("General")]
	public class WrenchSecurableWithAccessRestrictionTest
	{
		private const string BarrierPrefabPath = "Assets/Prefabs/Objects/Security/DeployableSecurityBarrier.prefab";
		private const string WrenchPrefabPath = "Assets/Prefabs/Items/Tools/Wrench.prefab";
		private const string ScrewdriverPrefabPath = "Assets/Prefabs/Items/Tools/Screwdriver.prefab";

		private GameObject performer;
		private GameObject wrenchRoot;
		private GameObject wrench;
		private GameObject otherItemRoot;
		private GameObject otherItem;
		private GameObject targetRoot;
		private GameObject target;
		private GameObject otherTargetRoot;
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

			wrenchRoot = InstantiatePrefab(WrenchPrefabPath);
			wrench = GetItemObject(wrenchRoot);
			otherItemRoot = InstantiatePrefab(ScrewdriverPrefabPath);
			otherItem = GetItemObject(otherItemRoot);
			Assert.True(Validations.HasItemTrait(wrench, commonTraits.Wrench));
			Assert.False(Validations.HasItemTrait(otherItem, commonTraits.Wrench));

			targetRoot = InstantiatePrefab(BarrierPrefabPath);
			wrenchRestriction = GetWrenchRestriction(targetRoot);
			target = wrenchRestriction.gameObject;
			restricted = target.GetComponent<ClearanceRestricted>();
			Assert.NotNull(restricted);
			restricted.SetCheckType(CheckType.Any);
			restricted.SetClearance(new List<Clearance> {Clearance.Security});

			otherTargetRoot = InstantiatePrefab(BarrierPrefabPath);
			otherTarget = GetWrenchRestriction(otherTargetRoot).gameObject;
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(performer);
			Object.DestroyImmediate(wrenchRoot);
			Object.DestroyImmediate(otherItemRoot);
			Object.DestroyImmediate(targetRoot);
			Object.DestroyImmediate(otherTargetRoot);
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
			Assert.DoesNotThrow(() => wrenchRestriction.ServerPerformInteraction(CreateInteraction(wrench)));
		}

		private HandApply CreateInteraction(GameObject handObject, GameObject interactionTarget = null)
		{
			return HandApply.ByClient(performer, handObject, interactionTarget ?? target, BodyPartType.None,
				null, Intent.Help, null, false);
		}

		private static GameObject InstantiatePrefab(string prefabPath)
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			Assert.NotNull(prefab, $"Missing prefab at {prefabPath}");

			var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			Assert.NotNull(instance, $"Could not instantiate prefab at {prefabPath}");
			return instance;
		}

		private static GameObject GetItemObject(GameObject root)
		{
			var itemAttributes = root.GetComponentInChildren<ItemAttributesV2>();
			Assert.NotNull(itemAttributes, $"{root.name} is missing {nameof(ItemAttributesV2)}");
			ApplySerializedInitialTraits(itemAttributes);
			return itemAttributes.gameObject;
		}

		private static void ApplySerializedInitialTraits(ItemAttributesV2 itemAttributes)
		{
			// EditMode prefab instantiation does not run ItemAttributesV2.Awake, so mirror its trait init.
			var serializedObject = new SerializedObject(itemAttributes);
			var initialTraits = serializedObject.FindProperty("initialTraits");
			Assert.NotNull(initialTraits);

			for (var i = 0; i < initialTraits.arraySize; i++)
			{
				var trait = initialTraits.GetArrayElementAtIndex(i).objectReferenceValue as ItemTrait;
				if (trait == null) continue;
				itemAttributes.AddTrait(trait);
			}
		}

		private static WrenchSecurableWithAccessRestriction GetWrenchRestriction(GameObject root)
		{
			var restriction = root.GetComponentInChildren<WrenchSecurableWithAccessRestriction>();
			Assert.NotNull(restriction, $"{root.name} is missing {nameof(WrenchSecurableWithAccessRestriction)}");
			return restriction;
		}

		private static void ExpectClientChatError()
		{
			// EditMode tests call server interaction routing without starting a Mirror server.
			LogAssert.Expect(LogType.Error, new Regex("A server only method was called on a client in chat.cs"));
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
