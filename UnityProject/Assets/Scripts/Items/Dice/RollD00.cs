using UnityEngine;

namespace Items.Dice
{
	public class RollD00 : RollDie
	{
		public override string Examine(Vector3 worldPos = default)
		{
			return $"It is showing side {result - 1}0.";
		}

		protected override string GetMessage()
		{
			return $"The {dieName} lands a {result - 1}0.";
		}
	}
}
