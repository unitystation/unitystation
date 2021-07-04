using System.Collections.Generic;
using System.Reflection;
using Systems.Clearance;
using NUnit.Framework;
using UnityEngine;

namespace Tests.ClearanceFramework
{
	[TestFixture]
	public class HasClearanceTest
	{
		private GameObject mockDoor;
		private ClearanceCheckable checkable;
		private MockedClearanceProvider mockProvider;
		private FieldInfo checkType;

		private class MockedClearanceProvider: IClearanceProvider
		{
			private List<Clearance> issuedClearance;

			public MockedClearanceProvider(List<Clearance> issuedClearance)
			{
				this.issuedClearance = issuedClearance;
			}

			public void SetClearances(List<Clearance> newClearances)
			{
				issuedClearance = newClearances;
			}

			public void ClearClearance()
			{
				issuedClearance.Clear();
			}

			public IEnumerable<Clearance> GetClearance()
			{
				return issuedClearance;
			}
		}

		private bool AttemptAccess()
		{
			return checkable.HasClearance(mockProvider.GetClearance());
		}

		[SetUp]
		public void SetUp()
		{
			mockDoor = new GameObject();
			checkable = mockDoor.AddComponent<ClearanceCheckable>();
			mockProvider = new MockedClearanceProvider(new List<Clearance>());

			var type = checkable.GetType();
			var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var prv in fields)
			{
				if (prv.FieldType != typeof(CheckType)) continue;
				checkType = prv;
				break;
			}
		}

		[TearDown]
		public void TearDown()
		{
			mockProvider.ClearClearance();
		}

		[Test]
		public void GivenNoClearanceWhenNoClearanceRequiredResultsTrue()
		{
			mockProvider = new MockedClearanceProvider(new List<Clearance>());
			//both checkable and provider have no clearances.
			Assert.True(AttemptAccess());
		}

		[Test]
		public void GivenNoClearanceWhenClearanceIsRequiredResultsFalse()
		{
			checkable.SetClearance(new List<Clearance> {Clearance.Captain});
			Assert.False(AttemptAccess());
		}

		[Test]
		public void GivenClearanceWhenAnyClearanceIsRequiredResultsTrue()
		{
			//door requires 2 clearances but is set to ANY on the check
			var clearances = new List<Clearance> {Clearance.Captain, Clearance.Court};
			checkable.SetClearance(clearances);
			mockProvider.SetClearances(new List<Clearance>{Clearance.Court});
			checkType.SetValue(checkable, CheckType.Any);

			Assert.True(AttemptAccess());
		}

		[Test]
		public void GivenInsufficientClearanceWhenAllClearanceIsRequiredResultsFalse()
		{
			checkable.SetClearance(new List<Clearance> {Clearance.Atmospherics, Clearance.Engine});
			mockProvider.SetClearances(new List<Clearance> {Clearance.Atmospherics});
			checkType.SetValue(checkable, CheckType.All);

			Assert.False(AttemptAccess());
		}

		[Test]
		public void GivenAllClearanceWhenAllClearanceIsRequiredResultsTrue()
		{
			checkable.SetClearance(new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar});
			mockProvider.SetClearances(new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar});
			checkType.SetValue(checkable, CheckType.All);

			Assert.True(AttemptAccess());
		}

		[Test]
		public void GivenAllClearanceIssuedInDiffOrderWhenAllClearanceIsRequiredResultsTrue()
		{
			checkable.SetClearance(new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar});
			mockProvider.SetClearances(new List<Clearance> {Clearance.Bar, Clearance.Armory, Clearance.Atmospherics});
			checkType.SetValue(checkable, CheckType.All);

			Assert.True(AttemptAccess());
		}
	}
}