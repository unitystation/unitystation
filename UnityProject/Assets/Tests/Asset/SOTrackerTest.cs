using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Asset
{
	[Category(nameof(Asset))]
	public class SOTrackerTest
	{
		[Test]
		public void SOTrackerInList()
		{
			bool Fail = false;
			var report = new TestReport();
			var trackers = Utils.FindAssetsByType<SOTracker>().ToList();

			foreach (var tracker in trackers)
			{
				if (tracker == null) continue;
				if (SOListTracker.Instance.SOTrackers.Contains(tracker) != false) continue;
				Fail = true;
				report.AppendLine($" Tracker {tracker.name} is not in SOListTracker");
			}

			if (Fail)
			{
				report.Fail();
			}
			else
			{
				report.AssertPassed();
			}
		}

		[Test]
		public void SOTrackerTestHaveID()
		{
			bool Fail = false;
			var report = new TestReport();
			var trackers = Utils.FindAssetsByType<SOTracker>().ToList();

			HashSet<string> PreviousTakenIDs = new HashSet<string>();

			foreach (var tracker in trackers)
			{
				if (tracker == null) continue;
				if (string.IsNullOrEmpty(tracker.ForeverID) || PreviousTakenIDs.Contains(tracker.ForeverID))
				{
					Fail = true;
					report.AppendLine($" Tracker {tracker.name} Has been updated with a new ID, Please commit");
					tracker.ForceSetID();
				}
				PreviousTakenIDs.Add(tracker.ForeverID);
			}

			if (Fail)
			{
				report.Fail();
			}
			else
			{
				report.AssertPassed();
			}
		}
	}
}