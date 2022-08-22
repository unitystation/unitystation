using System;
using UnityEngine;

namespace Items.Dice
{
	/// <summary>
	/// For dice that are multi-dimensional.
	/// </summary>
	public class RollMultidimensionalDie : RollDie
	{
		[Tooltip("The amount of sides a die for a single dimension has.")]
		public int singleDieSides;

		public override string Examine(Vector3 worldPos = default)
		{
			return $"It is showing Cube-Side: {GetDimensionalMessage()}.";
		}

		protected override string GetMessage()
		{
			return $"It lands with Cube-Side: {GetDimensionalMessage()}.";
		}

		private string GetDimensionalMessage()
		{
			int remainder, quotient = Math.DivRem(result, singleDieSides, out remainder);

			return $"{quotient + 1}-{remainder + 1}";
		}
	}
}
