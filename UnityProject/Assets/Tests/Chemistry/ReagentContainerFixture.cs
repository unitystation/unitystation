using System.Collections;
using System.Collections.Generic;
using System.Linq;
using global::Chemistry;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Chemistry
{
	public class ReactionContainerFixture
	{

		private static ReagentContainer GetContainer(int maxCapacity, ReagentMix contents)
		{
			GameObject obj = new GameObject();
			obj.AddComponent<ReagentContainer>();
			ReagentContainer container = obj.GetComponent<ReagentContainer>();
			container.ReactionSet = new ReactionSet();
			container.maxCapacity = maxCapacity;
			container.Add(contents);
			return container;
		}

		private static void AssertContainerContentsEqualTo(ReagentContainer container, ReagentMix expected)
		{
			container.Clean();
			expected.Clean();
			Assert.AreEqual(expected.Count(), container.Count());
			foreach (var pair in expected)
			{
				var val = container[pair.Key];
				Assert.IsTrue(val != null);
				Assert.AreEqual(pair.Value, val, 0.00000001, $"Wrong amount of {pair.Key}.");
			}
		}

		private static IEnumerable AdditionTestData()
		{
			var a = new global::Chemistry.Reagent();
			var b = new global::Chemistry.Reagent();
			var c = new global::Chemistry.Reagent();
			//Test adding without overflow
			yield return new object[]
			{
				50,
				new ReagentMix(a, 10),
				new ReagentMix(a, 10),
				new ReagentMix(a, 20)
			};
			yield return new object[]
			{
				50,
				new ReagentMix(a, 10),
				new ReagentMix(b, 10),
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}})
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}}),
				new ReagentMix(a, 5),
				new ReagentMix(new DictionaryReagentFloat {{a, 15}, {b, 10}})
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}}),
				new ReagentMix(new DictionaryReagentFloat {{a, 5}, {b, 20}}),
				new ReagentMix(new DictionaryReagentFloat {{a, 15}, {b, 30}})
			};
			yield return new object[]
			{
				50,
				new ReagentMix(),
				new ReagentMix(new DictionaryReagentFloat {{a, 8}, {b, 22}}),
				new ReagentMix(new DictionaryReagentFloat {{a, 8}, {b, 22}})
			};
			//Test overflow
			yield return new object[]
			{
				20,
				new ReagentMix(a, 10),
				new ReagentMix(a, 10),
				new ReagentMix(a, 20)
			};
			yield return new object[]
			{
				20,
				new ReagentMix(a, 10),
				new ReagentMix(a, 15),
				new ReagentMix(a, 20)
			};
			yield return new object[]
			{
				20,
				new ReagentMix(a, 20),
				new ReagentMix(a, 10),
				new ReagentMix(a, 20)
			};
			//Test multiple overflow
			yield return new object[]
			{
				10,
				new ReagentMix(),
				new ReagentMix(new DictionaryReagentFloat {{a, 60}, {b, 10}, {c, 30}}),
				new ReagentMix(new DictionaryReagentFloat {{a, 6}, {b, 1}, {c, 3}})
			};
		}

		[TestCaseSource(nameof(AdditionTestData))]
		public void AdditionTest(
			int capacity,
			ReagentMix initial,
			ReagentMix toAdd,
			ReagentMix final)
		{
			var container = GetContainer(capacity, initial);
			container.Add(toAdd);
			AssertContainerContentsEqualTo(container, final);
		}

		private static IEnumerable RemovalTestData()
		{
			var a = new global::Chemistry.Reagent();
			var b = new global::Chemistry.Reagent();
			var c = new global::Chemistry.Reagent();

			yield return new object[]
			{
				50,
				new ReagentMix(a, 10),
				5,
				new ReagentMix(a, 5)
			};
			yield return new object[]
			{
				50,
				new ReagentMix(a, 10),
				10,
				new ReagentMix()
			};
			yield return new object[]
			{
				50,
				new ReagentMix(a, 10),
				20,
				new ReagentMix()
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}}),
				10,
				new ReagentMix(new DictionaryReagentFloat {{a, 5}, {b, 5}})
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}}),
				16,
				new ReagentMix(new DictionaryReagentFloat {{a, 2}, {b, 2}})
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}}),
				20,
				new ReagentMix()
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}}),
				30,
				new ReagentMix()
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}, {c, 10}}),
				15,
				new ReagentMix(new DictionaryReagentFloat {{a, 5}, {b, 5}, {c, 5}})
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}, {c, 10}}),
				21,
				new ReagentMix(new DictionaryReagentFloat {{a, 3}, {b, 3}, {c, 3}})
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}, {c, 10}}),
				30,
				new ReagentMix()
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 10}, {c, 10}}),
				100,
				new ReagentMix()
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 10}, {b, 20}}),
				6,
				new ReagentMix(new DictionaryReagentFloat {{a, 8}, {b, 16}})
			};
			yield return new object[]
			{
				50,
				new ReagentMix(new DictionaryReagentFloat {{a, 5}, {b, 10}, {c, 15}}),
				12,
				new ReagentMix(new DictionaryReagentFloat {{a, 3}, {b, 6}, {c, 9}})
			};
		}

		[TestCaseSource(nameof(RemovalTestData))]
		public void RemovalTest(
			int capacity,
			ReagentMix initial,
			int amountToMove,
			ReagentMix final)
		{
			var container = GetContainer(capacity, initial);
			container.TakeReagents(amountToMove);
			AssertContainerContentsEqualTo(container, final);
		}
	}
}