using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Chemistry
{
    public class ReactionContainerFixture
    {
		[SetUp]
		public void SetUp() { }

		private static ReagentContainer GetContainer(int maxCapacity, Dictionary<string, float> contents)
		{
			GameObject obj = new GameObject();
			obj.AddComponent<ReagentContainer>();
			ReagentContainer container = obj.GetComponent<ReagentContainer>();
			container.MaxCapacity = maxCapacity;
			container.Contents = contents;
			return container;
		}

		private static void AssertContainerContentsEqualTo(ReagentContainer container, Dictionary<string, float> expected)
		{
			Assert.AreEqual(expected.Count, container.Contents.Count);
			foreach (var pair in expected)
			{
				Assert.IsTrue(container.Contents.TryGetValue(pair.Key, out float val));
				Assert.AreEqual(pair.Value, val, 0.00000001, $"Wrong amount of {pair.Key}.");
			}
		}

		private static readonly object[] ContainerInitialStateAndReagentsToAddAndFinalState = {
			//Test adding without overflow
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 } }, new Dictionary<string, float>{ {"A", 10 } }, new Dictionary<string, float>{ {"A", 20} } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 } }, new Dictionary<string, float>{ {"B", 10 } }, new Dictionary<string, float>{ {"A", 10 }, { "B", 10 } } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 } }, new Dictionary<string, float>{ {"A", 5 } }, new Dictionary<string, float>{ {"A", 15 }, { "B", 10 } } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 } }, new Dictionary<string, float>{ {"A", 5 }, { "B", 20 } }, new Dictionary<string, float>{ {"A", 15 }, { "B", 30 } } },
			new object[] { 50, new Dictionary<string, float>{ }, new Dictionary<string, float>{ {"A", 8 }, { "B", 22 } }, new Dictionary<string, float>{ {"A", 8 }, { "B", 22 } } },
			//Test overflow
			new object[] { 20, new Dictionary<string, float>{ { "A", 10 } }, new Dictionary<string, float>{ {"A", 10 } }, new Dictionary<string, float>{ {"A", 20} } },
			new object[] { 20, new Dictionary<string, float>{ { "A", 10 } }, new Dictionary<string, float>{ {"A", 15 } }, new Dictionary<string, float>{ {"A", 20} } },
			new object[] { 20, new Dictionary<string, float>{ { "A", 20 } }, new Dictionary<string, float>{ {"A", 10 } }, new Dictionary<string, float>{ {"A", 20} } },
			//Test multiple overflow
			new object[] { 20, new Dictionary<string, float>{ { "A", 10 } }, new Dictionary<string, float>{ {"A", 10 }, { "B", 10 } }, new Dictionary<string, float>{ {"A", 15}, { "B", 5 } } },
			new object[] { 100, new Dictionary<string, float>{ { "A", 90 } }, new Dictionary<string, float>{ {"A", 90 }, { "B", 10 } }, new Dictionary<string, float>{ {"A", 99}, { "B", 1 } } },
			new object[] { 10, new Dictionary<string, float>{ }, new Dictionary<string, float>{ { "A", 60 }, { "B", 10 }, { "C", 30 } }, new Dictionary<string, float>{ {"A", 6}, { "B", 1 }, { "C", 3 } } },
		};

		[TestCaseSource(nameof(ContainerInitialStateAndReagentsToAddAndFinalState))]
		public void GivenContainerWithReagents_WhenAddReagents_ThenContainerContainsCorrectAmountsOfReagents(int capacity,
			Dictionary<string, float> initial, Dictionary<string, float> toAdd, Dictionary<string, float> final)
        {
			var container = GetContainer(capacity, initial);
			container.AddReagents(toAdd, 20);
			AssertContainerContentsEqualTo(container, final);
		}

		private static readonly object[] ContainerInitialStateAndReagentsToMoveAndFinalState = {
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 } }, 5, new Dictionary<string, float>{ { "A", 5 } } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 } }, 10, new Dictionary<string, float>{ } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 } }, 20, new Dictionary<string, float>{ } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 } }, 10, new Dictionary<string, float>{ {"A", 5 }, { "B", 5 } } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 } }, 16, new Dictionary<string, float>{ {"A", 2 }, { "B", 2 } } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 } }, 20, new Dictionary<string, float>{ } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 } }, 30, new Dictionary<string, float>{ } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 }, { "C", 10 } }, 15, new Dictionary<string, float>{ {"A", 5 }, { "B", 5 }, { "C", 5 } } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 }, { "C", 10 } }, 21, new Dictionary<string, float>{ {"A", 3 }, { "B", 3 }, { "C", 3 } } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 }, { "C", 10 } }, 30, new Dictionary<string, float>{ } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 10 }, { "C", 10 } }, 100, new Dictionary<string, float>{ } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 10 }, { "B", 20 } }, 6, new Dictionary<string, float>{ {"A", 8 }, { "B", 16 } } },
			new object[] { 50, new Dictionary<string, float>{ { "A", 5 }, { "B", 10 }, { "C", 15 } }, 12, new Dictionary<string, float>{ {"A", 3 }, { "B", 6 }, { "C", 9 } } },
		};

		[TestCaseSource(nameof(ContainerInitialStateAndReagentsToMoveAndFinalState))]
		public void GivenContainerWithReagents_WhenMoveReagentsTo_ThenContainerContainsCorrectAmountsOfReagents(int capacity, Dictionary<string, float> initial, int amountToMove, Dictionary<string, float> final)
		{
			var container = GetContainer(capacity, initial);
			container.MoveReagentsTo(amountToMove, null);
			AssertContainerContentsEqualTo(container, final);
		}
	}
}
