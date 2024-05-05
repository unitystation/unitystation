using System;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Examinables
{
	public class MockExaminable : IExaminable
	{
		public string Message { get; }
		private readonly int _priority;
		public int ExaminablePriority => _priority;

		public MockExaminable(string message, int priority)
		{
			Message = message;
			_priority = priority;
		}

		public string Examine(Vector3 worldPos = default) => Message;
	}

	public class PriorityTest
	{
		[Test]
		public void Examinables_SortedByDescendingPriority()
		{
			var examinables = new IExaminable[]
			{
				new MockExaminable("Low", 1),
				new MockExaminable("Medium", 5),
				new MockExaminable("High", 10)
			};

			// we are assuming this is how RequestExamineMessage will always sort the list. Please update the test if the algo changes.
			Array.Sort(examinables, (first, other) => other.ExaminablePriority.CompareTo(first.ExaminablePriority));

			Assert.AreEqual("High", examinables[0].Examine());
			Assert.AreEqual("Medium", examinables[1].Examine());
			Assert.AreEqual("Low", examinables[2].Examine());
		}

		[Test]
		public void Examinables_WithEqualPriority_RemainInOriginalOrder()
		{
			var examinables = new IExaminable[]
			{
				new MockExaminable("First", 3),
				new MockExaminable("Second", 3),
				new MockExaminable("Third", 3)
			};

			Array.Sort(examinables, (first, other) => other.ExaminablePriority.CompareTo(first.ExaminablePriority));

			Assert.AreEqual("First", examinables[0].Examine());
			Assert.AreEqual("Second", examinables[1].Examine());
			Assert.AreEqual("Third", examinables[2].Examine());
		}
	}
}