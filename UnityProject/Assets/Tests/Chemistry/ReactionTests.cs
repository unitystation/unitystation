using System.Collections;
using Chemistry;
using NUnit.Framework;
using UnityEngine;
using Effect = Chemistry.Effect;

namespace Tests.Chemistry
{
	[TestFixture]
	public class ReactionTests
	{
		[Test]
		[TestCaseSource(nameof(ReactionTestData))]
		public void SimpleReaction(ReagentMix mix, Reaction reaction, ReagentMix result)
		{
			reaction.Apply(null, mix);

			Assert.True(mix.ContentEquals(result));
		}


		private static IEnumerable ReactionTestData()
		{
			var a = ScriptableObject.CreateInstance<Reagent>();
			a.Name = nameof(a);
			var b = ScriptableObject.CreateInstance<Reagent>();
			b.Name = nameof(b);
			var c = ScriptableObject.CreateInstance<Reagent>();
			c.Name = nameof(c);

			var simpleReaction = ScriptableObject.CreateInstance<Reaction>();
			simpleReaction.ingredients = new DictionaryReagentInt {[a] = 1, [b] = 1};
			simpleReaction.catalysts = new DictionaryReagentInt { };
			simpleReaction.inhibitors = new DictionaryReagentInt { };
			simpleReaction.results = new DictionaryReagentInt {[c] = 1};
			simpleReaction.effects = new global::Chemistry.Effect[0];

			var tempReaction = ScriptableObject.CreateInstance<Reaction>();
			tempReaction.ingredients = new DictionaryReagentInt {[a] = 1, [b] = 1};
			tempReaction.catalysts = new DictionaryReagentInt { };
			tempReaction.inhibitors = new DictionaryReagentInt { };
			tempReaction.results = new DictionaryReagentInt {[c] = 1};
			tempReaction.effects = new global::Chemistry.Effect[0];
			tempReaction.hasMinTemp = true;
			tempReaction.serializableTempMin = 200;
			tempReaction.hasMaxTemp = true;
			tempReaction.serializableTempMax = 300;

			// Non temperature dependant reaction, should always occur
			yield return new object[]
			{
				new ReagentMix(new DictionaryReagentFloat {[a] = 1, [b] = 1}),
				simpleReaction,
				new ReagentMix(new DictionaryReagentFloat {[c] = 1}),
			};

			yield return new object[]
			{
				new ReagentMix(new DictionaryReagentFloat {[a] = 1, [b] = 1}, 100),
				simpleReaction,
				new ReagentMix(new DictionaryReagentFloat {[c] = 1}, 100),
			};

			yield return new object[]
			{
				new ReagentMix(new DictionaryReagentFloat {[a] = 1, [b] = 1}, 300),
				simpleReaction,
				new ReagentMix(new DictionaryReagentFloat {[c] = 1}, 300),
			};

			// Temperature dependant reaction, should only occur in the right temperature window
			yield return new object[]
			{
				new ReagentMix(new DictionaryReagentFloat {[a] = 1, [b] = 1}),
				tempReaction,
				new ReagentMix(new DictionaryReagentFloat {[c] = 1}),
			};

			yield return new object[]
			{
				new ReagentMix(new DictionaryReagentFloat {[a] = 1, [b] = 1}, 100),
				tempReaction,
				new ReagentMix(new DictionaryReagentFloat {[a] = 1, [b] = 1}, 100),
			};

			yield return new object[]
			{
				new ReagentMix(new DictionaryReagentFloat {[a] = 1, [b] = 1}, 300),
				tempReaction,
				new ReagentMix(new DictionaryReagentFloat {[a] = 1, [b] = 1}, 300),
			};
		}
	}
}