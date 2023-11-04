using System.Collections.Generic;
using System.Linq;
using Systems.Clearance;
using NUnit.Framework;
using UnityEngine;

namespace Tests.ClearanceFramework
{
	public class MockClearanceSource : IClearanceSource
	{
		private List<Clearance> issuedClearance = new();
		private List<Clearance> issuedLowPopClearance = new();
		public bool IsLowPop { get; set; } = false;

		// Skips dependency with GameManager.Centcomm
		public IEnumerable<Clearance> GetCurrentClearance => IsLowPop ? IssuedClearance.Concat(LowPopIssuedClearance).Distinct() : IssuedClearance;
		public IEnumerable<Clearance> IssuedClearance => issuedClearance;
		public IEnumerable<Clearance> LowPopIssuedClearance => issuedLowPopClearance;

		public void SetClearance(IEnumerable<Clearance> clearance, IEnumerable<Clearance> lowPopClearance)
		{
			issuedClearance = new List<Clearance>(clearance);
			issuedLowPopClearance = new List<Clearance>(lowPopClearance);
		}
	}

	[TestFixture]
	public class HasClearanceTest
	{
		private ClearanceRestricted restricted;
		private MockClearanceSource source;

		[SetUp]
		public void SetUp()
		{
			var mockDoor = new GameObject();
			restricted = mockDoor.AddComponent<ClearanceRestricted>();
			restricted.SetCheckType(CheckType.Any);

			source = new MockClearanceSource();
		}

		[Test]
		public void GivenNoClearanceWhenNoClearanceRequiredResultsTrue()
		{
			//both restricted and source have no clearance set.
			Assert.True(restricted.HasClearance(source));
		}

		[Test]
		public void GivenNoClearanceWhenClearanceIsRequiredResultsFalse()
		{
			restricted.SetClearance(new List<Clearance> {Clearance.Captain});
			Assert.False(restricted.HasClearance(source));
		}

		[Test]
		public void GivenClearanceWhenAnyClearanceIsRequiredResultsTrue()
		{
			//door requires 2 clearances but is set to ANY on the check
			var clearances = new List<Clearance> {Clearance.Captain, Clearance.Court};
			restricted.SetClearance(clearances);
			source.SetClearance(new List<Clearance> {Clearance.Court}, new List<Clearance> {Clearance.Court});

			Assert.True(restricted.HasClearance(source));
		}

		[Test]
		public void GivenInsufficientClearanceWhenAllClearanceIsRequiredResultsFalse()
		{
			restricted.SetClearance(new List<Clearance> {Clearance.Atmospherics, Clearance.Engine});
			restricted.SetCheckType(CheckType.All);
			source.SetClearance(new List<Clearance> {Clearance.Atmospherics}, new List<Clearance> {Clearance.Atmospherics});

			Assert.False(restricted.HasClearance(source));
		}

		[Test]
		public void GivenAllClearanceWhenAllClearanceIsRequiredResultsTrue()
		{
			restricted.SetClearance(new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar});
			restricted.SetCheckType(CheckType.All);
			source.SetClearance(new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar},
				new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar});

			Assert.True(restricted.HasClearance(source));
		}

		[Test]
		public void GivenAllClearanceIssuedInDiffOrderWhenAllClearanceIsRequiredResultsTrue()
		{
			restricted.SetClearance(new List<Clearance> {Clearance.Armory, Clearance.Atmospherics, Clearance.Bar});
			restricted.SetCheckType(CheckType.All);
			source.SetClearance(new List<Clearance> {Clearance.Bar, Clearance.Armory, Clearance.Atmospherics},
				new List<Clearance> {Clearance.Bar, Clearance.Armory, Clearance.Atmospherics});

			Assert.True(restricted.HasClearance(source));
		}

		[Test]
		public void GivenSufficientExtraClearanceWhenRoundIsLowPopResultsTrue()
		{
			restricted.SetClearance(new List<Clearance>{ Clearance.Captain });
			source.SetClearance(new List<Clearance>(), new List<Clearance>{ Clearance.Captain });
			source.IsLowPop = true;
			Assert.True(restricted.HasClearance(source));
		}

		[Test]
		public void GivenSufficientExtraClearanceWhenRoundIsNotLowPopResultsFalse()
		{
			restricted.SetClearance(new List<Clearance>{ Clearance.Captain });
			source.SetClearance(new List<Clearance>(), new List<Clearance>{ Clearance.Captain });
			source.IsLowPop = false;
			Assert.False(restricted.HasClearance(source));
		}
	}
}