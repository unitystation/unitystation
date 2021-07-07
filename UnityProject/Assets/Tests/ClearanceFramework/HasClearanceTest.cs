using System.Collections.Generic;
using Systems.Clearance;
using NUnit.Framework;
using UnityEngine;

namespace Tests.ClearanceFramework
{
	[TestFixture]
	public class HasClearanceTest
	{
		private ClearanceCheckable checkable;

		private bool AttemptAccess(List<Clearance> clearances)
		{
			return checkable.HasClearance(clearances);
		}

		[SetUp]
		public void SetUp()
		{
			var mockDoor = new GameObject();
			checkable = mockDoor.AddComponent<ClearanceCheckable>();
			checkable.SetCheckType(CheckType.Any);
		}

		[Test]
		public void GivenNoClearanceWhenNoClearanceRequiredResultsTrue()
		{
			//both checkable and provider have no clearances.
			Assert.True(AttemptAccess(new List<Clearance>()));
		}

		[Test]
		public void GivenNoClearanceWhenClearanceIsRequiredResultsFalse()
		{
			checkable.SetClearance(new List<Clearance> {Clearance.Captain});
			Assert.False(AttemptAccess(new List<Clearance>()));
		}

		[Test]
		public void GivenClearanceWhenAnyClearanceIsRequiredResultsTrue()
		{
			//door requires 2 clearances but is set to ANY on the check
			var clearances = new List<Clearance> {Clearance.Captain, Clearance.Court};
			checkable.SetClearance(clearances);

			Assert.True(AttemptAccess(new List<Clearance> {Clearance.Court}));
		}

		[Test]
		public void GivenInsufficientClearanceWhenAllClearanceIsRequiredResultsFalse()
		{
			checkable.SetClearance(new List<Clearance> {Clearance.Atmospherics, Clearance.Engine});
			checkable.SetCheckType(CheckType.All);

			Assert.False(AttemptAccess(new List<Clearance> {Clearance.Atmospherics}));
		}

		[Test]
		public void GivenAllClearanceWhenAllClearanceIsRequiredResultsTrue()
		{
			checkable.SetClearance(new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar});
			checkable.SetCheckType(CheckType.All);

			Assert.True(AttemptAccess(new List<Clearance>
				{Clearance.Armory, Clearance.Atmospherics, Clearance.Bar}));
		}

		[Test]
		public void GivenAllClearanceIssuedInDiffOrderWhenAllClearanceIsRequiredResultsTrue()
		{
			checkable.SetClearance(new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar});
			checkable.SetCheckType(CheckType.All);

			Assert.True(AttemptAccess(new List<Clearance>
				{Clearance.Bar, Clearance.Armory, Clearance.Atmospherics}));
		}
	}
}